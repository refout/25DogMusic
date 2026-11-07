using FnMusic.Api;
using Terminal.Gui;
using DRect = System.Drawing.Rectangle;

namespace FnMusic.Tui;

/// <summary>歌词滚动视图：当前行高亮居中，背景透出壁纸。</summary>
public sealed class LyricsView : View
{
    List<LyricLine> _lines = new();
    int _index = -1;

    public LyricsView()
    {
        CanFocus = false;
        Width = Dim.Fill();
        Height = Dim.Fill();
    }

    public void SetLyrics(List<LyricLine> lines)
    {
        _lines = lines;
        _index = -1;
        SetNeedsDraw();
    }

    public void UpdatePosition(TimeSpan position)
    {
        var index = IndexAt(position);
        if (index == _index) return;

        _index = index;
        SetNeedsDraw();
    }

    int IndexAt(TimeSpan position)
    {
        var low = 0;
        var high = _lines.Count - 1;
        var result = -1;

        while (low <= high)
        {
            var mid = (low + high) / 2;
            if (_lines[mid].Time <= position)
            {
                result = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return result;
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        Paint(Viewport);
        return true;
    }

    void Paint(DRect bounds)
    {
        var width = bounds.Width;
        var height = bounds.Height;
        if (width <= 0 || height <= 0) return;

        var theme = ThemeProvider.Current;
        var origin = ViewportToScreen(new System.Drawing.Point(0, 0));
        var driver = Application.Driver!;

        for (var row = 0; row < height; row++)
        {
            var backdrop = Wallpaper.Shared.Blend(origin.X, origin.Y + row, theme, 0.46f);
            driver.SetAttribute(new Terminal.Gui.Attribute(backdrop, backdrop));
            driver.Move(origin.X, origin.Y + row);
            driver.AddStr(new string(' ', width));
        }

        if (_lines.Count == 0)
        {
            var emptyBackdrop = Wallpaper.Shared.Blend(origin.X, origin.Y + height / 2, theme, 0.46f);
            driver.SetAttribute(new Terminal.Gui.Attribute(theme.Dim, emptyBackdrop));
            driver.Move(origin.X + Math.Max(0, (width - 6) / 2), origin.Y + height / 2);
            driver.AddStr("无歌词");
            return;
        }

        var centerRow = height / 2;
        var startIndex = Math.Max(0, _index - centerRow);
        var endIndex = Math.Min(_lines.Count, startIndex + height);

        for (var i = startIndex; i < endIndex; i++)
        {
            var text = _lines[i].Text;
            var displayWidth = DisplayWidth(text);
            var col = Math.Max(0, (width - displayWidth) / 2);
            var row = i - startIndex;

            var backdrop = Wallpaper.Shared.Blend(origin.X + col, origin.Y + row, theme, i == _index ? 0.28f : 0.5f);
            var foreground = i == _index ? theme.Text : theme.Dim;

            driver.SetAttribute(new Terminal.Gui.Attribute(foreground, backdrop));
            driver.Move(origin.X + col, origin.Y + row);
            driver.AddStr(text);
        }
    }

    static int DisplayWidth(string text)
    {
        var width = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            width += rune.Value >= 0x2E80 && rune.Value <= 0xA4CF ? 2 : 1;
        }
        return width;
    }
}
