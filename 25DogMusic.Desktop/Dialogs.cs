using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace _25DogMusic.Desktop;

/// <summary>轻量的输入/确认对话框，避免为了两个小操作引入完整弹窗页。</summary>
public static class Dialogs
{
    static Window BaseWindow(string title, string message)
    {
        var win = new Window
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.Parse("#141418"))
        };

        var stack = new StackPanel { Spacing = 14, Margin = new Thickness(22) };
        if (!string.IsNullOrEmpty(message))
        {
            stack.Children.Add(new TextBlock
            {
                Text = message,
                FontSize = 13,
                Foreground = new SolidColorBrush(Color.Parse("#A8A8B2")),
                TextWrapping = TextWrapping.Wrap
            });
        }
        win.Content = stack;
        return win;
    }

    static Button PrimaryButton(string text)
    {
        var button = new Button
        {
            Content = text,
            Height = 34,
            Padding = new Thickness(18, 0),
            CornerRadius = new CornerRadius(17),
            Background = new SolidColorBrush(Color.Parse("#FF375F")),
            Foreground = Brushes.White,
            FontSize = 13,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        return button;
    }

    static Button GhostButton(string text)
    {
        var button = new Button
        {
            Content = text,
            Height = 34,
            Padding = new Thickness(18, 0),
            CornerRadius = new CornerRadius(17),
            Background = new SolidColorBrush(Color.Parse("#1AFFFFFF")),
            Foreground = new SolidColorBrush(Color.Parse("#E8E8F0")),
            FontSize = 13,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        return button;
    }

    static Control ButtonsRow(Button cancel, Button confirm)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        row.Children.Add(cancel);
        row.Children.Add(confirm);
        return row;
    }

    /// <summary>文本输入对话框，取消返回 null。</summary>
    public static async Task<string?> PromptAsync(Window owner, string title, string initial, string placeholder = "")
    {
        var win = BaseWindow(title, string.Empty);
        var stack = (StackPanel)win.Content!;

        var box = new TextBox
        {
            Text = initial,
            PlaceholderText = placeholder,
            FontSize = 13.5,
            Height = 38,
            VerticalContentAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(Color.Parse("#1AFFFFFF")),
            Foreground = new SolidColorBrush(Color.Parse("#F2F2F5")),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(12, 0)
        };
        stack.Children.Add(box);

        string? result = null;
        var cancel = GhostButton("取消");
        var confirm = PrimaryButton("确定");
        cancel.Click += (_, _) => win.Close();
        confirm.Click += (_, _) => { result = box.Text; win.Close(); };
        box.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) { result = box.Text; win.Close(); }
        };

        stack.Children.Add(ButtonsRow(cancel, confirm));
        win.Opened += (_, _) => box.Focus();
        await win.ShowDialog(owner);
        return result;
    }

    /// <summary>确认对话框。</summary>
    public static async Task<bool> ConfirmAsync(Window owner, string title, string message, string confirmText = "确定")
    {
        var win = BaseWindow(title, message);
        var stack = (StackPanel)win.Content!;

        var cancelled = true;
        var cancel = GhostButton("取消");
        var confirm = PrimaryButton(confirmText);
        cancel.Click += (_, _) => win.Close();
        confirm.Click += (_, _) => { cancelled = false; win.Close(); };

        stack.Children.Add(ButtonsRow(cancel, confirm));
        await win.ShowDialog(owner);
        return !cancelled;
    }
}
