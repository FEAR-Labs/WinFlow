using System.IO;
using System.Windows.Forms;

namespace WinFlow;

internal sealed class TrayController : NativeWindow, IDisposable
{
    private static readonly (int Id, Keys Key, Action Handler)[] Hotkeys =
    {
        (0x5746, Keys.C, WindowManager.CenterForegroundWindow),
        (0x5747, Keys.V, WindowManager.ToggleMaximizeForegroundWindow)
    };

    private readonly Dictionary<int, Action> _hotkeyHandlers = new();
    private readonly HashSet<int> _registeredIds = new();
    private readonly System.Windows.Forms.Timer _altReleaseTimer;
    private readonly NotifyIcon _trayIcon;
    private readonly System.Drawing.Icon _icon;
    private TrayMenuWindow? _menuWindow;
    private bool _handoffPrepared;
    private bool _disposed;

    internal TrayController()
    {
        CreateHandle(new CreateParams());
        _altReleaseTimer = new System.Windows.Forms.Timer { Interval = 20 };
        _altReleaseTimer.Tick += (_, _) => DismissAltMenuWhenReleased();
        _icon = LoadApplicationIcon();
        _trayIcon = new NotifyIcon { Icon = _icon, Text = "WinFlow", Visible = true };
        _trayIcon.MouseUp += (_, e) => { if (e.Button == MouseButtons.Right) ShowMenu(); };
        RegisterHotkeys();
    }

    private void RegisterHotkeys()
    {
        foreach ((int id, Keys key, Action handler) in Hotkeys)
        {
            uint modifiers = NativeMethods.ModAlt | NativeMethods.ModNoRepeat;

            if (!NativeMethods.RegisterHotKey(Handle, id, modifiers, (uint)key)) continue;
            _registeredIds.Add(id);
            _hotkeyHandlers[id] = handler;
        }
        if (_registeredIds.Count != Hotkeys.Length)
            _trayIcon.Text = "WinFlow - algunos atajos no están disponibles";
    }

    private void UnregisterHotkeys()
    {
        foreach (int id in _registeredIds) NativeMethods.UnregisterHotKey(Handle, id);
        _registeredIds.Clear();
        _hotkeyHandlers.Clear();
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeMethods.WmHotkey && _hotkeyHandlers.TryGetValue(message.WParam.ToInt32(), out Action? handler))
        {
            _menuWindow?.Hide();
            handler();
            _altReleaseTimer.Start();
        }
        base.WndProc(ref message);
    }

    private void DismissAltMenuWhenReleased()
    {
        if ((Control.ModifierKeys & Keys.Alt) == Keys.Alt) return;
        _altReleaseTimer.Stop();
        try { SendKeys.SendWait("{ESC}"); } catch { }
    }

    private void ShowMenu()
    {
        try
        {
            _menuWindow ??= new TrayMenuWindow();
            _menuWindow.PrepareForShow();
            if (_menuWindow.IsVisible) _menuWindow.Activate();
            else
            {
                _menuWindow.Show();
                _menuWindow.Activate();
            }
        }
        catch { }
    }

    private static System.Drawing.Icon LoadApplicationIcon()
    {
        try
        {
            string? path = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                using System.Drawing.Icon? icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
                if (icon is not null) return (System.Drawing.Icon)icon.Clone();
            }
        }
        catch { }
        return (System.Drawing.Icon)System.Drawing.SystemIcons.Application.Clone();
    }

    internal void PrepareForHandoff()
    {
        if (_disposed || _handoffPrepared) return;
        _handoffPrepared = true;
        _altReleaseTimer.Stop();
        _menuWindow?.Hide();
        UnregisterHotkeys();
    }

    internal void CancelHandoffPreparation()
    {
        if (_disposed || !_handoffPrepared) return;
        _handoffPrepared = false;
        RegisterHotkeys();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnregisterHotkeys();
        _altReleaseTimer.Stop();
        _altReleaseTimer.Dispose();
        _menuWindow?.Close();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _icon.Dispose();
        DestroyHandle();
    }
}
