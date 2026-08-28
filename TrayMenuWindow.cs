using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Forms = System.Windows.Forms;
using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using FontFamily = System.Windows.Media.FontFamily;
using WpfApplication = System.Windows.Application;
using WpfCursors = System.Windows.Input.Cursors;

namespace WinFlow;

internal sealed class TrayMenuWindow : Window
{
    private static readonly SolidColorBrush PanelBrush = Frozen(Colors.Black);
    private static readonly SolidColorBrush TextBrush = Frozen(Colors.White);
    private static readonly SolidColorBrush MutedBrush = Frozen(Colors.White);
    private static readonly SolidColorBrush DividerBrush = Frozen(Color.FromArgb(34, 255, 255, 255));
    private static readonly SolidColorBrush ButtonBrush = Frozen(Colors.Black);
    private static readonly SolidColorBrush ButtonHoverBrush = Frozen(Color.FromRgb(17, 17, 17));
    private static readonly SolidColorBrush ButtonBorderBrush = Frozen(Color.FromRgb(61, 66, 74));
    private static readonly FontFamily UiFont = new("Segoe UI Variable Text, Segoe UI");
    private static readonly FontFamily DisplayFont = new("Segoe UI Variable Display, Segoe UI");

    internal TrayMenuWindow()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Topmost = true;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;

        Content = BuildContent();
        Loaded += (_, _) => PositionNearTray();
        Deactivated += (_, _) => Hide();
    }

    internal void PrepareForShow() => PositionNearTray();

    private UIElement BuildContent()
    {
        var panel = new Border
        {
            Width = 330,
            Padding = new Thickness(23, 19, 23, 18),
            CornerRadius = new CornerRadius(22),
            Background = PanelBrush,
            BorderBrush = DividerBrush,
            BorderThickness = new Thickness(1),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 22,
                ShadowDepth = 5,
                Opacity = 0.25,
                Color = Colors.Black
            }
        };

        var root = new StackPanel();
        panel.Child = root;

        root.Children.Add(new TextBlock
        {
            Text = "WinFlow",
            Foreground = TextBrush,
            FontFamily = DisplayFont,
            FontSize = 21,
            FontWeight = FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 18)
        });

        root.Children.Add(CreateShortcutRow("Centrar ventana", "Alt + C"));
        root.Children.Add(CreateDivider());
        root.Children.Add(CreateShortcutRow("Maximizar ventana", "Alt + V"));
        root.Children.Add(CreateFooter());

        return panel;
    }

    private static UIElement CreateDivider() => new Border
    {
        Height = 1,
        Background = DividerBrush,
        Margin = new Thickness(0, 10, 0, 10)
    };

    private static UIElement CreateShortcutRow(string action, string shortcut)
    {
        var grid = new Grid { Margin = new Thickness(4, 5, 4, 5) };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(new TextBlock
        {
            Text = action,
            Foreground = TextBrush,
            FontFamily = UiFont,
            FontSize = 15.5,
            VerticalAlignment = VerticalAlignment.Center
        });

        var key = new TextBlock
        {
            Text = shortcut,
            Foreground = MutedBrush,
            FontFamily = UiFont,
            FontSize = 14.5,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(16, 0, 0, 0)
        };
        Grid.SetColumn(key, 1);
        grid.Children.Add(key);

        return grid;
    }

    private UIElement CreateFooter()
    {
        var footer = new Grid { Margin = new Thickness(0, 20, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition());
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var exitButton = new Border
        {
            Background = ButtonBrush,
            BorderBrush = ButtonBorderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(12, 8, 12, 8),
            Cursor = WpfCursors.Hand,
            Child = new TextBlock
            {
                Text = "Salir",
                Foreground = TextBrush,
                FontFamily = UiFont,
                FontSize = 14.5,
                FontWeight = FontWeights.SemiBold
            }
        };
        exitButton.MouseEnter += (_, _) => exitButton.Background = ButtonHoverBrush;
        exitButton.MouseLeave += (_, _) => exitButton.Background = ButtonBrush;
        exitButton.MouseLeftButtonUp += (_, _) => WpfApplication.Current.Shutdown();
        Grid.SetColumn(exitButton, 1);
        footer.Children.Add(exitButton);

        return footer;
    }

    private void PositionNearTray()
    {
        System.Drawing.Point cursor = Forms.Cursor.Position;
        Forms.Screen screen = Forms.Screen.FromPoint(cursor);
        PresentationSource? source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null)
            return;

        Matrix fromDevice = source.CompositionTarget.TransformFromDevice;
        Point cursorDip = fromDevice.Transform(new Point(cursor.X, cursor.Y));
        Point workTopLeft = fromDevice.Transform(new Point(screen.WorkingArea.Left, screen.WorkingArea.Top));
        Point workBottomRight = fromDevice.Transform(new Point(screen.WorkingArea.Right, screen.WorkingArea.Bottom));

        double width = ActualWidth > 0 ? ActualWidth : 330;
        double height = ActualHeight > 0 ? ActualHeight : 190;
        double left = cursorDip.X - width + 12;
        double top = cursorDip.Y - height - 12;

        Left = Math.Max(workTopLeft.X + 10, Math.Min(left, workBottomRight.X - width - 10));
        Top = Math.Max(workTopLeft.Y + 10, Math.Min(top, workBottomRight.Y - height - 10));
    }

    private static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
