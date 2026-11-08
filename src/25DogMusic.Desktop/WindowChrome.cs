using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Media;

namespace _25DogMusic.Desktop;

/// <summary>Windows 标题栏明暗、圆角与底色设置，避免系统标题栏与界面割裂。</summary>
internal static class WindowChrome
{
    const int UseImmersiveDarkMode = 20;
    const int WindowCornerPreference = 33;
    const int BorderColor = 34;
    const int CaptionColor = 35;
    const int TextColor = 36;
    const int CornerRound = 2;

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void ApplyTitleBar(Window window, bool dark, Color? background = null)
    {
        if (!OperatingSystem.IsWindows()) return;

        var handle = window.TryGetPlatformHandle()?.Handle;
        if (handle is null || handle == IntPtr.Zero) return;

        var value = dark ? 1 : 0;
        try
        {
            DwmSetWindowAttribute(handle.Value, UseImmersiveDarkMode, ref value, sizeof(int));
        }
        catch
        {
            // 旧版 Windows 不支持该属性时忽略
        }

        // 窗口外框统一为圆角，与卡片、按钮的圆角风格保持一致
        var corner = CornerRound;
        try
        {
            DwmSetWindowAttribute(handle.Value, WindowCornerPreference, ref corner, sizeof(int));
        }
        catch
        {
            // Windows 11 之前没有圆角偏好属性
        }

        if (background is { } bg)
        {
            // 标题栏、边框取界面底色，标题文字按主题取黑/白，保证整窗一致
            var caption = ToColorRef(bg);
            var border = caption;
            var text = ToColorRef(Color.Parse(dark ? "#F2F2F5" : "#1C1C1E"));

            try
            {
                DwmSetWindowAttribute(handle.Value, CaptionColor, ref caption, sizeof(int));
                DwmSetWindowAttribute(handle.Value, BorderColor, ref border, sizeof(int));
                DwmSetWindowAttribute(handle.Value, TextColor, ref text, sizeof(int));
            }
            catch
            {
                // Windows 11 22H2 之前不支持标题栏着色
            }
        }
    }

    /// <summary>Win32 COLORREF 为 0x00BBGGRR（低字节是红色）。</summary>
    static int ToColorRef(Color color) => color.R | (color.G << 8) | (color.B << 16);
}
