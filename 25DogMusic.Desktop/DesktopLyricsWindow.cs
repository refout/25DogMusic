using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace _25DogMusic.Desktop;

/// <summary>桌面歌词：无边框置顶浮窗，可拖动，显示当前行与下一行。</summary>
public sealed class DesktopLyricsWindow : Window
{
    readonly TextBlock _current;
    readonly TextBlock _next;

    public DesktopLyricsWindow()
    {
        WindowDecorations = WindowDecorations.None;
        TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent };
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        CanResize = false;
        Width = 860;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.Manual;

        _current = new TextBlock
        {
            Text = " ",
            FontSize = 26,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            TextAlignment = TextAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1
        };

        _next = new TextBlock
        {
            Text = " ",
            FontSize = 15,
            Foreground = new SolidColorBrush(Color.Parse("#D9FFFFFF")),
            TextAlignment = TextAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
            Margin = new Thickness(0, 6, 0, 0)
        };

        var close = new Button
        {
            Content = "✕",
            FontSize = 12,
            Width = 22,
            Height = 22,
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(Color.Parse("#33FFFFFF")),
            Foreground = new SolidColorBrush(Color.Parse("#D9FFFFFF")),
            CornerRadius = new CornerRadius(11),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -4, -10, 0)
        };
        close.Click += (_, _) => Close();

        var stack = new StackPanel { Spacing = 0 };
        stack.Children.Add(_current);
        stack.Children.Add(_next);

        var content = new Grid();
        content.Children.Add(stack);
        content.Children.Add(close);

        var root = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#B3000000")),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(30, 16, 30, 16),
            Child = content
        };
        root.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
        };

        Content = root;
    }

    /// <summary>更新当前行与下一行歌词。</summary>
    public void SetLines(string current, string next)
    {
        _current.Text = string.IsNullOrWhiteSpace(current) ? "♪" : current;
        _next.Text = string.IsNullOrWhiteSpace(next) ? " " : next;
    }

    /// <summary>显示到主显示器底部中间上方一点的位置。</summary>
    public void ShowBottomCenter()
    {
        Show();
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null) return;

        var area = screen.WorkingArea;
        var width = (int)(Width * screen.Scaling);
        var x = area.X + Math.Max(0, (area.Width - width) / 2);
        var y = area.Y + Math.Max(0, area.Height - 180);
        Position = new PixelPoint(x, y);
    }
}
