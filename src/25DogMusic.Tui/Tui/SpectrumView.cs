using Terminal.Gui;
using DRect = System.Drawing.Rectangle;

namespace _25DogMusic.Tui;

/// <summary>频谱条：32 段，底对齐，背景透出壁纸。</summary>
public sealed class SpectrumView : View
{
    const int BandCount = 32;

    readonly float[] _bands = new float[BandCount];

    public SpectrumView()
    {
        CanFocus = false;
        Width = Dim.Fill();
        Height = 4;
    }

    public void SetBands(float[] bands)
    {
        var count = Math.Min(bands.Length, BandCount);
        for (var i = 0; i < BandCount; i++)
        {
            _bands[i] = i < count ? bands[i] : 0;
        }

        SetNeedsDraw();
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        Paint(Viewport);
        return true;
    }

    void Paint(DRect bounds)
    {
        var width = bounds.Width;
        var rows = bounds.Height;
        if (width <= 0 || rows <= 0) return;

        var theme = ThemeProvider.Current;
        var origin = ViewportToScreen(new System.Drawing.Point(0, 0));
        var driver = Application.Driver!;

        for (var row = 0; row < rows; row++)
        {
            var backdrop = Wallpaper.Shared.Blend(origin.X, origin.Y + row, theme, 0.4f);
            driver.SetAttribute(new Terminal.Gui.Attribute(backdrop, backdrop));
            driver.Move(origin.X, origin.Y + row);
            driver.AddStr(new string(' ', width));
        }

        var barWidth = Math.Max(1, width / BandCount);
        var gap = barWidth > 2 ? 1 : 0;

        for (var band = 0; band < BandCount; band++)
        {
            var level = Math.Clamp(_bands[band], 0f, 1f);
            var height = (int)Math.Round(level * rows);
            if (height <= 0) continue;

            var x = origin.X + band * barWidth;
            var cellWidth = Math.Max(1, barWidth - gap);

            for (var row = 0; row < height; row++)
            {
                var t = (float)row / Math.Max(1, rows - 1);
                var color = Mix(theme.Accent, theme.Text, t * 0.75f);
                var backdrop = Wallpaper.Shared.Blend(x, origin.Y + rows - 1 - row, theme, 0.3f);
                driver.SetAttribute(new Terminal.Gui.Attribute(color, backdrop));
                driver.Move(x, origin.Y + rows - 1 - row);
                driver.AddStr(new string('█', cellWidth));
            }
        }
    }

    static Terminal.Gui.Color Mix(Terminal.Gui.Color a, Terminal.Gui.Color b, float t)
    {
        return new Terminal.Gui.Color(
            (int)Math.Round(a.R * (1 - t) + b.R * t),
            (int)Math.Round(a.G * (1 - t) + b.G * t),
            (int)Math.Round(a.B * (1 - t) + b.B * t),
            255);
    }
}
