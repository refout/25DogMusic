using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TG = Terminal.Gui;

namespace FnMusic.Tui;

/// <summary>当前主题的全局入口，自绘视图直接取用，避免层层传参。</summary>
public static class ThemeProvider
{
    public static ColorTheme Current { get; set; } = ColorTheme.Default;
}

/// <summary>从封面提取的主题色，用来统一整个界面的背景/文字/高亮。</summary>
public sealed record ColorTheme(
    TG.Color Background,
    TG.Color Panel,
    TG.Color Text,
    TG.Color Dim,
    TG.Color Accent,
    IReadOnlyList<TG.Color> Palette)
{
    public static ColorTheme Default { get; } = new(
        new TG.Color(16, 18, 24, 255),
        new TG.Color(24, 27, 35, 255),
        new TG.Color(228, 230, 236, 255),
        new TG.Color(128, 132, 144, 255),
        new TG.Color(96, 165, 250, 255),
        new[]
        {
            new TG.Color(96, 165, 250, 255),
            new TG.Color(147, 197, 253, 255),
            new TG.Color(59, 130, 246, 255),
            new TG.Color(191, 219, 254, 255)
        });

    public TG.ColorScheme BuildScheme()
    {
        var normal = new TG.Attribute(Text, Background);
        var focus = new TG.Attribute(Background, Accent);
        var hot = new TG.Attribute(Accent, Background);
        var hotFocus = new TG.Attribute(Background, Accent);
        var disabled = new TG.Attribute(Dim, Background);
        return new TG.ColorScheme(normal, focus, hot, hotFocus, disabled);
    }

    public static ColorTheme FromImage(Image<Rgba32> image)
    {
        var step = Math.Max(1, Math.Max(image.Width, image.Height) / 96);
        double sumR = 0, sumG = 0, sumB = 0, sumW = 0;

        for (var y = 0; y < image.Height; y += step)
        {
            for (var x = 0; x < image.Width; x += step)
            {
                var p = image[x, y];
                if (p.A < 128) continue;

                var luminance = (p.R + p.G + p.B) / 3.0;
                if (luminance < 24) continue;

                var max = Math.Max(p.R, Math.Max(p.G, p.B));
                var min = Math.Min(p.R, Math.Min(p.G, p.B));
                var weight = 1.0 + (max - min) / 48.0;

                sumR += p.R * weight;
                sumG += p.G * weight;
                sumB += p.B * weight;
                sumW += weight;
            }
        }

        if (sumW <= 0) return Default;

        var r = sumR / sumW;
        var g = sumG / sumW;
        var b = sumB / sumW;

        // 封面偏暗时抬一下亮度，避免主题色糊成一团黑
        var peak = Math.Max(r, Math.Max(g, b));
        if (peak < 90)
        {
            var gain = 140.0 / Math.Max(peak, 1);
            r *= gain;
            g *= gain;
            b *= gain;
        }

        var background = Darken(r, g, b, 0.17, 8);
        var panel = Darken(r, g, b, 0.26, 12);
        var text = Lighten(r, g, b, 0.84, 205);
        var dim = Mix(text, background, 0.58);
        var accent = Boost(r, g, b, 190);
        var palette = BuildPalette(image, accent);

        return new ColorTheme(background, panel, text, dim, accent, palette);
    }

    /// <summary>取封面的 4 个主色（按色相分桶），用于跑马灯边框。</summary>
    static IReadOnlyList<TG.Color> BuildPalette(Image<Rgba32> image, TG.Color fallback)
    {
        var step = Math.Max(1, Math.Max(image.Width, image.Height) / 64);
        var buckets = new Dictionary<int, (int Count, long R, long G, long B)>();

        for (var y = 0; y < image.Height; y += step)
        {
            for (var x = 0; x < image.Width; x += step)
            {
                var p = image[x, y];
                if (p.A < 128) continue;

                var luminance = (p.R + p.G + p.B) / 3.0;
                if (luminance < 20 || luminance > 238) continue;

                var key = (p.R >> 4) << 8 | (p.G >> 4) << 4 | (p.B >> 4);
                buckets.TryGetValue(key, out var bucket);
                buckets[key] = (bucket.Count + 1, bucket.R + p.R, bucket.G + p.G, bucket.B + p.B);
            }
        }

        var ordered = buckets.Values
            .OrderByDescending(v => v.Count)
            .Take(12)
            .Select(v => (v.R / v.Count, v.G / v.Count, v.B / v.Count))
            .ToList();

        var palette = new List<TG.Color>();
        foreach (var (r, g, b) in ordered)
        {
            if (palette.Count == 4) break;

            var candidate = Boost(r, g, b, 200);
            var tooClose = palette.Any(c =>
                Math.Abs(c.R - candidate.R) + Math.Abs(c.G - candidate.G) + Math.Abs(c.B - candidate.B) < 70);

            if (!tooClose) palette.Add(candidate);
        }

        while (palette.Count < 4)
        {
            var seed = palette.Count == 0 ? fallback : palette[palette.Count - 1];
            var lighten = 0.2f + 0.08f * palette.Count;
            palette.Add(Mix(seed, new TG.Color(255, 255, 255, 255), lighten));
        }

        return palette;
    }

    static TG.Color Darken(double r, double g, double b, double factor, int floor)
    {
        return new TG.Color(
            Math.Clamp((int)Math.Round(r * factor + floor), 0, 255),
            Math.Clamp((int)Math.Round(g * factor + floor), 0, 255),
            Math.Clamp((int)Math.Round(b * factor + floor), 0, 255),
            255);
    }

    static TG.Color Lighten(double r, double g, double b, double factor, int floor)
    {
        return new TG.Color(
            Math.Clamp(Math.Max(floor, (int)Math.Round(r * (1 - factor) + 255 * factor)), 0, 255),
            Math.Clamp(Math.Max(floor, (int)Math.Round(g * (1 - factor) + 255 * factor)), 0, 255),
            Math.Clamp(Math.Max(floor, (int)Math.Round(b * (1 - factor) + 255 * factor)), 0, 255),
            255);
    }

    static TG.Color Boost(double r, double g, double b, int target)
    {
        var peak = Math.Max(r, Math.Max(g, b));
        var scale = peak <= 0 ? 1.0 : target / peak;
        return new TG.Color(
            Math.Clamp((int)Math.Round(r * scale), 0, 255),
            Math.Clamp((int)Math.Round(g * scale), 0, 255),
            Math.Clamp((int)Math.Round(b * scale), 0, 255),
            255);
    }

    static TG.Color Mix(TG.Color a, TG.Color b, double t)
    {
        return new TG.Color(
            (int)Math.Round(a.R * (1 - t) + b.R * t),
            (int)Math.Round(a.G * (1 - t) + b.G * t),
            (int)Math.Round(a.B * (1 - t) + b.B * t),
            255);
    }
}
