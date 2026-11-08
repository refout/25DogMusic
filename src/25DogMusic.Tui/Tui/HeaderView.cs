using FnMusic.Api;
using Terminal.Gui;
using DRect = System.Drawing.Rectangle;

namespace FnMusic.Tui;

/// <summary>顶部信息区：应用标题、当前曲目、播放进度条（文字直接浮在壁纸上）。</summary>
public sealed class HeaderView : View
{
    Track? _track;
    string _user = string.Empty;
    string _server = string.Empty;
    TimeSpan _position;
    TimeSpan _duration;
    bool _playing;

    public HeaderView()
    {
        CanFocus = false;
        Width = Dim.Fill();
        Height = 6;
    }

    public void SetUser(string user, string server)
    {
        _user = user;
        _server = server;
        SetNeedsDraw();
    }

    public void SetTrack(Track? track)
    {
        _track = track;
        SetNeedsDraw();
    }

    public void SetProgress(TimeSpan position, TimeSpan duration, bool playing)
    {
        _position = position;
        _duration = duration;
        _playing = playing;
        SetNeedsDraw();
    }

    protected override bool OnDrawingContent(DrawContext? context)
    {
        Paint(Viewport);
        return true;
    }

    void Paint(DRect bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        var theme = ThemeProvider.Current;
        var origin = ViewportToScreen(new System.Drawing.Point(0, 0));
        var driver = Application.Driver!;

        for (var row = 0; row < bounds.Height; row++)
        {
            var backdrop = Wallpaper.Shared.Blend(origin.X, origin.Y + row, theme, 0.42f);
            driver.SetAttribute(new Terminal.Gui.Attribute(backdrop, backdrop));
            driver.Move(origin.X, origin.Y + row);
            driver.AddStr(new string(' ', bounds.Width));
        }

        Terminal.Gui.Attribute Attr(int row, Terminal.Gui.Color foreground)
        {
            var backdrop = Wallpaper.Shared.Blend(origin.X, origin.Y + row, theme, 0.42f);
            return new Terminal.Gui.Attribute(foreground, backdrop);
        }

        // 第一行：品牌 + 服务器/用户
        driver.SetAttribute(Attr(1, theme.Accent));
        driver.Move(origin.X + 2, origin.Y + 1);
        driver.AddStr("♪ 飞牛音乐");

        var meta = string.IsNullOrEmpty(_server) ? _user : $"{_server} · {_user}";
        if (!string.IsNullOrEmpty(meta))
        {
            var metaWidth = DisplayWidth(meta);
            driver.SetAttribute(Attr(1, theme.Dim));
            driver.Move(origin.X + Math.Max(2, bounds.Width - metaWidth - 2), origin.Y + 1);
            driver.AddStr(meta);
        }

        // 第三行：当前曲目
        var state = _playing ? "▶ " : "⏸ ";
        var title = _track?.Title ?? "未选择歌曲";
        var sub = _track is null ? string.Empty : $"   {_track.Artist} · {_track.AlbumName}";

        driver.SetAttribute(Attr(3, theme.Text));
        driver.Move(origin.X + 2, origin.Y + 3);
        driver.AddStr(state + title);

        if (sub.Length > 0)
        {
            driver.SetAttribute(Attr(3, theme.Dim));
            driver.Move(origin.X + 2 + DisplayWidth(state + title), origin.Y + 3);
            driver.AddStr(sub);
        }

        // 第五行：进度条 + 时间
        var timeText = $"{Format(_position)} / {Format(_duration)}";
        var barWidth = Math.Max(8, bounds.Width - timeText.Length - 8);
        var ratio = _duration > TimeSpan.Zero
            ? Math.Clamp(_position.TotalSeconds / _duration.TotalSeconds, 0, 1)
            : 0;
        var filled = Math.Clamp((int)Math.Round(barWidth * ratio), 0, barWidth);

        driver.SetAttribute(Attr(5, theme.Accent));
        driver.Move(origin.X + 2, origin.Y + 5);
        driver.AddStr(new string('━', filled));

        driver.SetAttribute(Attr(5, theme.Dim));
        driver.Move(origin.X + 2 + filled, origin.Y + 5);
        driver.AddStr(new string('┄', barWidth - filled));

        driver.SetAttribute(Attr(5, theme.Text));
        driver.Move(origin.X + 2 + barWidth + 2, origin.Y + 5);
        driver.AddStr(timeText);
    }

    static string Format(TimeSpan time)
        => time.TotalHours >= 1
            ? $"{(int)time.TotalHours:00}:{time:mm\\:ss}"
            : $"{time:mm\\:ss}";

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
