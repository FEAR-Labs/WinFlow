using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace WinFlow;

internal static class InstallManager
{
    private const string HandoffArgument = "--installed-handoff";
    private const string HandoffParentPrefix = "--handoff-parent-pid=";
    private const string HandoffReadyEventPrefix = "--handoff-ready-event=";
    private const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\WinFlow";
    private const string EmbeddedIconResourceName = "WinFlow.Icon.ico";
    private static readonly Guid ShellLinkClsid = new("00021401-0000-0000-C000-000000000046");

    internal static string InstallFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "WinFlow");
    internal static string InstalledExePath => Path.Combine(InstallFolder, "WinFlow.exe");
    internal static string InstalledIconPath => Path.Combine(InstallFolder, "WinFlow-Icon.ico");
    internal static string StartupShortcutPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "WinFlow.lnk");
    internal static string StartMenuShortcutPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "WinFlow.lnk");
    internal static string UninstallShortcutPath => Path.Combine(InstallFolder, "Uninstall WinFlow.lnk");

    internal static bool IsRunningFromInstalledLocation()
    {
        try { return string.Equals(Path.GetFullPath(Environment.ProcessPath ?? string.Empty), Path.GetFullPath(InstalledExePath), StringComparison.OrdinalIgnoreCase); }
        catch { return false; }
    }

    internal static bool IsHandoffRequest(string[] args) => args.Any(a => string.Equals(a, HandoffArgument, StringComparison.OrdinalIgnoreCase));
    internal static string? GetHandoffEventName(string[] args) => ParseStringArg(args, HandoffReadyEventPrefix);
    internal static int? GetHandoffParentPid(string[] args) => ParseIntArg(args, HandoffParentPrefix);

    internal static async Task InstallAsync(Action<int, string, string?> progress)
    {
        string source = Environment.ProcessPath ?? throw new InvalidOperationException("Environment.ProcessPath no está disponible.");
        progress(15, "Preparando WinFlow…", "Creando la carpeta de instalación.");
        await Task.Run(() => Directory.CreateDirectory(InstallFolder)).ConfigureAwait(false);
        progress(35, "Copiando archivos…", "Instalando WinFlow en tu equipo.");
        await Task.Run(() =>
        {
            CopyExecutableAtomically(source);
            WriteInstalledIcon();
        }).ConfigureAwait(false);
        progress(52, "Configurando WinFlow…", "Preparando accesos directos.");
        await EnsureShortcutsAsync().ConfigureAwait(false);
        progress(60, "Registrando aplicación…", "Añadiendo WinFlow a Aplicaciones instaladas de Windows.");
        await Task.Run(RegisterInstalledApp).ConfigureAwait(false);
        progress(68, "Copia completada", "Cambiando a la versión instalada.");
    }

    internal static Process StartInstalledCopy(string readyEventName)
    {
        string args = $"{HandoffArgument} {HandoffParentPrefix}{Environment.ProcessId} {HandoffReadyEventPrefix}{readyEventName}";
        return Process.Start(new ProcessStartInfo { FileName = InstalledExePath, Arguments = args, WorkingDirectory = InstallFolder, UseShellExecute = true })
            ?? throw new InvalidOperationException("No se pudo iniciar la copia instalada de WinFlow.");
    }

    internal static Task EnsureShortcutsAsync() => Task.Run(() =>
    {
        CreateShortcut(StartupShortcutPath, InstalledExePath, string.Empty, InstalledExePath);
        CreateShortcut(StartMenuShortcutPath, InstalledExePath, string.Empty, InstalledIconPath, replaceExisting: true);
        CreateShortcut(UninstallShortcutPath, InstalledExePath, UninstallManager.UninstallArgument, InstalledExePath);
    });

    internal static void RemoveInstalledAppRegistration()
    {
        try { Registry.CurrentUser.DeleteSubKeyTree(UninstallRegistryPath, false); }
        catch (Exception ex) { Debug.WriteLine(ex); }
    }

    private static void CopyExecutableAtomically(string source)
    {
        string temporaryPath = InstalledExePath + ".tmp";
        try
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            File.Copy(source, temporaryPath, true);
            ReplaceInstalledExecutableWithRetry(temporaryPath);
        }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); } catch { }
        }
    }

    private static void ReplaceInstalledExecutableWithRetry(string temporaryPath)
    {
        for (int i = 0; i < 30; i++)
        {
            try
            {
                File.Move(temporaryPath, InstalledExePath, true);
                return;
            }
            catch (IOException) when (i < 29) { }
            catch (UnauthorizedAccessException) when (i < 29) { }

            Thread.Sleep(100);
        }
    }

    private static void WriteInstalledIcon()
    {
        string temporaryPath = InstalledIconPath + ".tmp";
        try
        {
            using Stream source = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedIconResourceName)
                ?? throw new InvalidOperationException("No se pudo cargar el icono de WinFlow.");
            using (FileStream destination = new(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
                source.CopyTo(destination);
            File.Move(temporaryPath, InstalledIconPath, true);
        }
        finally
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); } catch { }
        }
    }

    private static void RegisterInstalledApp()
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(UninstallRegistryPath, true) ?? throw new InvalidOperationException("No se pudo registrar WinFlow.");
        string version;
        try { version = FileVersionInfo.GetVersionInfo(InstalledExePath).ProductVersion ?? "1.0"; } catch { version = "1.0"; }
        int size = 0;
        try { size = (int)Math.Min(int.MaxValue, Math.Max(1, new FileInfo(InstalledExePath).Length / 1024)); } catch { }
        string uninstall = $"\"{InstalledExePath}\" {UninstallManager.UninstallArgument}";
        key.SetValue("DisplayName", "WinFlow");
        key.SetValue("DisplayVersion", version);
        key.SetValue("Publisher", "FEAR-Labs");
        key.SetValue("InstallLocation", InstallFolder);
        key.SetValue("DisplayIcon", $"{InstalledExePath},0");
        key.SetValue("UninstallString", uninstall);
        key.SetValue("QuietUninstallString", uninstall);
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        if (size > 0) key.SetValue("EstimatedSize", size, RegistryValueKind.DWord);
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string arguments, string iconPath, bool replaceExisting = false)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath) ?? InstallFolder);
        if (File.Exists(shortcutPath))
        {
            if (!replaceExisting) return;
            File.SetAttributes(shortcutPath, FileAttributes.Normal);
            File.Delete(shortcutPath);
        }

        Type type = Type.GetTypeFromCLSID(ShellLinkClsid, true)!;
        object instance = Activator.CreateInstance(type)!;
        try
        {
            IShellLinkW link = (IShellLinkW)instance;
            link.SetPath(targetPath);
            link.SetArguments(arguments);
            link.SetWorkingDirectory(Path.GetDirectoryName(targetPath) ?? InstallFolder);
            link.SetIconLocation(iconPath, 0);
            link.SetDescription("WinFlow");
            ((IPersistFile)link).Save(shortcutPath, true);
        }
        finally { if (Marshal.IsComObject(instance)) Marshal.FinalReleaseComObject(instance); }
    }

    private static string? ParseStringArg(string[] args, string prefix)
    {
        string? arg = args.FirstOrDefault(a => a.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        return arg is null ? null : arg[prefix.Length..];
    }

    private static int? ParseIntArg(string[] args, string prefix) => int.TryParse(ParseStringArg(args, prefix), out int value) ? value : null;

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxPath);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }
}
