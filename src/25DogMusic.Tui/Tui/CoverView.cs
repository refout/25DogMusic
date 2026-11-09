using Terminal.Gui;
using DRect = System.Drawing.Rectangle;

namespace FnMusic.Tui;

/// <summary>
/// 用真彩色半块字符把封面画到终端里。
/// 像素数据会缓存，尺寸或图片变化时才重建，保证切换时不卡顿。
/// </summary>
public sealed class CoverView : View
{
    RgbaImage? _image;
    byte[]? _buffer;
    int _bufferWidth;
    int _bufferHeight;

    public ColorTheme Theme { get; private set; } = ColorTheme.Default;
    public RgbaImage? Image => _image;

    public CoverView()
    {
        CanFocus = false;
        Width = Dim.Fill();
        Height = 12;
    }

    public ColorTheme SetJpeg(byte[]? data)
    {
        var next = data is null ? null : RgbaImage.TryDecode(data);
        _image = next;

        InvalidateBuffer();
        Theme = next is null ? ColorTheme.Default : ColorTheme.FromImage(next);
        SetNeedsDraw();
        return Theme;
    }

    void InvalidateBuffer()
    {
        _buffer = null;
        _bufferWidth = 0;
        _bufferHeight = 0;
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        Paint(Viewport);
        return true;
    }

    void Paint(DRect bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        if (Application.Driver is not { } driver) return;

        var origin = ViewportToScreen(new System.Drawing.Point(0, 0));

        // 留白处直接透出壁纸，避免出现矩形色块
        for (var row = 0; row < bounds.Height; row++)
        {
            var backdrop = Wallpaper.Shared.Blend(origin.X, origin.Y + row, Theme, 0f);
            driver.SetAttribute(new Terminal.Gui.Attribute(backdrop, backdrop));
            driver.Move(origin.X, origin.Y + row);
            driver.AddStr(new string(' ', bounds.Width));
        }

        if (_image is null) return;

        var cellW = Math.Min(bounds.Width, bounds.Height * 2);
        var cellH = Math.Max(1, cellW / 2);
        EnsureBuffer(cellW, cellH * 2);

        if (_buffer is null) return;

        var padLeft = (bounds.Width - cellW) / 2;
        var padTop = Math.Max(0, (bounds.Height - cellH) / 2);

        for (var row = 0; row < cellH; row++)
        {
            for (var col = 0; col < cellW; col++)
            {
                var top = ((row * 2) * cellW + col) * 3;
                var bottomRow = Math.Min(row * 2 + 1, _bufferHeight - 1);
                var bottom = (bottomRow * cellW + col) * 3;

                var fg = Scale(_buffer, top, 1f);
                var bg = Scale(_buffer, bottom, 1f);

                driver.SetAttribute(new Terminal.Gui.Attribute(fg, bg));
                driver.Move(origin.X + padLeft + col, origin.Y + padTop + row);
                driver.AddRune('▀');
            }
        }
    }

    static Terminal.Gui.Color Scale(byte[] buffer, int index, float gain)
    {
        return new Terminal.Gui.Color(
            (int)Math.Min(255, buffer[index] * gain),
            (int)Math.Min(255, buffer[index + 1] * gain),
            (int)Math.Min(255, buffer[index + 2] * gain),
            255);
    }

    void EnsureBuffer(int width, int height)
    {
        if (_image is null) return;
        if (_buffer is not null && _bufferWidth == width && _bufferHeight == height) return;

        var resized = _image.Resize(width, height);
        _buffer = resized.ToRgb();
        _bufferWidth = width;
        _bufferHeight = height;
    }
}
