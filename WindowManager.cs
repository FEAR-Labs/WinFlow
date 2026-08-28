using System.Drawing;
using System.Windows.Forms;

namespace WinFlow;

internal static class WindowManager
{
    private const int AnimationSteps = 7;
    private const int AnimationFrameMs = 9;

    private static int _operationVersion;

    internal static void CenterForegroundWindow()
    {
        IntPtr window = GetEligibleForegroundWindow();
        if (window == IntPtr.Zero) return;

        int operation = BeginOperation();
        if (NativeMethods.IsZoomed(window)) NativeMethods.ShowWindow(window, NativeMethods.SwRestore);
        _ = CenterSmoothAsync(window, Screen.FromHandle(window), operation);
    }

    internal static void ToggleMaximizeForegroundWindow()
    {
        IntPtr window = GetEligibleForegroundWindow();
        if (window == IntPtr.Zero) return;

        BeginOperation();
        if (!NativeMethods.IsZoomed(window)) NativeMethods.ShowWindow(window, NativeMethods.SwMaximize);
        else
        {
            NativeMethods.ShowWindow(window, NativeMethods.SwRestore);
            CenterInstant(window, Screen.FromHandle(window));
        }
    }

    private static int BeginOperation() => Interlocked.Increment(ref _operationVersion);

    private static IntPtr GetEligibleForegroundWindow()
    {
        IntPtr window = NativeMethods.GetForegroundWindow();
        if (!IsManipulableWindow(window) || IsUserMovingOrSizing(window)) return IntPtr.Zero;

        NativeMethods.GetWindowThreadProcessId(window, out uint processId);
        return processId == (uint)Environment.ProcessId ? IntPtr.Zero : window;
    }

    private static bool IsUserMovingOrSizing(IntPtr window)
    {
        uint threadId = NativeMethods.GetWindowThreadProcessId(window, out _);
        if (threadId == 0) return false;

        NativeMethods.GuiThreadInfo info = new()
        {
            CbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.GuiThreadInfo>()
        };

        if (!NativeMethods.GetGUIThreadInfo(threadId, ref info)) return false;
        return (info.Flags & NativeMethods.GuiInMoveSize) != 0 || info.HwndMoveSize == window;
    }

    private static bool IsManipulableWindow(IntPtr window)
    {
        if (window == IntPtr.Zero || !NativeMethods.IsWindowVisible(window) || NativeMethods.IsIconic(window))
            return false;

        if (NativeMethods.GetAncestor(window, NativeMethods.GaRoot) != window)
            return false;

        long style = NativeMethods.GetWindowLongPtr(window, NativeMethods.GwlStyle).ToInt64();
        long exStyle = NativeMethods.GetWindowLongPtr(window, NativeMethods.GwlExStyle).ToInt64();

        if ((style & NativeMethods.WsVisible) == 0 || (exStyle & NativeMethods.WsExToolWindow) != 0)
            return false;

        bool isPopup = (style & NativeMethods.WsPopup) != 0;
        if (isPopup)
        {
            bool resizable = (style & NativeMethods.WsThickFrame) != 0;
            bool looksLikeAppWindow = (style & NativeMethods.WsCaption) != 0 ||
                                      (style & (NativeMethods.WsMinimizeBox | NativeMethods.WsMaximizeBox)) != 0;
            if (!resizable || !looksLikeAppWindow)
                return false;
        }

        IntPtr owner = NativeMethods.GetWindow(window, NativeMethods.GwOwner);
        if (owner != IntPtr.Zero && NativeMethods.IsWindowVisible(owner))
            return false;

        return true;
    }

    private static async Task CenterSmoothAsync(IntPtr window, Screen screen, int operation)
    {
        bool moving = false;
        try
        {
            if (!NativeMethods.GetWindowRect(window, out NativeMethods.Rect rect)) return;
            Rectangle area = screen.WorkingArea;
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            (int targetX, int targetY) = CenteredPosition(area, width, height);
            int startX = rect.Left;
            int startY = rect.Top;
            uint flags = NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate;

            BeginUserMove(window);
            moving = true;

            for (int i = 1; i <= AnimationSteps; i++)
            {
                if (operation != Volatile.Read(ref _operationVersion)) return;

                double t = i / (double)AnimationSteps;
                double eased = 1.0 - Math.Pow(1.0 - t, 3.0);
                int x = (int)Math.Round(startX + (targetX - startX) * eased);
                int y = (int)Math.Round(startY + (targetY - startY) * eased);
                NativeMethods.SetWindowPos(window, IntPtr.Zero, x, y, 0, 0, flags);

                if (i < AnimationSteps)
                    await Task.Delay(AnimationFrameMs).ConfigureAwait(false);
            }
        }
        catch { }
        finally
        {
            if (moving) EndUserMove(window);
        }
    }

    private static void CenterInstant(IntPtr window, Screen screen)
    {
        if (!NativeMethods.GetWindowRect(window, out NativeMethods.Rect rect)) return;
        Rectangle area = screen.WorkingArea;
        (int x, int y) = CenteredPosition(area, rect.Right - rect.Left, rect.Bottom - rect.Top);

        BeginUserMove(window);
        try
        {
            NativeMethods.SetWindowPos(
                window,
                IntPtr.Zero,
                x,
                y,
                0,
                0,
                NativeMethods.SwpNoSize | NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate);
        }
        finally
        {
            EndUserMove(window);
        }
    }

    private static (int X, int Y) CenteredPosition(Rectangle area, int width, int height)
    {
        int x = area.Left + (area.Width - width) / 2;
        int y = area.Top + (area.Height - height) / 2;

        if (width > area.Width) x = area.Left;
        if (height > area.Height) y = area.Top;
        return (x, y);
    }

    private static void BeginUserMove(IntPtr window) =>
        NativeMethods.SendMessage(window, NativeMethods.WmEnterSizeMove, IntPtr.Zero, IntPtr.Zero);

    private static void EndUserMove(IntPtr window) =>
        NativeMethods.SendMessage(window, NativeMethods.WmExitSizeMove, IntPtr.Zero, IntPtr.Zero);
}
