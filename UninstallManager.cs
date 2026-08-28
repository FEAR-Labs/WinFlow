using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace WinFlow;

internal static class UninstallManager
{
    internal const string UninstallArgument = "--uninstall";
    internal const string WorkerPrefix = "--uninstall-worker";
    private const string ParentPidPrefix = "--uninstall-parent-pid=";

    internal static bool IsUninstallRequest(string[] args) => args.Any(a => string.Equals(a, UninstallArgument, StringComparison.OrdinalIgnoreCase));
    internal static bool IsWorkerRequest(string[] args) => args.Any(a => a.StartsWith(WorkerPrefix, StringComparison.OrdinalIgnoreCase));

    internal static int? GetWorkerParentPid(string[] args)
    {
        string? arg = args.FirstOrDefault(a => a.StartsWith(ParentPidPrefix, StringComparison.OrdinalIgnoreCase));
        return arg is not null && int.TryParse(arg[ParentPidPrefix.Length..], out int pid) ? pid : null;
    }

    internal static void StartWorker()
    {
        string source = Environment.ProcessPath ?? throw new InvalidOperationException("No se pudo localizar WinFlow.exe.");
        string folder = Path.Combine(Path.GetTempPath(), "WinFlow-Uninstall-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string worker = Path.Combine(folder, "WinFlowCleanup.exe");
        File.Copy(source, worker, true);

        if (Process.Start(new ProcessStartInfo
        {
            FileName = worker,
            Arguments = $"{WorkerPrefix} {ParentPidPrefix}{Environment.ProcessId}",
            WorkingDirectory = folder,
            UseShellExecute = true
        }) is null)
        {
            throw new InvalidOperationException("No se pudo iniciar el limpiador de WinFlow.");
        }
    }

    internal static async Task RunWorkerAsync(int? parentPid, Action<int, string, string?> progress)
    {
        progress(10, "Preparando desinstalación…", "Se eliminarán archivos, accesos y datos de WinFlow.");
        await WaitForProcessExitAsync(parentPid).ConfigureAwait(false);

        progress(30, "Cerrando WinFlow…", "Deteniendo procesos activos de la aplicación.");
        await StopInstalledProcessesAsync().ConfigureAwait(false);
        await WaitForInstalledExecutableReleaseAsync().ConfigureAwait(false);

        progress(55, "Eliminando archivos…", "Borrando la instalación de WinFlow.");
        if (Directory.Exists(InstallManager.InstallFolder))
        {
            NormalizeAttributes(InstallManager.InstallFolder);
            Directory.Delete(InstallManager.InstallFolder, true);
        }

        progress(75, "Eliminando accesos…", "Quitando WinFlow del inicio y del menú Inicio de Windows.");
        DeleteIfExists(InstallManager.StartupShortcutPath);
        DeleteIfExists(InstallManager.StartMenuShortcutPath);

        progress(90, "Actualizando Windows…", "Quitando WinFlow de Aplicaciones instaladas.");
        InstallManager.RemoveInstalledAppRegistration();

        progress(100, "WinFlow desinstalado", "Se eliminaron los archivos, accesos y datos de WinFlow.");
    }

    internal static void ScheduleSelfDelete()
    {
        string? folder = Path.GetDirectoryName(Environment.ProcessPath);
        if (string.IsNullOrWhiteSpace(folder)) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
            Arguments = $"/d /c ping 127.0.0.1 -n 3 >nul & rmdir /s /q \"{folder.Replace("\"", "\"\"")}\"",
            WorkingDirectory = Path.GetTempPath(),
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    private static async Task WaitForProcessExitAsync(int? pid)
    {
        if (pid is null || pid <= 0) return;

        try
        {
            using Process process = Process.GetProcessById(pid.Value);
            await process.WaitForExitAsync().ConfigureAwait(false);
        }
        catch (ArgumentException) { }
        catch (InvalidOperationException) { }
    }

    private static async Task StopInstalledProcessesAsync()
    {
        foreach (Process process in Process.GetProcessesByName("WinFlow"))
        using (process)
        {
            try
            {
                if (process.HasExited) continue;
                process.Kill(true);
                await process.WaitForExitAsync().ConfigureAwait(false);
            }
            catch (InvalidOperationException) { }
            catch (Win32Exception ex)
            {
                throw new InvalidOperationException("No se pudo cerrar WinFlow antes de desinstalarlo.", ex);
            }
        }
    }

    private static async Task WaitForInstalledExecutableReleaseAsync()
    {
        if (!File.Exists(InstallManager.InstalledExePath)) return;

        for (int i = 0; i < 50; i++)
        {
            try
            {
                using FileStream stream = new(InstallManager.InstalledExePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return;
            }
            catch (IOException) when (i < 49) { }
            catch (UnauthorizedAccessException) when (i < 49) { }

            await Task.Delay(100).ConfigureAwait(false);
        }
    }

    private static void NormalizeAttributes(string folder)
    {
        foreach (string file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            try { File.SetAttributes(file, FileAttributes.Normal); }
            catch { }
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (!File.Exists(path)) return;
        File.SetAttributes(path, FileAttributes.Normal);
        File.Delete(path);
    }
}
