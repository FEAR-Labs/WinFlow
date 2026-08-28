using System.Windows;
using System.Windows.Threading;
using WpfBorder = System.Windows.Controls.Border;
using WpfProgressBar = System.Windows.Controls.ProgressBar;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace WinFlow;

internal sealed class OperationProgressWindow : Window
{
    private readonly WpfProgressBar _progressBar;
    private readonly WpfTextBlock _statusText;
    private readonly WpfTextBlock _detailText;

    internal OperationProgressWindow(string title, string initialStatus, string initialDetail)
    {
        Title = title;
        Width = 430;
        Height = 210;
        ResizeMode = ResizeMode.NoResize;
        WindowStyle = WindowStyle.None;
        WindowStartupLocation = WindowStartupLocation.Manual;
        ShowInTaskbar = true;
        Topmost = true;
        Background = WpfBrushes.Transparent;
        AllowsTransparency = true;

        var panel = new WpfBorder
        {
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(26),
            Background = WpfBrushes.Black,
            BorderBrush = new WpfSolidColorBrush(WpfColor.FromRgb(61, 66, 74)),
            BorderThickness = new Thickness(1)
        };

        var root = new WpfStackPanel();
        panel.Child = root;

        root.Children.Add(new WpfTextBlock
        {
            Text = "WinFlow",
            Foreground = WpfBrushes.White,
            FontFamily = new WpfFontFamily("Segoe UI Variable Display, Segoe UI"),
            FontSize = 24,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        _statusText = new WpfTextBlock
        {
            Text = initialStatus,
            Foreground = WpfBrushes.White,
            FontFamily = new WpfFontFamily("Segoe UI Variable Text, Segoe UI"),
            FontSize = 15,
            Margin = new Thickness(0, 0, 0, 14)
        };
        root.Children.Add(_statusText);

        _progressBar = new WpfProgressBar
        {
            Minimum = 0,
            Maximum = 100,
            Value = 5,
            Height = 8,
            IsIndeterminate = false,
            Foreground = new WpfSolidColorBrush(WpfColor.FromRgb(0, 120, 212)),
            Background = new WpfSolidColorBrush(WpfColor.FromRgb(24, 24, 24)),
            Margin = new Thickness(0, 0, 0, 13)
        };
        root.Children.Add(_progressBar);

        _detailText = new WpfTextBlock
        {
            Text = initialDetail,
            Foreground = WpfBrushes.White,
            FontFamily = new WpfFontFamily("Segoe UI Variable Text, Segoe UI"),
            FontSize = 13.5,
            TextWrapping = TextWrapping.Wrap
        };
        root.Children.Add(_detailText);

        Content = panel;
        Loaded += (_, _) => CenterOnPrimaryMonitor();
    }

    private void CenterOnPrimaryMonitor()
    {
        Rect workArea = SystemParameters.WorkArea;
        Left = workArea.Left + Math.Max(0, (workArea.Width - ActualWidth) / 2);
        Top = workArea.Top + Math.Max(0, (workArea.Height - ActualHeight) / 2);
    }

    internal void Report(int percent, string status, string? detail = null)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(new Action(() => Report(percent, status, detail)));
            return;
        }

        _progressBar.Value = Math.Clamp(percent, 0, 100);
        _statusText.Text = status;
        if (!string.IsNullOrWhiteSpace(detail))
            _detailText.Text = detail;
    }

    internal void CompleteAndCloseSoon(string status, string detail)
    {
        Report(100, status, detail);
        CloseSoon();
    }

    internal void ShowFailure(string status, string detail)
    {
        Report(100, status, detail);
    }

    internal void CloseSoon(double seconds = 2.2)
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Close();
        };
        timer.Start();
    }
}
