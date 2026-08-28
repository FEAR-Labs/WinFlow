using System.Diagnostics;
using System.Threading;

namespace WinFlow;

public partial class App : System.Windows.Application
{
    private TrayController? _tray;
    private OperationProgressWindow? _progress;
    private Mutex? _instanceMutex;
    private bool _ownsInstance;

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
        if (UninstallManager.IsWorkerRequest(e.Args)) { _ = RunUninstallWorkerAsync(e.Args); return; }
        if (UninstallManager.IsUninstallRequest(e.Args)) { UninstallManager.StartWorker(); Shutdown(); return; }
        _instanceMutex = new Mutex(false, @"Local\WinFlow.SingleInstance");
        if (!AcquireInstance()) { Shutdown(); return; }
        _tray = new TrayController();
        if (!InstallManager.IsRunningFromInstalledLocation())
        {
            ShowProgress(5, "Preparando instalación…", "WinFlow seguirá funcionando mientras se instala.");
            _ = InstallAndSwitchAsync();
        }
        else if (InstallManager.IsHandoffRequest(e.Args))
        {
            ShowProgress(82, "WinFlow instalado…", "Liberando la copia temporal.");
            SignalHandoffReady(InstallManager.GetHandoffEventName(e.Args));
            _ = CompleteHandoffAsync(InstallManager.GetHandoffParentPid(e.Args));
        }
        else _ = InstallManager.EnsureShortcutsAsync();
    }

    private async Task InstallAndSwitchAsync()
    {
        Process? child = null;
        try
        {
            await InstallManager.InstallAsync(ReportProgress);
            _tray?.PrepareForHandoff();
            ReleaseInstance();
            string eventName = $@"Local\WinFlow.HandoffUi.{Environment.ProcessId}.{Guid.NewGuid():N}";
            using var ready = new EventWaitHandle(false, EventResetMode.ManualReset, eventName);
            child = InstallManager.StartInstalledCopy(eventName);
            bool signaled = await Task.Run(() => ready.WaitOne(TimeSpan.FromSeconds(10)));
            if (!signaled)
            {
                try
                {
                    child.Kill(true);
                    await child.WaitForExitAsync();
                }
                catch { }
                throw new TimeoutException("La copia instalada no pudo iniciar a tiempo.");
            }
            await Dispatcher.InvokeAsync(Shutdown);
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                AcquireInstance();
                _tray?.CancelHandoffPreparation();
                ShowFailure(ex);
            });
        }
    }

    private async Task CompleteHandoffAsync(int? parentPid)
    {
        try
        {
            if (parentPid is > 0)
            {
                try { using Process parent = Process.GetProcessById(parentPid.Value); await parent.WaitForExitAsync(); } catch (ArgumentException) { } catch (InvalidOperationException) { }
            }
            await InstallManager.EnsureShortcutsAsync();
            await Dispatcher.InvokeAsync(() => _progress?.CompleteAndCloseSoon("WinFlow está listo", "Ya puedes eliminar la copia temporal."));
        }
        catch (Exception ex) { ShowFailure(ex); }
    }

    private async Task RunUninstallWorkerAsync(string[] args)
    {
        _progress = new OperationProgressWindow("Desinstalando WinFlow", "Preparando desinstalación…", "Se eliminarán archivos, accesos y datos de WinFlow.");
        _progress.Closed += (_, _) => { UninstallManager.ScheduleSelfDelete(); Shutdown(); };
        _progress.Show();
        try
        {
            await UninstallManager.RunWorkerAsync(UninstallManager.GetWorkerParentPid(args), ReportProgress);
            await Dispatcher.InvokeAsync(() => _progress?.CompleteAndCloseSoon("WinFlow desinstalado", "Se eliminaron archivos, accesos y datos de WinFlow."));
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() => { _progress?.ShowFailure("No se pudo completar la desinstalación", ex.Message); _progress?.CloseSoon(5); });
        }
    }

    private static void SignalHandoffReady(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        try { using EventWaitHandle handle = EventWaitHandle.OpenExisting(name); handle.Set(); } catch { }
    }

    private void ShowProgress(int percent, string status, string detail)
    {
        _progress ??= new OperationProgressWindow("Instalando WinFlow", status, detail);
        _progress.Report(percent, status, detail);
        if (!_progress.IsVisible) _progress.Show();
    }

    private void ReportProgress(int percent, string status, string? detail) => Dispatcher.BeginInvoke(new Action(() => ShowProgress(percent, status, detail ?? string.Empty)));

    private void ShowFailure(Exception ex)
    {
        ShowProgress(100, "No se pudo completar la operación", ex.Message);
        _progress?.ShowFailure("No se pudo completar la operación", ex.Message);
    }

    private bool AcquireInstance()
    {
        if (_instanceMutex is null || _ownsInstance) return _ownsInstance;
        try { _ownsInstance = _instanceMutex.WaitOne(0); }
        catch (AbandonedMutexException) { _ownsInstance = true; }
        return _ownsInstance;
    }

    private void ReleaseInstance()
    {
        if (!_ownsInstance || _instanceMutex is null) return;
        _instanceMutex.ReleaseMutex();
        _ownsInstance = false;
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _tray?.Dispose();
        ReleaseInstance();
        _instanceMutex?.Dispose();
        _instanceMutex = null;
        base.OnExit(e);
    }
}
