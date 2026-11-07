using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using TG = Terminal.Gui;

namespace FnMusic.Tui;

/// <summary>
/// 把封面模糊、压暗后铺满整个窗口，作为全局壁纸。
/// 以“字符格”为分辨率缓存，所有面板用 Blend 取色，避免各自铺纯色挡住壁纸。
/// </summary>
public sealed class Wallpaper
{
    public static Wallpaper Shared { get; } = new();

    Image<Rgba32>? _source;
    byte[]? _cells;
    int _width;
    int _height;

    public void Update(Image<Rgba32>? image, int width, int height, ColorTheme theme)
    {
        if (width <= 0 || height <= 0) return;
        if (_cells is not null && _source == image && _width == width && _height == height) return;

        _source = image;
        _width = width;
        _height = height;

        if (image is null)
        {
            _cells = null;
            return;
        }

        using var resized = image.Clone();
        resized.Mutate(ctx => ctx.Resize(width, height));

        var cells = new byte[width * height * 3];
        var index = 0;
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = resized[x, y];
                cells[index++] = pixel.R;
                cells[index++] = pixel.G;
                cells[index++] = pixel.B;
            }
        }

        Blur(cells, width, height, 2);
        Blur(cells, width, height, 2);

        // 压暗并略向主题底色靠拢，保证前景文字可读
        for (var i = 0; i < cells.Length; i += 3)
        {
            cells[i] = Darken(cells[i], theme.Background.R);
            cells[i + 1] = Darken(cells[i + 1], theme.Background.G);
            cells[i + 2] = Darken(cells[i + 2], theme.Background.B);
        }

        _cells = cells;
    }

    static byte Darken(byte value, byte floor)
    {
        var scaled = (int)(value * 0.34);
        var blended = (int)(floor * 0.45);
        return (byte)Math.Clamp(scaled + blended, 0, 255);
    }

    public TG.Color Cell(int x, int y)
    {
        if (_cells is null || x < 0 || y < 0 || x >= _width || y >= _height)
        {
            return new TG.Color(16, 18, 24, 255);
        }

        var index = (y * _width + x) * 3;
        return new TG.Color(_cells[index], _cells[index + 1], _cells[index + 2], 255);
    }

    /// <summary>把壁纸颜色按 scrim 比例与面板底色混合。</summary>
    public TG.Color Blend(int x, int y, ColorTheme theme, float scrim)
    {
        var cell = Cell(x, y);
        return new TG.Color(
            (int)Math.Round(cell.R * (1 - scrim) + theme.Background.R * scrim),
            (int)Math.Round(cell.G * (1 - scrim) + theme.Background.G * scrim),
            (int)Math.Round(cell.B * (1 - scrim) + theme.Background.B * scrim),
            255);
    }

    static void Blur(byte[] cells, int width, int height, int radius)
    {
        var copy = (byte[])cells.Clone();

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                int sumR = 0, sumG = 0, sumB = 0, count = 0;

                for (var dy = -radius; dy <= radius; dy++)
                {
                    var ny = y + dy;
                    if (ny < 0 || ny >= height) continue;

                    for (var dx = -radius; dx <= radius; dx++)
                    {
                        var nx = x + dx;
                        if (nx < 0 || nx >= width) continue;

                        var srcIndex = (ny * width + nx) * 3;
                        sumR += copy[srcIndex];
                        sumG += copy[srcIndex + 1];
                        sumB += copy[srcIndex + 2];
                        count++;
                    }
                }

                var index = (y * width + x) * 3;
                cells[index] = (byte)(sumR / count);
                cells[index + 1] = (byte)(sumG / count);
                cells[index + 2] = (byte)(sumB / count);
            }
        }
    }
}
