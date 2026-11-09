using Terminal.Gui;
using DRect = System.Drawing.Rectangle;

namespace _25DogMusic.Tui;

/// <summary>自绘列表：文字直接浮在壁纸上，不铺纯色底，选中行用强调色叠加。</summary>
public sealed class MusicListView : View
{
    readonly List<string> _items = new();

    public int Selected { get; set; } = -1;
    public event Action? OpenSelected;
    public event Action? SelectionChanged;

    int _scroll;

    public MusicListView()
    {
        CanFocus = true;
    }

    public int Count => _items.Count;

    public void SetItems(IEnumerable<string> items, int select = 0)
    {
        _items.Clear();
        _items.AddRange(items);
        Selected = _items.Count == 0 ? -1 : Math.Clamp(select, 0, _items.Count - 1);
        _scroll = Math.Max(0, Selected - Viewport.Height + 1);
        SetNeedsDraw();
        SelectionChanged?.Invoke();
    }

    public string? ItemAt(int index)
        => index >= 0 && index < _items.Count ? _items[index] : null;

    protected override bool OnKeyDown(Key key)
    {
        if (_items.Count == 0) return false;

        switch (key.KeyCode)
        {
            case KeyCode.CursorUp:
                Move(-1);
                return true;
            case KeyCode.CursorDown:
                Move(1);
                return true;
            case KeyCode.PageUp:
                Move(-Math.Max(1, Viewport.Height - 1));
                return true;
            case KeyCode.PageDown:
                Move(Math.Max(1, Viewport.Height - 1));
                return true;
            case KeyCode.Home:
                MoveTo(0);
                return true;
            case KeyCode.End:
                MoveTo(_items.Count - 1);
                return true;
            case KeyCode.Enter:
                OpenSelected?.Invoke();
                return true;
        }

        return base.OnKeyDown(key);
    }

    void Move(int delta) => MoveTo(Selected + delta);

    void Select(int index, bool notify)
    {
        if (_items.Count == 0) return;

        var next = Math.Clamp(index, 0, _items.Count - 1);
        if (next == Selected && !notify) return;

        Selected = next;
        if (Selected < _scroll) _scroll = Selected;
        if (Selected >= _scroll + Viewport.Height) _scroll = Selected - Viewport.Height + 1;

        SetNeedsDraw();
        SelectionChanged?.Invoke();
    }

    protected override bool OnMouseEvent(MouseEventArgs e)
    {
        if (_items.Count == 0) return base.OnMouseEvent(e);

        var index = _scroll + e.Position.Y;
        if (index < 0 || index >= _items.Count) return base.OnMouseEvent(e);

        if (e.Flags.HasFlag(MouseFlags.WheeledUp) || e.Flags.HasFlag(MouseFlags.WheeledDown))
        {
            SetFocus();
            Move(e.Flags.HasFlag(MouseFlags.WheeledUp) ? -3 : 3);
            return true;
        }

        if (e.Flags.HasFlag(MouseFlags.Button1Clicked) || e.Flags.HasFlag(MouseFlags.Button1DoubleClicked))
        {
            SetFocus();
            Select(index, notify: true);

            if (e.Flags.HasFlag(MouseFlags.Button1DoubleClicked))
            {
                OpenSelected?.Invoke();
            }

            return true;
        }

        return base.OnMouseEvent(e);
    }

    void MoveTo(int index) => Select(index, notify: false);

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
        var focused = HasFocus;

        for (var row = 0; row < bounds.Height; row++)
        {
            var itemIndex = _scroll + row;
            var backdrop = Wallpaper.Shared.Blend(origin.X, origin.Y + row, theme, 0.58f);
            var selected = itemIndex == Selected;

            var foreground = theme.Text;
            var background = backdrop;

            if (selected)
            {
                var highlight = Wallpaper.Shared.Blend(origin.X, origin.Y + row, theme, 0.15f);
                background = Mix(highlight, theme.Accent, focused ? 0.55f : 0.3f);
                foreground = theme.Text;
            }
            else if (itemIndex < 0 || itemIndex >= _items.Count)
            {
                background = backdrop;
            }

            driver.SetAttribute(new Terminal.Gui.Attribute(foreground, background));
            driver.Move(origin.X, origin.Y + row);
            driver.AddStr(new string(' ', bounds.Width));

            if (itemIndex < 0 || itemIndex >= _items.Count) continue;

            var text = Truncate(_items[itemIndex], bounds.Width - 2);
            driver.SetAttribute(new Terminal.Gui.Attribute(foreground, background));
            driver.Move(origin.X + 1, origin.Y + row);
            driver.AddStr(text);
        }
    }

    static string Truncate(string text, int maxWidth)
    {
        var width = 0;
        var result = new System.Text.StringBuilder();

        foreach (var rune in text.EnumerateRunes())
        {
            width += rune.Value >= 0x2E80 && rune.Value <= 0xA4CF ? 2 : 1;
            if (width > maxWidth) break;
            result.Append(rune.ToString());
        }

        return result.ToString();
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
