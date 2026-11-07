using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using FnMusic.Api;
using FnMusic.Config;
using FnMusic.Playback;
using FluentIcons.Avalonia;
using FluentIcons.Common;

namespace _25DogMusic.Desktop;

public partial class MainWindow : Window
{
    static readonly IBrush DTextPrimary = new SolidColorBrush(Color.Parse("#F2F2F5"));
    static readonly IBrush LTextPrimary = new SolidColorBrush(Color.Parse("#1C1C1E"));
    static readonly IBrush DTextSecondary = new SolidColorBrush(Color.Parse("#B8B8C0"));
    static readonly IBrush LTextSecondary = new SolidColorBrush(Color.Parse("#44444C"));
    static readonly IBrush DTextMuted = new SolidColorBrush(Color.Parse("#A2A2AA"));
    static readonly IBrush LTextMuted = new SolidColorBrush(Color.Parse("#5C5C64"));
    static readonly IBrush DIconIdle = new SolidColorBrush(Color.Parse("#C9C9CF"));
    static readonly IBrush LIconIdle = new SolidColorBrush(Color.Parse("#5E5E66"));
    static readonly IBrush DHeartOn = new SolidColorBrush(Color.Parse("#FF375F"));
    static readonly IBrush LHeartOn = new SolidColorBrush(Color.Parse("#E0003A"));
    static readonly IBrush DAccentSoft = new SolidColorBrush(Color.Parse("#FF7A90"));
    static readonly IBrush LAccentSoft = new SolidColorBrush(Color.Parse("#E0003A"));
    static readonly IBrush DCardBg = new SolidColorBrush(Color.Parse("#1A1A22"));
    static readonly IBrush LCardBg = new SolidColorBrush(Color.Parse("#E4E4EA"));
    static readonly IBrush DCoverBg = new SolidColorBrush(Color.Parse("#17171E"));
    static readonly IBrush LCoverBg = new SolidColorBrush(Color.Parse("#E4E4EA"));
    static readonly IBrush DarkCoverPlaceholder = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        GradientStops = new GradientStops
        {
            new GradientStop(Color.Parse("#23232B"), 0),
            new GradientStop(Color.Parse("#0F0F13"), 1)
        }
    };
    static readonly IBrush LightCoverPlaceholder = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        GradientStops = new GradientStops
        {
            new GradientStop(Color.Parse("#E9E9EF"), 0),
            new GradientStop(Color.Parse("#F8F8FC"), 1)
        }
    };
    static readonly IBrush DRowHover = new SolidColorBrush(Color.Parse("#12FFFFFF"));
    static readonly IBrush LRowHover = new SolidColorBrush(Color.Parse("#0D000000"));
    static readonly IBrush DRowActive = new SolidColorBrush(Color.Parse("#24FFFFFF"));
    static readonly IBrush LRowActive = new SolidColorBrush(Color.Parse("#14FF375F"));
    static readonly IBrush DDivider = new SolidColorBrush(Color.Parse("#1AFFFFFF"));
    static readonly IBrush LDivider = new SolidColorBrush(Color.Parse("#14000000"));
    static readonly IBrush DSectionLabel = new SolidColorBrush(Color.Parse("#A8A8B0"));
    static readonly IBrush LSectionLabel = new SolidColorBrush(Color.Parse("#5C5C64"));
    static readonly IBrush DCardLabel = new SolidColorBrush(Color.Parse("#C4C4CC"));
    static readonly IBrush LCardLabel = new SolidColorBrush(Color.Parse("#44444C"));
    static readonly IBrush DSwatchBorder = new SolidColorBrush(Color.Parse("#26FFFFFF"));
    static readonly IBrush LSwatchBorder = new SolidColorBrush(Color.Parse("#33000000"));
    static readonly IBrush DEmptyIcon = new SolidColorBrush(Color.Parse("#4E4E58"));
    static readonly IBrush LEmptyIcon = new SolidColorBrush(Color.Parse("#A8A8B0"));
    static readonly IBrush DNoteStroke = new SolidColorBrush(Color.Parse("#75757F"));
    static readonly IBrush LNoteStroke = new SolidColorBrush(Color.Parse("#8E8E96"));
    static readonly IBrush DHeroBg = new SolidColorBrush(Color.Parse("#10FFFFFF"));
    static readonly IBrush LHeroBg = new SolidColorBrush(Color.Parse("#FFFFFF"));
    static readonly IBrush DSurface = new SolidColorBrush(Color.Parse("#0EFFFFFF"));
    static readonly IBrush LSurface = new SolidColorBrush(Color.Parse("#FFFFFF"));
    static readonly IBrush DPillBg = new SolidColorBrush(Color.Parse("#1FFFFFFF"));
    static readonly IBrush LPillBg = new SolidColorBrush(Color.Parse("#E9E9EF"));
    static readonly IBrush DPlayButtonFg = new SolidColorBrush(Color.Parse("#0B0B0E"));
    static readonly IBrush LPlayButtonFg = new SolidColorBrush(Color.Parse("#FFFFFF"));
    static readonly IBrush DLyricIdle = new SolidColorBrush(Color.Parse("#A2A2AA"));
    static readonly IBrush LLyricIdle = new SolidColorBrush(Color.Parse("#5C5C64"));
    static readonly IBrush DLyricActive = new SolidColorBrush(Color.Parse("#FFFFFF"));

    bool IsLight => string.Equals(_config.Theme, "light", StringComparison.OrdinalIgnoreCase);

    IBrush TextPrimary => IsLight ? LTextPrimary : DTextPrimary;
    IBrush TextSecondary => IsLight ? LTextSecondary : DTextSecondary;
    IBrush TextMuted => IsLight ? LTextMuted : DTextMuted;
    IBrush IconIdle => IsLight ? LIconIdle : DIconIdle;
    IBrush HeartOn => IsLight ? LHeartOn : DHeartOn;
    IBrush AccentSoft => IsLight ? LAccentSoft : DAccentSoft;
    IBrush CardBg => IsLight ? LCardBg : DCardBg;
    IBrush CoverBg => IsLight ? LCoverBg : DCoverBg;
    IBrush CoverPlaceholder => IsLight ? LightCoverPlaceholder : DarkCoverPlaceholder;
    IBrush RowHover => IsLight ? LRowHover : DRowHover;
    IBrush RowActive => IsLight ? LRowActive : DRowActive;
    IBrush Divider => IsLight ? LDivider : DDivider;
    IBrush SectionLabel => IsLight ? LSectionLabel : DSectionLabel;
    IBrush CardLabel => IsLight ? LCardLabel : DCardLabel;
    IBrush SwatchBorder => IsLight ? LSwatchBorder : DSwatchBorder;
    IBrush EmptyIcon => IsLight ? LEmptyIcon : DEmptyIcon;
    IBrush NoteStroke => IsLight ? LNoteStroke : DNoteStroke;
    IBrush HeroBg => IsLight ? LHeroBg : DHeroBg;
    IBrush Surface => IsLight ? LSurface : DSurface;
    IBrush PillBg => IsLight ? LPillBg : DPillBg;
    IBrush PlayButtonFg => IsLight ? LPlayButtonFg : DPlayButtonFg;
    IBrush LyricIdle => IsLight ? LLyricIdle : DLyricIdle;
    IBrush LyricActive => IsLight ? LTextPrimary : DLyricActive;

    /// <summary>统一使用 FluentIcons 图标库构建图标，不再手绘几何路径。</summary>
    static SymbolIcon MakeIcon(Symbol symbol, double size, IBrush? brush = null, bool filled = false)
    {
        var icon = new SymbolIcon
        {
            Symbol = symbol,
            FontSize = size,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (filled) icon.IconVariant = IconVariant.Filled;
        if (brush is not null) icon.Foreground = brush;
        return icon;
    }

    /// <summary>侧边栏歌单条目图标，配色跟随导航图标。</summary>
    SymbolIcon PlaylistIcon()
    {
        var icon = MakeIcon(Symbol.TextBulletList, 14);
        icon.Classes.Add("navicon");
        return icon;
    }

    readonly AppConfig _config = AppConfig.Load();

    // 封面按用途分桶做 LRU 缓存：列表缩略图、卡片、全屏大图，避免内存随浏览无上限增长
    readonly LruImageCache _thumbCache = new(160);
    readonly LruImageCache _artCache = new(48);
    readonly LruImageCache _largeCache = new(4);

    // 跟随封面时使用的封面色调，以及按封面 GUID 缓存的取色结果
    readonly Dictionary<string, Color> _tintCache = new();
    Color? _coverTint;
    Color _windowBaseColor = Color.Parse("#0B0B0E");

    readonly Dictionary<string, Image> _rowImages = new();
    readonly Dictionary<string, Border> _rowBorders = new();
    readonly Dictionary<string, SymbolIcon> _heartIcons = new();
    readonly Dictionary<string, TextBlock> _rowNumbers = new();
    readonly HashSet<string> _favorites = new();

    MusicApiClient? _api;
    VlcPlayer? _player;
    StreamProxy? _proxy;
    DispatcherTimer? _timer;
    CancellationTokenSource? _pageLoadCts;
    TrayIcon? _tray;
    bool _hiddenToTray;
    bool _reallyExit;

    List<Album> _albums = new();
    List<Genre> _genres = new();
    List<Track> _tracks = new();
    List<Track> _queue = new();
    int _queueIndex = -1;

    bool _roaming;
    string? _roamId;
    string _lastSearch = string.Empty;
    Image? _roamCover;
    TextBlock? _roamTitle;
    TextBlock? _roamSub;
    TextBlock? _roamMeta;
    SymbolIcon? _roamPlayIcon;

    enum Mode { Home, Albums, Tracks, Genres, History, Favorites, Artists, Folders, Search, Roam, Settings }

    Mode _mode = Mode.Albums;

    Track? _currentTrack;
    List<LyricLine> _lyrics = new();
    readonly List<TextBlock> _lyricBlocks = new();
    int _lyricIndex = -1;

    Panel _pageHost = null!;

    // 音乐库分页（滚动到底部继续加载）
    readonly HashSet<string> _librarySeen = new();
    int _libraryPage;
    int _libraryTotal = -1;
    bool _libraryLoading;
    bool _infiniteLibrary;
    TextBlock? _libraryFooter;

    bool _shuffle;
    int _repeat;
    bool _seeking;
    bool _suppressSeek;
    bool _paused;
    bool _ended;

    public MainWindow()
    {
        InitializeComponent();
        _pageHost = ContentPanel;

        NavHome.Click += async (_, _) => await LoadHomeAsync();
        NavFavorites.Click += async (_, _) => await LoadFavoritesAsync();
        NavRecent.Click += async (_, _) => await LoadHistoryAsync();
        NavAlbums.Click += async (_, _) => await LoadAlbumsAsync();
        NavArtists.Click += async (_, _) => await LoadArtistsAsync();
        NavGenres.Click += async (_, _) => await LoadGenresAsync();
        NavLibrary.Click += async (_, _) => await LoadAllTracksAsync();
        NavFolders.Click += (_, _) => ShowFoldersPage();
        NavSettings.Click += (_, _) => ShowSettingsPage();

        PlaylistAddButton.Click += (_, _) =>
        {
            PlaylistCreateBar.IsVisible = !PlaylistCreateBar.IsVisible;
            if (PlaylistCreateBar.IsVisible) PlaylistNameBox.Focus();
        };
        PlaylistCreateButton.Click += async (_, _) => await CreatePlaylistFromSidebarAsync();
        PlaylistNameBox.KeyDown += async (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            await CreatePlaylistFromSidebarAsync();
        };

        SearchBox.KeyDown += async (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            await SearchAsync(SearchBox.Text ?? string.Empty);
        };

        PlayButton.Click += (_, _) => TogglePlay();
        NextButton.Click += async (_, _) => await AdvanceAsync(1, allowWrap: true);
        PrevButton.Click += async (_, _) => await AdvanceAsync(-1, allowWrap: true);
        ShuffleButton.Click += (_, _) => { _shuffle = !_shuffle; UpdatePlaybackModeUi(); };
        RepeatButton.Click += (_, _) => { _repeat = (_repeat + 1) % 3; UpdatePlaybackModeUi(); };
        QueueButton.Click += (_, _) => ToggleQueueDrawer();

        QueuePanelClose.Click += (_, _) => HideQueueDrawer();
        QueuePanelClear.Click += (_, _) =>
        {
            _queue.Clear();
            _queueIndex = -1;
            RefreshQueueDrawer();
            StatusText.Text = "已清空播放列表";
        };

        NowClickArea.PointerPressed += (_, _) => OpenOverlay();
        CloseOverlayButton.Click += (_, _) => HideOverlay();
        OverlayPlay.Click += (_, _) => TogglePlay();
        OverlayNext.Click += async (_, _) => await AdvanceAsync(1, allowWrap: true);
        OverlayPrev.Click += async (_, _) => await AdvanceAsync(-1, allowWrap: true);
        OverlayShuffle.Click += (_, _) => { _shuffle = !_shuffle; UpdatePlaybackModeUi(); };
        OverlayRepeat.Click += (_, _) => { _repeat = (_repeat + 1) % 3; UpdatePlaybackModeUi(); };

        WireSeek(SeekSlider);
        WireSeek(OverlaySeek);

        VolumeSlider.Value = _config.Volume;
        VolumeSlider.ValueChanged += (_, e) =>
        {
            if (_player is not null) _player.Volume = MapVolume(e.NewValue);
        };

        KeyDown += OnWindowKeyDown;
        ListScroll.ScrollChanged += (_, _) => OnListScrolled();

        _player = new VlcPlayer();
        _player.Volume = MapVolume(_config.Volume);
        _player.EndReached += (_, _) => Dispatcher.UIThread.Post(OnEndReached);

        _tray = CreateTrayIcon();

        _shuffle = _config.StartShuffle;
        _repeat = Math.Clamp(_config.StartRepeat, 0, 2);
        ApplyBackground();
        UpdatePlaybackModeUi();

        if (!string.IsNullOrWhiteSpace(_config.Token))
        {
            StatusText.Text = "正在加载曲库…";
            _ = InitialLoadAsync();
        }
        else
        {
            StatusText.Text = "请先在「设置」中登录";
        }
    }

    async Task InitialLoadAsync()
    {
        await RefreshFavoriteGuidsAsync();
        await RefreshSidebarPlaylistsAsync();
        await LoadHomeAsync();
    }

    /// <summary>滑块 0-100 按平方曲线映射到播放器音量：听感更接近线性，两端不会突变。</summary>
    static int MapVolume(double sliderValue)
    {
        var fraction = Math.Clamp(sliderValue, 0, 100) / 100.0;
        return (int)Math.Round(Math.Pow(fraction, 2) * 100);
    }

    // ---- 登录 / 客户端 ----

    int _settingsTab;

    void ShowSettingsPage(string? message = null)
    {
        _mode = Mode.Settings;
        SetActiveNav(NavSettings);
        BeginPage("设置", "外观、音质与播放");
        EnsureDeviceId();

        var tabs = new TabControl
        {
            Width = 680,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 0, 0)
        };

        tabs.Items.Add(new TabItem { Header = "账号与连接", Content = BuildAccountCard(message) });
        tabs.Items.Add(new TabItem { Header = "外观", Content = BuildAppearanceCard() });
        tabs.Items.Add(new TabItem { Header = "音质与播放", Content = BuildPlaybackCard() });

        tabs.SelectedIndex = Math.Clamp(_settingsTab, 0, tabs.ItemCount - 1);
        tabs.SelectionChanged += (_, _) =>
        {
            if (tabs.SelectedIndex >= 0) _settingsTab = tabs.SelectedIndex;
        };

        ContentPanel.Children.Add(tabs);
    }

    // ---- 播放列表抽屉 ----

    readonly List<(string Guid, Border Row, TextBlock Number, TextBlock Title)> _drawerRows = new();

    void ToggleQueueDrawer()
    {
        if (QueuePanel.IsVisible) HideQueueDrawer();
        else ShowQueueDrawer();
    }

    void ShowQueueDrawer()
    {
        RefreshQueueDrawer();
        QueuePanel.IsVisible = true;
        Dispatcher.UIThread.Post(() => QueuePanelScroll.Offset = new Vector(0, 0), DispatcherPriority.Background);
    }

    void HideQueueDrawer() => QueuePanel.IsVisible = false;

    void RefreshQueueDrawer()
    {
        QueuePanelRows.Children.Clear();
        _drawerRows.Clear();

        QueuePanelCount.Text = _queue.Count == 0 ? string.Empty : $"共 {_queue.Count} 首";
        QueuePanelClear.IsVisible = _queue.Count > 0;

        if (_queue.Count == 0)
        {
            QueuePanelRows.Children.Add(new TextBlock
            {
                Text = "还没有歌曲，先播放一首吧",
                FontSize = 12.5,
                Foreground = TextMuted,
                Margin = new Thickness(4, 20, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        for (var i = 0; i < _queue.Count; i++)
        {
            QueuePanelRows.Children.Add(BuildDrawerRow(_queue[i], i));
        }

        UpdateQueueDrawerHighlight();
    }

    Control BuildDrawerRow(Track track, int index)
    {
        var number = new TextBlock
        {
            Text = (index + 1).ToString(),
            FontSize = 11.5,
            Foreground = TextMuted,
            Width = 22,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Right
        };

        var title = new TextBlock
        {
            Text = track.Title,
            FontSize = 13,
            Foreground = TextPrimary,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var sub = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(track.Artist) ? string.Empty : track.Artist,
            FontSize = 10.5,
            Foreground = TextSecondary,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var texts = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        texts.Children.Add(title);
        texts.Children.Add(sub);

        var duration = new TextBlock
        {
            Text = FormatDuration(track.DurationMs),
            FontSize = 10.5,
            Foreground = TextMuted,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("22,*,Auto"), VerticalAlignment = VerticalAlignment.Center };
        grid.Children.Add(number);
        Grid.SetColumn(texts, 1);
        grid.Children.Add(texts);
        Grid.SetColumn(duration, 2);
        grid.Children.Add(duration);

        var row = new Border
        {
            Padding = new Thickness(8, 7),
            CornerRadius = new CornerRadius(10),
            Background = Brushes.Transparent,
            Child = grid,
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        row.PointerEntered += (_, _) =>
        {
            if (!IsPlayingTrack(track)) row.Background = RowHover;
        };
        row.PointerExited += (_, _) =>
        {
            row.Background = IsPlayingTrack(track) ? RowActive : Brushes.Transparent;
        };
        row.PointerPressed += (_, _) => PlayFromQueue(_queue, index);

        _drawerRows.Add((track.Guid, row, number, title));
        return row;
    }

    void UpdateQueueDrawerHighlight()
    {
        if (!QueuePanel.IsVisible) return;

        foreach (var item in _drawerRows)
        {
            var active = item.Guid == _currentTrack?.Guid;
            item.Row.Background = active ? RowActive : Brushes.Transparent;
            item.Title.Foreground = active ? AccentSoft : TextPrimary;
            item.Number.Foreground = active ? AccentSoft : TextMuted;
        }
    }

    Border MakeSettingsCard(StackPanel content) => new()
    {
        Background = Surface,
        CornerRadius = new CornerRadius(12),
        Padding = new Thickness(18),
        Width = 640,
        HorizontalAlignment = HorizontalAlignment.Left,
        Child = content
    };

    Control BuildField(string label, Control field)
    {
        var stack = new StackPanel { Spacing = 5 };
        stack.Children.Add(new TextBlock { Text = label, FontSize = 11.5, Foreground = TextSecondary });
        stack.Children.Add(field);
        return stack;
    }

    Control BuildSettingRow(string label, string? desc, Control control)
    {
        var texts = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        texts.Children.Add(new TextBlock { Text = label, FontSize = 13, Foreground = TextPrimary });
        if (!string.IsNullOrWhiteSpace(desc))
        {
            texts.Children.Add(new TextBlock
            {
                Text = desc,
                FontSize = 11.5,
                Foreground = TextMuted,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 520
            });
        }

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), VerticalAlignment = VerticalAlignment.Center };
        grid.Children.Add(texts);
        Grid.SetColumn(control, 1);
        grid.Children.Add(control);
        return grid;
    }

    Control BuildAccountCard(string? message)
    {
        var loggedIn = !string.IsNullOrWhiteSpace(_config.Token);

        var statusText = new TextBlock
        {
            Text = message ?? (loggedIn
                ? string.IsNullOrWhiteSpace(_config.LastUsername) ? "已连接飞牛音乐" : $"已登录为 {_config.LastUsername}"
                : "未登录，请填写账号或使用浏览器授权"),
            FontSize = 12.5,
            Foreground = loggedIn ? TextPrimary : TextSecondary,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        var dot = new Border
        {
            Width = 7,
            Height = 7,
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(Color.Parse(loggedIn ? "#34C759" : "#FF9F0A")),
            VerticalAlignment = VerticalAlignment.Center
        };

        var statusRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        statusRow.Children.Add(dot);
        statusRow.Children.Add(statusText);

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), VerticalAlignment = VerticalAlignment.Center };
        header.Children.Add(statusRow);

        if (loggedIn)
        {
            var logout = new Button
            {
                Content = "退出登录",
                FontSize = 12,
                Padding = new Thickness(14, 6),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            logout.Classes.Add("ghost");
            logout.Click += (_, _) => Logout();
            Grid.SetColumn(logout, 1);
            header.Children.Add(logout);
        }

        var serverBox = MakeFormBox(_config.BaseUrl, "https://10.0.0.10");
        var userBox = MakeFormBox(_config.LastUsername ?? string.Empty, "用户名");
        var passBox = MakeFormBox(string.Empty, "密码");
        passBox.PasswordChar = '*';
        passBox.KeyDown += async (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            await PasswordLoginAsync(serverBox.Text, userBox.Text, passBox.Text, statusText);
        };

        var loginButton = MakeFormButton("登录");
        loginButton.Click += async (_, _) =>
            await PasswordLoginAsync(serverBox.Text, userBox.Text, passBox.Text, statusText);

        var oauthButton = MakeFormButton("在浏览器中授权");
        oauthButton.Click += async (_, _) => await StartOAuthAsync(serverBox.Text, statusText);

        var codeBox = MakeFormBox(string.Empty, "code 或完整回跳地址");
        var submitButton = MakeFormButton("提交授权码");
        submitButton.Width = 120;
        submitButton.HorizontalAlignment = HorizontalAlignment.Right;
        submitButton.Click += async (_, _) => await SubmitOAuthAsync(serverBox.Text, codeBox.Text, statusText);

        var credentials = new Grid { ColumnDefinitions = new ColumnDefinitions("*,12,*") };
        credentials.Children.Add(BuildField("用户名", userBox));
        var passField = BuildField("密码", passBox);
        Grid.SetColumn(passField, 2);
        credentials.Children.Add(passField);

        var codeRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,Auto") };
        codeRow.Children.Add(codeBox);
        Grid.SetColumn(submitButton, 2);
        codeRow.Children.Add(submitButton);

        var body = new StackPanel { Spacing = 12 };
        body.Children.Add(header);
        body.Children.Add(BuildField("服务器地址", serverBox));
        body.Children.Add(credentials);
        body.Children.Add(loginButton);
        body.Children.Add(new Border { Height = 1, Background = Divider, Margin = new Thickness(0, 4, 0, 2) });
        body.Children.Add(new TextBlock
        {
            Text = "浏览器授权",
            FontSize = 12.5,
            FontWeight = FontWeight.SemiBold,
            Foreground = CardLabel
        });
        body.Children.Add(new TextBlock
        {
            Text = "打开飞牛 NAS 授权页，登录后把回跳地址或 code 粘贴到下方。",
            FontSize = 11.5,
            Foreground = TextMuted,
            TextWrapping = TextWrapping.Wrap
        });
        body.Children.Add(oauthButton);
        body.Children.Add(codeRow);

        return MakeSettingsCard(body);
    }

    Control BuildAppearanceCard()
    {
        var swatches = new Dictionary<string, Border>();

        var options = IsLight
            ? new (string Mode, string Label, IBrush Brush)[]
            {
                ("cover", "跟随封面", new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop(Color.Parse("#D9D9E4"), 0),
                        new GradientStop(Color.Parse("#F8F8FC"), 1)
                    }
                }),
                ("black", "纯白", new SolidColorBrush(Color.Parse("#FFFFFF"))),
                ("gray", "浅灰", new SolidColorBrush(Color.Parse("#F0F0F5"))),
                ("navy", "浅蓝", new SolidColorBrush(Color.Parse("#E8EFFA"))),
                ("wine", "暖白", new SolidColorBrush(Color.Parse("#FAF5EE")))
            }
            : new (string Mode, string Label, IBrush Brush)[]
            {
                ("cover", "跟随封面", new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop(Color.Parse("#4B3B5A"), 0),
                        new GradientStop(Color.Parse("#121218"), 1)
                    }
                }),
                ("black", "纯黑", new SolidColorBrush(Color.Parse("#0B0B0E"))),
                ("gray", "深灰", new SolidColorBrush(Color.Parse("#17171C"))),
                ("navy", "午夜蓝", new SolidColorBrush(Color.Parse("#101A2E"))),
                ("wine", "深棕", new SolidColorBrush(Color.Parse("#1A1112")))
            };

        var wrap = new WrapPanel { Margin = new Thickness(0, 2, 0, 0) };

        foreach (var option in options)
        {
            var fill = new Border
            {
                Width = 54,
                Height = 40,
                CornerRadius = new CornerRadius(10),
                Background = option.Brush,
                BorderBrush = SwatchBorder,
                BorderThickness = new Thickness(1),
                IsHitTestVisible = false
            };

            var swatch = new Button
            {
                Content = fill,
                Width = 54,
                Height = 40,
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            swatch.Classes.Add("swatch");
            swatch.Click += (_, _) =>
            {
                _config.BackgroundMode = option.Mode;
                _config.Save();
                ApplyBackground();
                UpdateSelection();
                StatusText.Text = $"背景：{option.Label}";
            };

            var item = new StackPanel { Spacing = 0, Margin = new Thickness(0, 0, 16, 8) };
            item.Children.Add(swatch);
            item.Children.Add(new TextBlock
            {
                Text = option.Label,
                FontSize = 11.5,
                Foreground = TextSecondary,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 0)
            });
            wrap.Children.Add(item);
            swatches[option.Mode] = fill;
        }

        void UpdateSelection()
        {
            foreach (var pair in swatches)
            {
                var selected = string.Equals(_config.BackgroundMode, pair.Key, StringComparison.OrdinalIgnoreCase);
                pair.Value.BorderBrush = selected ? (IsLight ? TextPrimary : Brushes.White) : SwatchBorder;
                pair.Value.BorderThickness = new Thickness(selected ? 2 : 1);
            }
        }
        UpdateSelection();

        var body = new StackPanel { Spacing = 12 };

        var themeBox = new ComboBox
        {
            ItemsSource = new[] { "深色", "浅色" },
            SelectedIndex = IsLight ? 1 : 0,
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        themeBox.SelectionChanged += (_, _) =>
        {
            var index = themeBox.SelectedIndex;
            if (index < 0) return;
            var target = index == 1 ? "light" : "dark";
            if (target != _config.Theme) Dispatcher.UIThread.Post(() => ApplyTheme(target));
        };
        body.Children.Add(BuildSettingRow("主题", "深色或浅色外观", themeBox));

        var materialKeys = new[] { "off", "mica", "acrylic", "blur", "liquid" };
        var materialIndex = Array.IndexOf(materialKeys, string.IsNullOrWhiteSpace(_config.Material) ? "off" : _config.Material);
        if (materialIndex < 0) materialIndex = 0;

        var materialBox = new ComboBox
        {
            ItemsSource = new[] { "无特效", "云母 Mica", "亚克力 Acrylic", "模糊 Blur", "液态玻璃" },
            SelectedIndex = materialIndex,
            Width = 190,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        body.Children.Add(BuildSettingRow("窗口材质", "Windows 11 的云母 / 亚克力 / 液态玻璃效果", materialBox));

        var opacityValue = new TextBlock
        {
            Text = $"{(int)Math.Round(_config.UiOpacity * 100)}%",
            FontSize = 12,
            Foreground = TextSecondary,
            Width = 44,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        var opacitySlider = new Slider
        {
            Minimum = 30,
            Maximum = 100,
            Value = Math.Clamp(_config.UiOpacity * 100, 30, 100),
            Width = 190,
            VerticalAlignment = VerticalAlignment.Center
        };
        var opacityBox = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        opacityBox.Children.Add(opacitySlider);
        opacityBox.Children.Add(opacityValue);
        body.Children.Add(BuildSettingRow("窗口透明度", "整个窗口（内容区、侧边栏、播放栏）的透出程度", opacityBox));

        var description = new TextBlock
        {
            FontSize = 11.5,
            Foreground = TextMuted,
            TextWrapping = TextWrapping.Wrap
        };
        body.Children.Add(description);
        body.Children.Add(wrap);

        void UpdateMaterialState()
        {
            var active = materialBox.SelectedIndex > 0;
            opacitySlider.IsEnabled = active;
            opacityValue.Opacity = active ? 1.0 : 0.45;
            wrap.IsEnabled = true;
            wrap.Opacity = 1.0;
            description.Text = active
                ? "材质与背景作用于整个窗口：跟随封面时透出当前歌曲封面，纯色时按所选颜色着色；「窗口透明度」统一控制整个窗口的透出程度。"
                : "选择窗口背景。跟随封面会用当前歌曲封面做柔和底图，纯色背景更安静。";
        }

        materialBox.SelectionChanged += (_, _) =>
        {
            var index = materialBox.SelectedIndex;
            if (index < 0) return;
            _config.Material = materialKeys[index];
            _config.Save();
            ApplyBackground(announceMaterial: true);
            UpdateMaterialState();
        };

        opacitySlider.ValueChanged += (_, e) =>
        {
            var percent = (int)Math.Round(e.NewValue);
            _config.UiOpacity = percent / 100.0;
            opacityValue.Text = $"{percent}%";
            ApplyBackground();
        };
        opacitySlider.PointerReleased += (_, _) => _config.Save();

        UpdateMaterialState();
        return MakeSettingsCard(body);
    }

    Control BuildPlaybackCard()
    {
        var body = new StackPanel { Spacing = 14 };

        var qualitySwitch = new ToggleSwitch
        {
            IsChecked = _config.ShowAudioQuality,
            OnContent = string.Empty,
            OffContent = string.Empty,
            VerticalAlignment = VerticalAlignment.Center
        };
        qualitySwitch.IsCheckedChanged += (_, _) =>
        {
            _config.ShowAudioQuality = qualitySwitch.IsChecked == true;
            _config.Save();
            RefreshNowPlayingText();
        };
        body.Children.Add(BuildSettingRow(
            "显示音质信息",
            "在播放栏与全屏页显示当前曲目的格式、位深与采样率",
            qualitySwitch));

        var backgroundSwitch = new ToggleSwitch
        {
            IsChecked = _config.BackgroundPlayback,
            OnContent = string.Empty,
            OffContent = string.Empty,
            VerticalAlignment = VerticalAlignment.Center
        };
        backgroundSwitch.IsCheckedChanged += (_, _) =>
        {
            _config.BackgroundPlayback = backgroundSwitch.IsChecked == true;
            _config.Save();
        };
        body.Children.Add(BuildSettingRow(
            "关闭窗口后继续播放",
            "开启时关闭窗口会隐藏到系统托盘，音乐不中断；退出请使用托盘菜单",
            backgroundSwitch));

        var volumeValue = new TextBlock
        {
            Text = _config.Volume.ToString(),
            FontSize = 12,
            Foreground = TextSecondary,
            Width = 34,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        var volumeSlider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = _config.Volume,
            Width = 190,
            VerticalAlignment = VerticalAlignment.Center
        };
        volumeSlider.ValueChanged += (_, e) =>
        {
            var value = (int)e.NewValue;
            _config.Volume = value;
            volumeValue.Text = value.ToString();
            VolumeSlider.Value = value;
        };
        volumeSlider.PointerReleased += (_, _) => _config.Save();

        var volumeBox = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        volumeBox.Children.Add(volumeSlider);
        volumeBox.Children.Add(volumeValue);
        body.Children.Add(BuildSettingRow("默认音量", "启动应用时使用的音量", volumeBox));

        var shuffleSwitch = new ToggleSwitch
        {
            IsChecked = _config.StartShuffle,
            OnContent = string.Empty,
            OffContent = string.Empty,
            VerticalAlignment = VerticalAlignment.Center
        };
        shuffleSwitch.IsCheckedChanged += (_, _) =>
        {
            _config.StartShuffle = shuffleSwitch.IsChecked == true;
            _shuffle = _config.StartShuffle;
            UpdatePlaybackModeUi();
            _config.Save();
        };
        body.Children.Add(BuildSettingRow("启动时随机播放", "开启后启动默认按随机顺序播放", shuffleSwitch));

        var repeatBox = new ComboBox
        {
            ItemsSource = new[] { "关闭", "列表循环", "单曲循环" },
            SelectedIndex = Math.Clamp(_config.StartRepeat, 0, 2),
            Width = 150,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        repeatBox.SelectionChanged += (_, _) =>
        {
            _config.StartRepeat = repeatBox.SelectedIndex;
            _repeat = Math.Clamp(_config.StartRepeat, 0, 2);
            UpdatePlaybackModeUi();
            _config.Save();
        };
        body.Children.Add(BuildSettingRow("默认循环模式", "启动时应用的循环方式", repeatBox));

        return MakeSettingsCard(body);
    }

    void ApplyBackground(bool announceMaterial = false)
    {
        var mode = string.IsNullOrWhiteSpace(_config.BackgroundMode) ? "cover" : _config.BackgroundMode;
        var material = string.IsNullOrWhiteSpace(_config.Material) ? "off" : _config.Material;
        var useMaterial = material != "off";
        var opacity = Math.Clamp(_config.UiOpacity <= 0 ? 0.75 : _config.UiOpacity, 0.3, 1.0);

        var page = Color.Parse(IsLight
            ? mode switch
            {
                "black" => "#FFFFFF",
                "gray" => "#F0F0F5",
                "navy" => "#E8EFFA",
                "wine" => "#FAF5EE",
                _ => "#F5F5F9"
            }
            : mode switch
            {
                "black" => "#0B0B0E",
                "gray" => "#17171C",
                "navy" => "#101A2E",
                "wine" => "#1A1112",
                _ => "#0B0B0E"
            });

        var themePage = Color.Parse(IsLight ? "#F5F5F9" : "#0B0B0E");
        // 材质与背景可叠加：跟随封面时底色取自封面平均色，纯色时用所选颜色
        var useCover = mode == "cover";
        var coverBase = useCover ? CoverTint(_coverTint ?? themePage) : page;
        Backdrop.IsVisible = useCover;
        BackdropShade.IsVisible = useCover;
        // 封面压暗/提亮后再透出，既能看见封面又保证文字对比度；材质模式下随透明度一起淡出
        var backdropStrength = IsLight ? 0.50 : 0.62;
        Backdrop.Opacity = useMaterial
            ? backdropStrength * (0.35 + 0.65 * opacity)
            : backdropStrength;
        BackdropShade.Background = new SolidColorBrush(
            IsLight ? Colors.White : Colors.Black,
            IsLight ? 0.72 : 0.64);

        var liquid = material == "liquid";
        GlassSheen.IsVisible = liquid;
        GlassGlow.IsVisible = liquid;
        GlassEdge.IsVisible = liquid;

        // 纯色背景时，侧边栏与播放条都从同一个背景色派生，保证整窗一致
        var solidTint = !useCover && !useMaterial;
        Color sideColor;
        Color playerColor;
        double sideAlpha;
        double playerAlpha;

        if (solidTint)
        {
            sideColor = BlendColor(page, Colors.Black, IsLight ? 0.035 : 0.22);
            playerColor = BlendColor(page, IsLight ? Colors.White : Colors.Black, IsLight ? 0.62 : 0.1);
            sideAlpha = 1.0;
            playerAlpha = 1.0;
        }
        else if (useMaterial)
        {
            sideColor = BlendColor(coverBase, Colors.Black, IsLight ? 0.04 : 0.2);
            playerColor = BlendColor(coverBase, IsLight ? Colors.White : Colors.Black, IsLight ? 0.55 : 0.08);

            // 透明度作用于整个窗口：侧边栏、内容区、播放栏使用同一个透明度
            sideAlpha = 0.45 + 0.55 * opacity;
            playerAlpha = sideAlpha;
        }
        else if (useCover)
        {
            sideColor = BlendColor(coverBase, Colors.Black, IsLight ? 0.05 : 0.18);
            playerColor = BlendColor(coverBase, IsLight ? Colors.White : Colors.Black, IsLight ? 0.55 : 0.08);
            sideAlpha = IsLight ? 0.98 : 0.78;
            playerAlpha = IsLight ? 1.0 : 0.91;
        }
        else
        {
            sideColor = Color.Parse(IsLight ? "#F0F0F5" : "#0A0A0D");
            playerColor = Color.Parse(IsLight ? "#FBFBFD" : "#0C0C11");
            sideAlpha = IsLight ? 0.98 : 0.78;
            playerAlpha = IsLight ? 1.0 : 0.91;
        }

        SidebarPane.Background = new SolidColorBrush(sideColor, sideAlpha);
        PlayerBar.Background = new SolidColorBrush(playerColor, playerAlpha);
        // 播放列表是浮在内容上的面板：沿用侧边栏的底色并稍作提亮、提高不透明度，
        // 这样在浅色/深色、纯色/封面/材质各种组合下都不会变成一块突兀的灰板
        var panelColor = BlendColor(sideColor, Colors.White, IsLight ? 0.0 : 0.05);
        var panelAlpha = useMaterial ? Math.Min(1.0, sideAlpha + 0.2) : (IsLight ? 0.98 : 0.94);
        QueuePanel.Background = new SolidColorBrush(panelColor, panelAlpha);

        if (useMaterial)
        {
            TransparencyLevelHint = material switch
            {
                "mica" => new[]
                {
                    WindowTransparencyLevel.Mica,
                    WindowTransparencyLevel.AcrylicBlur,
                    WindowTransparencyLevel.Blur,
                    WindowTransparencyLevel.None
                },
                "acrylic" or "liquid" => new[]
                {
                    WindowTransparencyLevel.AcrylicBlur,
                    WindowTransparencyLevel.Blur,
                    WindowTransparencyLevel.None
                },
                _ => new[] { WindowTransparencyLevel.Blur, WindowTransparencyLevel.None }
            };

            Background = Brushes.Transparent;
            RootGrid.Background = new SolidColorBrush(coverBase, 0.45 + 0.55 * opacity);

            if (announceMaterial)
            {
                Dispatcher.UIThread.Post(
                    () => StatusText.Text = $"窗口材质：{ActualTransparencyLevel}",
                    DispatcherPriority.Background);
            }
        }
        else
        {
            TransparencyLevelHint = new[] { WindowTransparencyLevel.None };
            Background = new SolidColorBrush(coverBase);
            RootGrid.Background = new SolidColorBrush(coverBase);
        }

        // 系统标题栏与边框取同一底色，避免顶部出现割裂感
        _windowBaseColor = coverBase;
        WindowChrome.ApplyTitleBar(this, !IsLight, coverBase);
    }

    static Color BlendColor(Color baseColor, Color overlay, double amount)
    {
        var r = (byte)Math.Round(baseColor.R + (overlay.R - baseColor.R) * amount);
        var g = (byte)Math.Round(baseColor.G + (overlay.G - baseColor.G) * amount);
        var b = (byte)Math.Round(baseColor.B + (overlay.B - baseColor.B) * amount);
        return Color.FromArgb(255, r, g, b);
    }

    void ApplyTheme(string theme)
    {
        _config.Theme = theme;
        _config.Save();

        var variant = theme == "light" ? ThemeVariant.Light : ThemeVariant.Dark;
        if (Application.Current is not null) Application.Current.RequestedThemeVariant = variant;

        ApplyBackground();
        UpdateThemeChrome();
        UpdatePlaybackModeUi();
        RefreshNowPlayingText();

        if (NowPlayingOverlay.IsVisible && _currentTrack is not null)
        {
            _ = RefreshOverlayAsync(_currentTrack);
        }

        ShowSettingsPage();
        StatusText.Text = theme == "light" ? "已切换到浅色模式" : "已切换到深色模式";
    }

    void UpdateThemeChrome()
    {
        WindowChrome.ApplyTitleBar(this, !IsLight, _windowBaseColor);
        OverlayBackdrop.Opacity = IsLight ? 0.16 : 0.3;
    }

    static string QualityLabel(Track track)
    {
        if (string.IsNullOrWhiteSpace(track.Format)) return string.Empty;

        var format = track.Format.ToUpperInvariant();
        if (track.BitDepth > 0 && track.SampleRate > 0)
        {
            return $"{format} {track.BitDepth}bit/{track.SampleRate / 1000.0:0.#}kHz";
        }
        if (track.SampleRate > 0)
        {
            return $"{format} {track.SampleRate / 1000.0:0.#}kHz";
        }
        return format;
    }

    string BuildNowSub(Track track)
    {
        var text = string.IsNullOrWhiteSpace(track.AlbumName) ? track.Artist : $"{track.Artist} · {track.AlbumName}";
        if (!_config.ShowAudioQuality) return text;

        var quality = QualityLabel(track);
        return string.IsNullOrEmpty(quality) ? text : $"{text} · {quality}";
    }

    void RefreshNowPlayingText()
    {
        if (_currentTrack is null) return;

        var sub = BuildNowSub(_currentTrack);
        NowSub.Text = sub;
        if (NowPlayingOverlay.IsVisible) OverlaySub.Text = sub;
    }

    TextBox MakeFormBox(string text, string placeholder)
    {
        var box = new TextBox
        {
            Text = text,
            PlaceholderText = placeholder,
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        box.Classes.Add("form");
        return box;
    }

    Button MakeFormButton(string label, bool primary = false)
    {
        var button = new Button
        {
            Content = label,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        button.Classes.Add(primary ? "primary" : "ghost");
        return button;
    }

    string EnsureDeviceId()
    {
        if (string.IsNullOrWhiteSpace(_config.DeviceId))
        {
            _config.DeviceId = MusicApiClient.NewDeviceId();
            _config.Save();
        }
        return _config.DeviceId;
    }

    async Task PasswordLoginAsync(string? server, string? username, string? password, TextBlock status)
    {
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            status.Text = "请填写服务器地址、用户名和密码";
            return;
        }

        try
        {
            status.Text = "登录中…";
            var device = EnsureDeviceId();
            using var api = new MusicApiClient(server.Trim(), null, device);
            var token = await api.LoginWithPasswordAsync(username.Trim(), password);
            ApplySession(server.Trim(), token, username.Trim(), device, "登录成功");
        }
        catch (Exception ex)
        {
            status.Text = $"登录失败：{ex.Message}";
        }
    }

    async Task StartOAuthAsync(string? server, TextBlock status)
    {
        try
        {
            status.Text = "正在打开授权页…";
            var device = EnsureDeviceId();
            using var api = new MusicApiClient(server?.Trim() ?? _config.BaseUrl, null, device);
            var url = await api.BuildOAuthUrlAsync();
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            status.Text = "已在浏览器打开授权页";
        }
        catch (Exception ex)
        {
            status.Text = $"发起授权失败：{ex.Message}";
        }
    }

    async Task SubmitOAuthAsync(string? server, string? input, TextBlock status)
    {
        var code = ExtractCode(input ?? string.Empty);
        if (string.IsNullOrWhiteSpace(code))
        {
            status.Text = "请粘贴授权码或回跳地址";
            return;
        }

        try
        {
            status.Text = "授权登录中…";
            var device = EnsureDeviceId();
            using var api = new MusicApiClient(server?.Trim() ?? _config.BaseUrl, null, device);
            var token = await api.LoginWithOAuthCodeAsync(code);
            ApplySession(server?.Trim() ?? _config.BaseUrl, token, _config.LastUsername, device, "授权登录成功");
        }
        catch (Exception ex)
        {
            status.Text = $"授权失败：{ex.Message}";
        }
    }

    void ApplySession(string server, string token, string? username, string device, string message)
    {
        _config.BaseUrl = server;
        _config.Token = token;
        _config.LastUsername = username;
        _config.DeviceId = device;
        _config.Save();

        _api = null;
        ClearCoverCaches();
        _favorites.Clear();

        ShowSettingsPage(message);
        _ = RefreshFavoriteGuidsAsync();
        _ = RefreshSidebarPlaylistsAsync();
        StatusText.Text = message;
    }

    void Logout()
    {
        _config.Token = null;
        _config.Save();

        _api = null;
        ClearCoverCaches();
        _favorites.Clear();
        SidebarPlaylists.Children.Clear();
        PlaylistCreateBar.IsVisible = false;

        ShowSettingsPage("已退出登录");
        StatusText.Text = "已退出登录";
    }

    static string ExtractCode(string input)
    {
        var index = input.IndexOf("code=", StringComparison.Ordinal);
        if (index < 0) return input.Trim();

        var code = input[(index + 5)..];
        var end = code.IndexOf('&');
        if (end >= 0) code = code[..end];
        return Uri.UnescapeDataString(code);
    }

    MusicApiClient Api() => _api ??= CreateClient();

    MusicApiClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_config.DeviceId))
        {
            _config.DeviceId = MusicApiClient.NewDeviceId();
            _config.Save();
        }

        // 播放代理与业务请求共用同一个客户端，复用连接池，避免重复的 HttpClient 与套接字
        var client = new MusicApiClient(_config.BaseUrl, _config.Token, _config.DeviceId);

        _proxy?.Dispose();
        _proxy = new StreamProxy(client);
        _proxy.Start();

        return client;
    }

    bool HandleError(Exception ex, string what)
    {
        if (ex is MusicApiException apiEx && apiEx.Code == 99999)
        {
            ShowSettingsPage("登录已失效，请重新登录");
            StatusText.Text = "登录已失效，请重新登录";
            return true;
        }

        StatusText.Text = $"{what}: {ex.Message}";
        return false;
    }

    // ---- 页面搭建 ----

    void ResetPageContent()
    {
        // 切换页面时取消上一页仍在进行的封面加载，避免无用的网络与解码开销
        _pageLoadCts?.Cancel();
        _pageLoadCts?.Dispose();
        _pageLoadCts = new CancellationTokenSource();

        ContentPanel.Children.Clear();
        ListHeader.Children.Clear();
        ListRows.Children.Clear();
        _rowImages.Clear();
        _rowBorders.Clear();
        _heartIcons.Clear();
        _rowNumbers.Clear();
    }

    Control BuildPageHeader(string title, string? subtitle, Control? action)
    {
        var head = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), VerticalAlignment = VerticalAlignment.Center };
        var stack = new StackPanel { Spacing = 5 };
        stack.Children.Add(new TextBlock { Text = title, FontSize = 26, FontWeight = FontWeight.Bold, Foreground = TextPrimary });
        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            stack.Children.Add(new TextBlock { Text = subtitle, FontSize = 12.5, Foreground = TextSecondary });
        }
        head.Children.Add(stack);

        if (action is not null)
        {
            Grid.SetColumn(action, 1);
            head.Children.Add(action);
        }

        return head;
    }

    Control BuildPageDivider() => new Border { Height = 1, Background = Divider, Margin = new Thickness(0, 4, 0, 0) };

    void BeginPage(string title, string? subtitle = null, Control? action = null)
    {
        ResetPageContent();
        _infiniteLibrary = false;
        _libraryFooter = null;
        ContentScroll.IsVisible = true;
        ListPageHost.IsVisible = false;
        _pageHost = ContentPanel;

        ContentPanel.Children.Add(BuildPageHeader(title, subtitle, action));
        ContentPanel.Children.Add(BuildPageDivider());

        ScrollToTop();
    }

    /// <summary>列表页：标题与操作区域固定，只让曲目列表滚动。</summary>
    void BeginListPage(string title, string? subtitle = null, Control? action = null)
    {
        ResetPageContent();
        _infiniteLibrary = false;
        _libraryFooter = null;
        ContentScroll.IsVisible = false;
        ListPageHost.IsVisible = true;
        _pageHost = ListRows;

        ListHeader.Children.Add(BuildPageHeader(title, subtitle, action));
        ListHeader.Children.Add(BuildPageDivider());

        ScrollToTop();
    }

    void ScrollToTop()
    {
        ContentScroll.Offset = new Vector(0, 0);
        ListScroll.Offset = new Vector(0, 0);
        Dispatcher.UIThread.Post(() =>
        {
            ContentScroll.Offset = new Vector(0, 0);
            ListScroll.Offset = new Vector(0, 0);
        }, DispatcherPriority.Background);
    }

    void AddSection(string title, int? count = null)
    {
        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 12, 0, 0)
        };
        stack.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeight.SemiBold, Foreground = TextPrimary });
        if (count is int c)
        {
            stack.Children.Add(new TextBlock
            {
                Text = c.ToString(),
                FontSize = 11.5,
                Foreground = TextMuted,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 3)
            });
        }
        _pageHost.Children.Add(stack);
    }

    void AddHint(string text)
    {
        _pageHost.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 13,
            Foreground = TextSecondary,
            Margin = new Thickness(0, 20, 0, 0)
        });
    }

    void AddEmptyState(string title, string desc, Symbol icon)
    {
        var stack = new StackPanel
        {
            Spacing = 10,
            Margin = new Thickness(0, 64, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        stack.Children.Add(MakeIcon(icon, 40, EmptyIcon));
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 15.5,
            FontWeight = FontWeight.SemiBold,
            Foreground = TextPrimary,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        stack.Children.Add(new TextBlock
        {
            Text = desc,
            FontSize = 12,
            Foreground = TextSecondary,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 380
        });
        _pageHost.Children.Add(stack);
    }

    void SetActiveNav(Button? active)
    {
        foreach (var button in new[] { NavHome, NavFavorites, NavRecent, NavAlbums, NavArtists, NavGenres, NavLibrary, NavFolders, NavSettings })
        {
            if (button == active)
            {
                if (!button.Classes.Contains("active")) button.Classes.Add("active");
            }
            else
            {
                button.Classes.Remove("active");
            }
        }
    }

    // ---- 卡片 / 行 ----

    Control BuildCard(string title, string subtitle, string? coverId, Func<Task> onClick, List<(string?, Image)> pending)
    {
        var image = new Image { Stretch = Stretch.UniformToFill };
        var coverGrid = new Grid();
        coverGrid.Children.Add(MakeIcon(Symbol.MusicNote2, 38, NoteStroke));
        coverGrid.Children.Add(image);

        var cover = new Border
        {
            Width = 180,
            Height = 180,
            CornerRadius = new CornerRadius(12),
            ClipToBounds = true,
            Background = CoverPlaceholder,
            Child = coverGrid
        };

        var name = new TextBlock
        {
            Text = title,
            FontSize = 15,
            FontWeight = FontWeight.SemiBold,
            Foreground = TextPrimary,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 180
        };

        var sub = new TextBlock
        {
            Text = subtitle,
            FontSize = 11.5,
            Foreground = TextSecondary,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 180
        };

        var stack = new StackPanel { Spacing = 6 };
        stack.Children.Add(cover);
        stack.Children.Add(name);
        stack.Children.Add(sub);

        var card = new Border
        {
            Width = 204,
            Margin = new Thickness(4, 4, 12, 12),
            CornerRadius = new CornerRadius(14),
            Child = stack,
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Padding = new Thickness(4)
        };

        card.PointerEntered += (_, _) => card.Background = RowHover;
        card.PointerExited += (_, _) => card.Background = Brushes.Transparent;
        card.PointerPressed += async (_, _) => await onClick();

        if (!string.IsNullOrWhiteSpace(coverId)) pending.Add((coverId, image));
        return card;
    }

    async Task LoadCardCoversAsync(List<(string?, Image)> pending)
    {
        var token = _pageLoadCts?.Token ?? CancellationToken.None;
        foreach (var (coverId, image) in pending.ToList())
        {
            if (token.IsCancellationRequested) return;
            var bitmap = await GetCoverAsync(coverId, 180);
            if (bitmap is not null) image.Source = bitmap;
        }
    }

    LruImageCache CacheFor(int size) => size <= 160 ? _thumbCache : size <= 300 ? _artCache : _largeCache;

    async Task<Bitmap?> GetCoverAsync(string? coverId, int size = 300)
    {
        if (string.IsNullOrWhiteSpace(coverId)) return null;

        var key = $"{coverId}@{size}";
        var cache = CacheFor(size);
        if (cache.Get(key) is { } cached) return cached;

        try
        {
            var bytes = await Api().GetCoverAsync(coverId, size);
            // 服务端可能返回原图，按请求宽度解码，避免整张高清封面占用内存
            using var stream = new MemoryStream(bytes);
            var bitmap = Bitmap.DecodeToWidth(stream, size, BitmapInterpolationMode.MediumQuality);
            cache.Set(key, bitmap);
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    void ClearCoverCaches()
    {
        _thumbCache.Clear();
        _artCache.Clear();
        _largeCache.Clear();
        _tintCache.Clear();
    }

    /// <summary>取封面平均色，作为「跟随封面」时的窗口底色。</summary>
    async Task<Color?> GetCoverTintAsync(string? coverId)
    {
        if (string.IsNullOrWhiteSpace(coverId)) return null;
        if (_tintCache.TryGetValue(coverId, out var cached)) return cached;

        try
        {
            var bytes = await Api().GetCoverAsync(coverId, 32);
            using var stream = new MemoryStream(bytes);
            using var small = WriteableBitmap.DecodeToWidth(stream, 8, BitmapInterpolationMode.MediumQuality);
            var color = AverageColor(small);

            if (_tintCache.Count >= 32) _tintCache.Clear();
            _tintCache[coverId] = color;
            return color;
        }
        catch
        {
            return null;
        }
    }

    static Color AverageColor(WriteableBitmap bitmap)
    {
        using var frame = bitmap.Lock();
        var bgra = frame.Format == PixelFormat.Bgra8888;
        var width = frame.Size.Width;
        var height = frame.Size.Height;

        double r = 0, g = 0, b = 0;
        var count = 0;

        for (var y = 0; y < height; y++)
        {
            var row = IntPtr.Add(frame.Address, y * frame.RowBytes);
            for (var x = 0; x < width; x++)
            {
                var px = IntPtr.Add(row, x * 4);
                var c0 = Marshal.ReadByte(px, 0);
                var c1 = Marshal.ReadByte(px, 1);
                var c2 = Marshal.ReadByte(px, 2);
                if (Marshal.ReadByte(px, 3) < 24) continue;

                if (bgra) { b += c0; g += c1; r += c2; }
                else { r += c0; g += c1; b += c2; }
                count++;
            }
        }

        if (count == 0) return Color.FromRgb(128, 128, 128);
        return Color.FromRgb((byte)(r / count), (byte)(g / count), (byte)(b / count));
    }

    /// <summary>把封面平均色调整为适合当前主题的底色：略增饱和，再向黑/白混合保证文字对比度。</summary>
    Color CoverTint(Color color)
    {
        var average = (color.R + color.G + color.B) / 3.0;
        byte Channel(double value) => (byte)Math.Clamp(average + (value - average) * 1.35, 0, 255);
        var vivid = Color.FromRgb(Channel(color.R), Channel(color.G), Channel(color.B));

        return BlendColor(vivid, IsLight ? Colors.White : Colors.Black, IsLight ? 0.76 : 0.52);
    }

    async Task UpdateCoverTintAsync(string? coverId)
    {
        var mode = string.IsNullOrWhiteSpace(_config.BackgroundMode) ? "cover" : _config.BackgroundMode;
        if (mode != "cover") return;

        var tint = await GetCoverTintAsync(coverId);
        if (tint is null) return;

        _coverTint = tint;
        ApplyBackground();
    }

    Control BuildTrackRow(Track track, IReadOnlyList<Track> queue, int index)
    {
        var number = new TextBlock
        {
            Text = (index + 1).ToString(),
            FontSize = 12,
            Foreground = TextMuted,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _rowNumbers[track.Guid] = number;

        var coverImage = new Image { Stretch = Stretch.UniformToFill };
        var coverGrid = new Grid();
        coverGrid.Children.Add(MakeIcon(Symbol.MusicNote2, 14, EmptyIcon));
        coverGrid.Children.Add(coverImage);

        var cover = new Border
        {
            Width = 44,
            Height = 44,
            CornerRadius = new CornerRadius(8),
            ClipToBounds = true,
            Background = CoverBg,
            Child = coverGrid,
            VerticalAlignment = VerticalAlignment.Center
        };
        _rowImages[track.Guid] = coverImage;

        var title = new TextBlock
        {
            Text = track.Title,
            FontSize = 14,
            Foreground = TextPrimary,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var subtitle = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(track.AlbumName) ? track.Artist : $"{track.Artist} · {track.AlbumName}",
            FontSize = 11.5,
            Foreground = TextSecondary,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var texts = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        texts.Children.Add(title);
        texts.Children.Add(subtitle);

        var duration = new TextBlock
        {
            Text = FormatDuration(track.DurationMs),
            FontSize = 11.5,
            Foreground = TextMuted,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0)
        };

        var heartIcon = MakeIcon(Symbol.Heart, 15, IconIdle);
        ApplyHeart(heartIcon, _favorites.Contains(track.Guid));
        _heartIcons[track.Guid] = heartIcon;

        var heart = new Button { Content = heartIcon, VerticalAlignment = VerticalAlignment.Center };
        heart.Classes.Add("tool");
        heart.PointerPressed += (_, e) => e.Handled = true;
        heart.Click += async (_, _) => await ToggleFavoriteAsync(track, heartIcon);

        var plus = new Button
        {
            Content = MakeIcon(Symbol.AddCircle, 15, IconIdle),
            VerticalAlignment = VerticalAlignment.Center
        };
        plus.Classes.Add("tool");
        plus.PointerPressed += (_, e) => e.Handled = true;
        plus.Click += async (_, _) => await AddToPlaylistAsync(track);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("30,58,*,Auto,36,36"),
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Children.Add(number);
        Grid.SetColumn(cover, 1);
        grid.Children.Add(cover);
        Grid.SetColumn(texts, 2);
        grid.Children.Add(texts);
        Grid.SetColumn(duration, 3);
        grid.Children.Add(duration);
        Grid.SetColumn(heart, 4);
        grid.Children.Add(heart);
        Grid.SetColumn(plus, 5);
        grid.Children.Add(plus);

        var row = new Border
        {
            Padding = new Thickness(8, 7),
            CornerRadius = new CornerRadius(10),
            Background = Brushes.Transparent,
            Child = grid,
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        row.PointerEntered += (_, _) =>
        {
            if (!IsPlayingTrack(track)) row.Background = RowHover;
        };
        row.PointerExited += (_, _) =>
        {
            row.Background = IsPlayingTrack(track) ? RowActive : Brushes.Transparent;
        };
        row.PointerPressed += (_, _) => PlayFromQueue(queue, index);

        _rowBorders[track.Guid] = row;
        return row;
    }

    void AddTrackRows(IReadOnlyList<Track> tracks, IReadOnlyList<Track> queue, int startIndex = 0)
    {
        var panel = new StackPanel { Spacing = 2, Margin = new Thickness(0, 2, 0, 0) };
        for (var i = 0; i < tracks.Count; i++)
        {
            panel.Children.Add(BuildTrackRow(tracks[i], queue, startIndex + i));
        }
        _pageHost.Children.Add(panel);
        _ = LoadTrackCoversAsync(tracks);
        UpdateRowHighlight();
    }

    async Task LoadTrackCoversAsync(IEnumerable<Track> tracks)
    {
        var token = _pageLoadCts?.Token ?? CancellationToken.None;
        // 超大列表只预载前若干张缩略图，避免几千次请求和位图常驻内存
        foreach (var track in tracks.Take(500).ToList())
        {
            if (token.IsCancellationRequested) return;
            if (string.IsNullOrWhiteSpace(track.CoverId)) continue;
            var bitmap = await GetCoverAsync(track.CoverId, 96);
            if (bitmap is null) continue;
            if (_rowImages.TryGetValue(track.Guid, out var image)) image.Source = bitmap;
        }
    }

    void ApplyHeart(SymbolIcon icon, bool on)
    {
        icon.Foreground = on ? HeartOn : IconIdle;
        icon.IconVariant = on ? IconVariant.Filled : IconVariant.Regular;
    }

    Control BuildDetailActions(Func<Task> back, List<Track> tracks)
    {
        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };
        stack.Children.Add(MakePill("返回", Symbol.ArrowLeft, back));
        stack.Children.Add(CreatePlayAllAction(tracks));
        return stack;
    }

    Control CreatePlayAllAction(List<Track> tracks)
    {
        var button = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    MakeIcon(Symbol.Play, 12, TextPrimary),
                    new TextBlock { Text = "播放全部", FontSize = 12.5, Foreground = TextPrimary, VerticalAlignment = VerticalAlignment.Center }
                }
            },
            Padding = new Thickness(16, 8),
            Background = PillBg,
            CornerRadius = new CornerRadius(18),
            VerticalAlignment = VerticalAlignment.Center
        };
        button.Click += (_, _) =>
        {
            if (tracks.Count > 0) PlayFromQueue(tracks, 0);
        };
        return button;
    }

    // ---- 浏览 ----

    async Task LoadAlbumsAsync()
    {
        _mode = Mode.Albums;
        SetActiveNav(NavAlbums);
        BeginPage("专辑", "正在加载…");

        try
        {
            using var doc = await Api().GetAlbumsAsync(1, 200);
            _albums = Library.ParseAlbums(doc);

            ContentPanel.Children.Clear();
            BeginPage("专辑", $"共 {_albums.Count} 张专辑");

            var pending = new List<(string?, Image)>();
            var panel = new WrapPanel { Margin = new Thickness(-4, 6, 0, 0) };
            foreach (var album in _albums)
            {
                panel.Children.Add(BuildCard(album.Name, album.Artist, album.CoverId,
                    () => OpenAlbumAsync(album), pending));
            }
            ContentPanel.Children.Add(panel);
            _ = LoadCardCoversAsync(pending);

            StatusText.Text = $"专辑 {_albums.Count} 张";
            var first = _albums.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.CoverId));
            if (first is not null)
            {
                _ = SetBackdropAsync(first.CoverId);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载专辑失败");
        }
    }

    async Task SetBackdropAsync(string? coverId)
    {
        var bitmap = await GetCoverAsync(coverId, 480);
        if (bitmap is not null) Backdrop.Source = bitmap;
        await UpdateCoverTintAsync(coverId);
    }

    async Task OpenAlbumAsync(Album album)
    {
        Func<Task> back = LoadAlbumsAsync;
        if (_mode == Mode.Search && _lastSearch.Length > 0) back = () => SearchAsync(_lastSearch);

        try
        {
            using var doc = await Api().GetTracksByAlbumAsync(album.Guid);
            _tracks = Library.ParseTracks(doc);
            _mode = Mode.Tracks;
            BeginListPage(album.Name, $"专辑 · {album.Artist} · {_tracks.Count} 首", BuildDetailActions(back, _tracks));
            AddTrackRows(_tracks, _tracks);
            if (_tracks.Count == 0) AddEmptyState("暂无歌曲", "这张专辑还没有曲目", Symbol.MusicNote2);
            StatusText.Text = $"{album.Name} · {_tracks.Count} 首";
            _ = SetBackdropAsync(album.CoverId);
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载专辑曲目失败");
        }
    }

    async Task LoadAllTracksAsync()
    {
        _mode = Mode.Tracks;
        SetActiveNav(NavLibrary);
        BeginListPage("全部歌曲", "正在加载…");
        _infiniteLibrary = true;
        _libraryPage = 0;
        _libraryTotal = -1;
        _librarySeen.Clear();
        _tracks = new List<Track>();
        await LoadLibraryPageAsync();
    }

    void OnListScrolled()
    {
        if (!_infiniteLibrary || _libraryLoading) return;
        if (_libraryTotal >= 0 && _tracks.Count >= _libraryTotal) return;
        if (ListScroll.Extent.Height <= 0) return;

        if (ListScroll.Offset.Y + ListScroll.Viewport.Height >= ListScroll.Extent.Height - 160)
        {
            _ = LoadLibraryPageAsync();
        }
    }

    async Task LoadLibraryPageAsync()
    {
        if (_libraryLoading) return;
        if (_libraryTotal >= 0 && _tracks.Count >= _libraryTotal) return;

        var token = _pageLoadCts?.Token;
        _libraryLoading = true;
        RefreshLibraryFooter();

        try
        {
            _libraryPage++;
            using var doc = await Api().GetTracksAsync(_libraryPage, 200);
            if (token is { IsCancellationRequested: true }) return;

            var batch = Library.ParseTracks(doc);
            if (batch.Count == 0)
            {
                if (_libraryTotal < 0) _libraryTotal = _tracks.Count;
                return;
            }

            if (_libraryTotal < 0) _libraryTotal = PageTotal(doc);

            var added = new List<Track>();
            var startIndex = _tracks.Count;
            foreach (var track in batch)
            {
                if (!_librarySeen.Add(track.Guid)) continue;
                _tracks.Add(track);
                added.Add(track);
            }

            if (startIndex == 0)
            {
                // 第一页拿到后确定标题与总数，再逐页追加
                BeginListPage("全部歌曲",
                    _libraryTotal >= 0 ? $"共 {_libraryTotal} 首" : $"已加载 {_tracks.Count} 首",
                    CreatePlayAllAction(_tracks));
                _infiniteLibrary = true;
                AddTrackRows(_tracks, _tracks);
            }
            else if (added.Count > 0)
            {
                AddTrackRows(added, _tracks, startIndex);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载歌曲失败");
        }
        finally
        {
            _libraryLoading = false;
            RefreshLibraryFooter();
        }
    }

    void RefreshLibraryFooter()
    {
        if (!_infiniteLibrary) return;

        _libraryFooter ??= new TextBlock
        {
            FontSize = 11.5,
            Foreground = TextMuted,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 14, 0, 6)
        };

        if (_libraryFooter.Parent is Panel parent) parent.Children.Remove(_libraryFooter);

        _libraryFooter.Text = _libraryLoading
            ? "正在加载…"
            : _libraryTotal >= 0 && _tracks.Count >= _libraryTotal
                ? $"已加载全部 {_tracks.Count} 首"
                : "滚动到底部加载更多";

        _pageHost.Children.Add(_libraryFooter);
    }

    static int PageTotal(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            return -1;
        }

        return data.TryGetProperty("total", out var total) && total.ValueKind == JsonValueKind.Number
            ? total.GetInt32()
            : -1;
    }

    async Task LoadHistoryAsync()
    {
        _mode = Mode.History;
        SetActiveNav(NavRecent);
        BeginListPage("最近播放", "正在加载…");
        try
        {
            using var doc = await Api().GetPlayHistoryAsync(1, 200);
            _tracks = Library.ParseTracks(doc);
            ContentPanel.Children.Clear();
            BeginListPage("最近播放", $"共 {_tracks.Count} 首", CreatePlayAllAction(_tracks));
            AddTrackRows(_tracks, _tracks);
            if (_tracks.Count == 0) AddEmptyState("暂无播放记录", "在飞牛音乐中播放过的歌曲会出现在这里", Symbol.Clock);
            StatusText.Text = $"最近 {_tracks.Count} 首";
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载历史失败");
        }
    }

    async Task LoadGenresAsync()
    {
        _mode = Mode.Genres;
        SetActiveNav(NavGenres);
        BeginPage("风格", "按风格浏览曲库");
        try
        {
            using var doc = await Api().GetGenresAsync(1, 200);
            _genres = Library.ParseGenres(doc);

            var panel = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            foreach (var genre in _genres)
            {
                panel.Children.Add(BuildGenreCard(genre));
            }
            ContentPanel.Children.Add(panel);

            if (_genres.Count == 0) AddEmptyState("暂无流派", "曲库还没有分类信息", Symbol.MusicNote2);
            StatusText.Text = $"风格 {_genres.Count} 个";
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载流派失败");
        }
    }

    Control BuildGenreCard(Genre genre)
    {
        var card = new Button
        {
            Width = 176,
            Height = 76,
            Margin = new Thickness(0, 0, 10, 10),
            Padding = new Thickness(14),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Content = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock
                    {
                        Text = genre.Name,
                        FontSize = 15,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = TextPrimary,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        MaxWidth = 148
                    },
                    new TextBlock { Text = $"{genre.TrackCount} 首", FontSize = 11.5, Foreground = TextSecondary }
                }
            },
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        card.Classes.Add("genre");

        card.Click += async (_, _) => await OpenGenreAsync(genre);
        return card;
    }

    async Task OpenGenreAsync(Genre genre)
    {
        try
        {
            using var doc = await Api().GetTracksByGenreAsync(genre.Guid);
            _tracks = Library.ParseTracks(doc);
            _mode = Mode.Tracks;
            BeginListPage(genre.Name, $"风格 · {_tracks.Count} 首", BuildDetailActions(LoadGenresAsync, _tracks));
            AddTrackRows(_tracks, _tracks);
            if (_tracks.Count == 0) AddEmptyState("暂无歌曲", "这个流派下还没有曲目", Symbol.MusicNote2);
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载流派曲目失败");
        }
    }

    async Task OpenArtistAsync(Artist artist)
    {
        Func<Task> back = LoadAlbumsAsync;
        if (_mode == Mode.Search && _lastSearch.Length > 0) back = () => SearchAsync(_lastSearch);

        try
        {
            using var doc = await Api().GetTracksByArtistAsync(artist.Guid);
            _tracks = Library.ParseTracks(doc);
            _mode = Mode.Tracks;
            BeginListPage(artist.Name, $"歌手 · {_tracks.Count} 首", BuildDetailActions(back, _tracks));
            AddTrackRows(_tracks, _tracks);
            if (_tracks.Count == 0) AddEmptyState("暂无歌曲", "这位歌手还没有曲目", Symbol.MusicNote2);
            _ = SetBackdropAsync(artist.CoverId);
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载歌手曲目失败");
        }
    }

    async Task LoadRoamAsync()
    {
        _mode = Mode.Roam;
        SetActiveNav(NavHome);

        // 已经在漫游中：只重新展示当前推荐，不重新发起漫游、不打断当前播放
        if (_roaming && _roamId is not null && _currentTrack is not null)
        {
            BeginPage("漫游", "根据收藏与播放记录持续推荐");
            BuildRoamHero(_currentTrack);
            StatusText.Text = "漫游进行中";
            return;
        }

        BeginPage("漫游", "根据收藏与播放记录持续推荐");

        try
        {
            using var doc = await Api().RoamStartAsync();
            var result = Library.ParseRoam(doc);
            if (result is null)
            {
                AddEmptyState("暂时没有推荐", "多收藏、多播放几首歌后再来漫游", Symbol.MusicNote2);
                return;
            }

            _roaming = true;
            _roamId = result.Value.RoamId;
            _queue = new List<Track> { result.Value.Track };
            _queueIndex = 0;

            BuildRoamHero(result.Value.Track);
            PlayTrack(result.Value.Track);
            StatusText.Text = "漫游已开始";
        }
        catch (Exception ex)
        {
            HandleError(ex, "开始漫游失败");
        }
    }

    void BuildRoamHero(Track track)
    {
        var image = new Image { Stretch = Stretch.UniformToFill };
        _roamCover = image;

        var coverGrid = new Grid();
        coverGrid.Children.Add(MakeIcon(Symbol.MusicNote2, 56, EmptyIcon));
        coverGrid.Children.Add(image);

        var cover = new Border
        {
            Width = 232,
            Height = 232,
            CornerRadius = new CornerRadius(18),
            ClipToBounds = true,
            Background = CoverPlaceholder,
            Child = coverGrid
        };

        var eyebrow = new TextBlock
        {
            Text = "为你漫游",
            FontSize = 11.5,
            FontWeight = FontWeight.SemiBold,
            Foreground = AccentSoft
        };

        var title = new TextBlock
        {
            Text = track.Title,
            FontSize = 30,
            FontWeight = FontWeight.Bold,
            Foreground = TextPrimary,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 540
        };
        _roamTitle = title;

        var sub = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(track.AlbumName)
                ? track.Artist
                : $"{track.Artist} · {track.AlbumName}",
            FontSize = 13,
            Foreground = TextSecondary,
            TextWrapping = TextWrapping.Wrap
        };
        _roamSub = sub;

        var meta = new TextBlock
        {
            Text = $"时长 {FormatDuration(track.DurationMs)}",
            FontSize = 11.5,
            Foreground = TextMuted
        };
        _roamMeta = meta;

        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(0, 10, 0, 0)
        };
        controls.Children.Add(MakePill("上一首", Symbol.PreviousFrame, () => RoamStepAsync(-1)));

        var playIcon = MakeIcon(
            _player?.IsPlaying == true ? Symbol.Pause : Symbol.Play,
            16, PlayButtonFg, filled: true);
        _roamPlayIcon = playIcon;

        var playButton = new Button { Content = playIcon, VerticalAlignment = VerticalAlignment.Center };
        playButton.Classes.Add("playmain");
        playButton.Click += (_, _) => TogglePlay();
        controls.Children.Add(playButton);

        controls.Children.Add(MakePill("下一首", Symbol.NextFrame, () => RoamStepAsync(1)));

        var textStack = new StackPanel
        {
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(30, 0, 0, 0)
        };
        textStack.Children.Add(eyebrow);
        textStack.Children.Add(title);
        textStack.Children.Add(sub);
        textStack.Children.Add(meta);
        textStack.Children.Add(controls);

        var heroGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        heroGrid.Children.Add(cover);
        Grid.SetColumn(textStack, 1);
        heroGrid.Children.Add(textStack);

        var hero = new Border
        {
            Background = HeroBg,
            CornerRadius = new CornerRadius(22),
            Padding = new Thickness(22),
            Child = heroGrid
        };

        ContentPanel.Children.Add(hero);
        AddHint("漫游会沿着当前曲目持续推荐，切歌请使用上一首 / 下一首。");
        _ = LoadRoamCoverAsync(track.CoverId);
        _ = SetBackdropAsync(track.CoverId);
    }

    Button MakePill(string label, Symbol symbol, Func<Task> action)
    {
        var button = new Button
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children =
                {
                    MakeIcon(symbol, 11, TextPrimary),
                    new TextBlock
                    {
                        Text = label,
                        FontSize = 12.5,
                        Foreground = TextPrimary,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            },
            VerticalAlignment = VerticalAlignment.Center
        };
        button.Classes.Add("pill");
        button.Click += async (_, _) => await action();
        return button;
    }

    async Task LoadRoamCoverAsync(string? coverId)
    {
        var cover = await GetCoverAsync(coverId, 500);
        if (cover is null || _roamCover is null) return;
        _roamCover.Source = cover;
    }

    void SyncRoamHero(Track track)
    {
        if (_mode != Mode.Roam) return;

        if (_roamTitle is not null) _roamTitle.Text = track.Title;
        if (_roamSub is not null)
        {
            _roamSub.Text = BuildNowSub(track);
        }
        if (_roamMeta is not null) _roamMeta.Text = $"时长 {FormatDuration(track.DurationMs)}";
        if (_roamPlayIcon is not null)
        {
            _roamPlayIcon.Symbol = _player?.IsPlaying == true ? Symbol.Pause : Symbol.Play;
        }

        _ = LoadRoamCoverAsync(track.CoverId);
        _ = SetBackdropAsync(track.CoverId);
    }

    async Task<bool> RoamStepAsync(int dir)
    {
        if (_roamId is null)
        {
            await LoadRoamAsync();
            return true;
        }

        try
        {
            using var doc = dir > 0
                ? await Api().RoamNextAsync(_roamId)
                : await Api().RoamPreviousAsync(_roamId);

            var result = Library.ParseRoam(doc);
            if (result is null) return false;

            _roamId = result.Value.RoamId;
            _queue = new List<Track> { result.Value.Track };
            _queueIndex = 0;

            SyncRoamHero(result.Value.Track);
            PlayTrack(result.Value.Track);
            return true;
        }
        catch (Exception ex)
        {
            HandleError(ex, "漫游切歌失败");
            return false;
        }
    }

    async Task LoadFavoritesAsync()
    {
        _mode = Mode.Favorites;
        SetActiveNav(NavFavorites);
        BeginListPage("我的收藏", "正在加载…");
        try
        {
            using var doc = await Api().GetFavoritesAsync();
            _tracks = Library.ParseTracks(doc);
            _favorites.Clear();
            foreach (var track in _tracks) _favorites.Add(track.Guid);

            ContentPanel.Children.Clear();
            BeginListPage("我的收藏", $"共 {_tracks.Count} 首", CreatePlayAllAction(_tracks));
            AddTrackRows(_tracks, _tracks);
            if (_tracks.Count == 0) AddEmptyState("还没有收藏", "点击歌曲右侧的心形图标即可收藏", Symbol.Heart);
            StatusText.Text = $"收藏 {_tracks.Count} 首";
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载收藏失败");
        }
    }

    async Task RefreshFavoriteGuidsAsync()
    {
        try
        {
            using var doc = await Api().GetFavoritesAsync();
            _favorites.Clear();
            foreach (var track in Library.ParseTracks(doc)) _favorites.Add(track.Guid);
        }
        catch
        {
            // 收藏状态加载失败不影响主流程
        }
    }

    async Task RefreshSidebarPlaylistsAsync()
    {
        try
        {
            using var doc = await Api().GetPlaylistsAsync();
            BuildSidebarPlaylists(Library.ParsePlaylists(doc));
        }
        catch
        {
            // 歌单加载失败不影响其它页面
        }
    }

    void BuildSidebarPlaylists(List<Playlist> playlists)
    {
        SidebarPlaylists.Children.Clear();

        if (playlists.Count == 0)
        {
            SidebarPlaylists.Children.Add(new TextBlock
            {
                Text = "还没有歌单",
                FontSize = 11,
                Foreground = TextMuted,
                Margin = new Thickness(14, 4, 0, 4)
            });
            return;
        }

        foreach (var playlist in playlists)
        {
            var button = new Button
            {
                Padding = new Thickness(10, 6),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 10,
                    Children =
                    {
                        PlaylistIcon(),
                        new TextBlock
                        {
                            Text = playlist.Name,
                            FontSize = 12.5,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        }
                    }
                }
            };
            button.Classes.Add("nav");
            button.Click += async (_, _) => await OpenPlaylistAsync(playlist);
            SidebarPlaylists.Children.Add(button);
        }
    }

    async Task CreatePlaylistFromSidebarAsync()
    {
        var name = PlaylistNameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusText.Text = "请先填写歌单名称";
            PlaylistNameBox.Focus();
            return;
        }

        try
        {
            await Api().CreatePlaylistAsync(name);
            PlaylistNameBox.Text = string.Empty;
            PlaylistCreateBar.IsVisible = false;
            StatusText.Text = $"已创建歌单「{name}」";
            await RefreshSidebarPlaylistsAsync();
        }
        catch (Exception ex)
        {
            HandleError(ex, "创建歌单失败");
        }
    }

    // ---- 首页 / 歌手 / 文件夹 ----

    async Task LoadHomeAsync()
    {
        _mode = Mode.Home;
        SetActiveNav(NavHome);
        BeginPage("首页", "欢迎回来");

        var recent = new List<Track>();
        try
        {
            using var doc = await Api().GetPlayHistoryAsync(1, 20);
            recent = Library.ParseTracks(doc).Take(12).ToList();
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载最近播放失败");
        }

        var favorites = new List<Track>();
        try
        {
            using var doc = await Api().GetFavoritesAsync(1, 12);
            favorites = Library.ParseTracks(doc);
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载收藏失败");
        }

        var albums = new List<Album>();
        try
        {
            using var doc = await Api().GetAlbumsAsync(1, 8);
            albums = Library.ParseAlbums(doc).Take(8).ToList();
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载专辑失败");
        }

        var latest = new List<Track>();
        try
        {
            using var doc = await Api().GetTracksAsync(1, 12);
            latest = Library.ParseTracks(doc).Take(12).ToList();
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载最近添加歌曲失败");
        }

        // 顶部四个入口卡片：封面拼贴 + 标题
        var shortcutCovers = new List<(string?, Image)>();
        var cards = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*,*") };

        var roamCard = BuildHomeShortcutCard("漫游", recent.Select(track => track.CoverId), LoadRoamAsync, shortcutCovers);
        Grid.SetColumn(roamCard, 0);
        cards.Children.Add(roamCard);

        var favoritesCard = BuildHomeShortcutCard(
            "收藏",
            favorites.Select(track => track.CoverId)
                .Concat(recent.Select(track => track.CoverId))
                .Distinct(),
            LoadFavoritesAsync,
            shortcutCovers);
        Grid.SetColumn(favoritesCard, 1);
        cards.Children.Add(favoritesCard);

        var recentCard = BuildHomeShortcutCard("最近播放", recent.Select(track => track.CoverId), LoadHistoryAsync, shortcutCovers);
        Grid.SetColumn(recentCard, 2);
        cards.Children.Add(recentCard);

        var latestCovers = latest.Select(track => track.CoverId).Concat(albums.Select(album => album.CoverId));
        var latestCard = BuildHomeShortcutCard("最近添加", latestCovers, LoadAllTracksAsync, shortcutCovers);
        Grid.SetColumn(latestCard, 3);
        cards.Children.Add(latestCard);

        ContentPanel.Children.Add(cards);
        _ = LoadHomeCardCoversAsync(shortcutCovers);

        if (recent.Count > 0)
        {
            AddSection("最近播放", recent.Count);
            AddTrackRows(recent, recent);
        }

        if (albums.Count > 0)
        {
            AddSection("最近添加专辑", albums.Count);
            var pending = new List<(string?, Image)>();
            var panel = new WrapPanel { Margin = new Thickness(-4, 2, 0, 0) };
            foreach (var album in albums)
            {
                panel.Children.Add(BuildCard(album.Name, album.Artist, album.CoverId,
                    () => OpenAlbumAsync(album), pending));
            }
            ContentPanel.Children.Add(panel);
            _ = LoadCardCoversAsync(pending);
        }

        if (latest.Count > 0)
        {
            AddSection("最近添加歌曲", latest.Count);
            AddTrackRows(latest, latest);
        }

        if (recent.Count == 0 && albums.Count == 0 && latest.Count == 0)
        {
            AddEmptyState("欢迎使用 25DogMusic", "去「音乐库」挑一首歌开始播放吧", Symbol.MusicNote2);
        }

        StatusText.Text = "首页";
    }

    Control BuildHomeShortcutCard(string title, IEnumerable<string?> coverIds, Func<Task> onClick, List<(string?, Image)> pending)
    {
        var covers = coverIds.Where(id => !string.IsNullOrWhiteSpace(id)).Take(4).ToList();

        var mosaic = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*"),
            RowDefinitions = new RowDefinitions("*,*")
        };

        for (var i = 0; i < 4; i++)
        {
            var image = new Image { Stretch = Stretch.UniformToFill };
            if (i < covers.Count)
            {
                pending.Add((covers[i], image));
            }

            var cell = new Border
            {
                Background = CoverPlaceholder,
                ClipToBounds = true,
                Margin = new Thickness(1),
                Child = image
            };
            Grid.SetColumn(cell, i % 2);
            Grid.SetRow(cell, i / 2);
            mosaic.Children.Add(cell);
        }

        var label = new Border
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = new SolidColorBrush(Colors.Black, 0.55),
            Padding = new Thickness(10, 6),
            Child = new TextBlock
            {
                Text = title,
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brushes.White
            }
        };

        var content = new Grid();
        content.Children.Add(mosaic);
        content.Children.Add(label);

        var card = new Border
        {
            Height = 112,
            Margin = new Thickness(0, 0, 10, 0),
            CornerRadius = new CornerRadius(14),
            ClipToBounds = true,
            Background = CoverPlaceholder,
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = content
        };

        card.PointerPressed += async (_, _) => await onClick();
        return card;
    }

    async Task LoadHomeCardCoversAsync(List<(string?, Image)> pending)
    {
        var token = _pageLoadCts?.Token ?? CancellationToken.None;
        foreach (var (coverId, image) in pending)
        {
            if (token.IsCancellationRequested) return;
            var bitmap = await GetCoverAsync(coverId, 110);
            if (bitmap is not null) image.Source = bitmap;
        }
    }

    async Task LoadArtistsAsync()
    {
        _mode = Mode.Artists;
        SetActiveNav(NavArtists);
        BeginPage("歌手", "按歌手浏览曲库");

        try
        {
            using var doc = await Api().GetArtistsAsync(1, 200);
            var artists = Library.ParseArtists(doc);

            var pending = new List<(string?, Image)>();
            var panel = new StackPanel { Spacing = 2, Margin = new Thickness(0, 4, 0, 0) };
            foreach (var artist in artists)
            {
                panel.Children.Add(BuildArtistRow(artist, pending));
            }
            ContentPanel.Children.Add(panel);
            _ = LoadCardCoversAsync(pending);

            if (artists.Count == 0) AddEmptyState("暂无歌手", "曲库还没有歌手信息", Symbol.MusicNote2);
            StatusText.Text = $"歌手 {artists.Count} 位";
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载歌手失败");
        }
    }

    void ShowFoldersPage()
    {
        _mode = Mode.Folders;
        SetActiveNav(NavFolders);
        BeginPage("文件夹", "按目录浏览音乐");
        AddEmptyState("暂不支持文件夹浏览", "当前服务端接口没有提供目录列表，可先用「音乐库」或「专辑」浏览", Symbol.TextBulletList);
        StatusText.Text = "文件夹功能待服务端支持";
    }

    async Task OpenPlaylistAsync(Playlist playlist)
    {
        Func<Task> back = LoadHomeAsync;
        if (_mode == Mode.Search && _lastSearch.Length > 0) back = () => SearchAsync(_lastSearch);

        try
        {
            using var doc = await Api().GetPlaylistTracksAsync(playlist.Guid);
            _tracks = Library.ParseTracks(doc);
            _mode = Mode.Tracks;
            BeginListPage(playlist.Name, $"歌单 · {_tracks.Count} 首", BuildDetailActions(back, _tracks));
            AddTrackRows(_tracks, _tracks);
            if (_tracks.Count == 0) AddEmptyState("歌单是空的", "在歌曲右侧点击「+」即可加入歌单", Symbol.TextBulletList);
            if (!string.IsNullOrWhiteSpace(playlist.CoverId)) _ = SetBackdropAsync(playlist.CoverId);
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载歌单失败");
        }
    }

    async Task SearchAsync(string keyword)
    {
        keyword = keyword.Trim();
        if (string.IsNullOrWhiteSpace(keyword)) return;

        _mode = Mode.Search;
        _lastSearch = keyword;
        SetActiveNav(null);
        BeginPage($"搜索 “{keyword}”", "歌曲 · 专辑 · 歌手 · 歌单");

        var found = false;
        var pendingImages = new List<(string?, Image)>();

        try
        {
            using var doc = await Api().SearchTracksAsync(keyword);
            var tracks = Library.ParseTracks(doc);
            if (tracks.Count > 50) tracks = tracks.Take(50).ToList();
            if (tracks.Count > 0)
            {
                found = true;
                AddSection("歌曲", tracks.Count);
                AddTrackRows(tracks, tracks);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "搜索歌曲失败");
        }

        try
        {
            using var doc = await Api().SearchAlbumsAsync(keyword);
            var albums = Library.ParseAlbums(doc);
            if (albums.Count > 0)
            {
                found = true;
                AddSection("专辑", albums.Count);
                var panel = new WrapPanel { Margin = new Thickness(-4, 4, 0, 0) };
                foreach (var album in albums)
                {
                    panel.Children.Add(BuildCard(album.Name, album.Artist, album.CoverId,
                        () => OpenAlbumAsync(album), pendingImages));
                }
                ContentPanel.Children.Add(panel);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "搜索专辑失败");
        }

        try
        {
            using var doc = await Api().SearchArtistsAsync(keyword);
            var artists = Library.ParseArtists(doc);
            if (artists.Count > 0)
            {
                found = true;
                AddSection("歌手", artists.Count);
                var panel = new StackPanel { Spacing = 2, Margin = new Thickness(0, 2, 0, 0) };
                foreach (var artist in artists)
                {
                    panel.Children.Add(BuildArtistRow(artist, pendingImages));
                }
                ContentPanel.Children.Add(panel);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "搜索歌手失败");
        }

        try
        {
            using var doc = await Api().SearchPlaylistsAsync(keyword);
            var playlists = Library.ParsePlaylists(doc);
            if (playlists.Count > 0)
            {
                found = true;
                AddSection("歌单", playlists.Count);
                var panel = new WrapPanel { Margin = new Thickness(-4, 4, 0, 0) };
                foreach (var playlist in playlists)
                {
                    panel.Children.Add(BuildCard(playlist.Name, $"{playlist.TrackCount} 首", playlist.CoverId,
                        () => OpenPlaylistAsync(playlist), pendingImages));
                }
                ContentPanel.Children.Add(panel);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "搜索歌单失败");
        }

        _ = LoadCardCoversAsync(pendingImages);

        if (!found) AddEmptyState("没有找到相关内容", "换个关键词试试", Symbol.Search);
        StatusText.Text = $"搜索：{keyword}";
        ScrollToTop();
    }

    Control BuildArtistRow(Artist artist, List<(string?, Image)> pending)
    {
        var image = new Image { Stretch = Stretch.UniformToFill };
        var avatar = new Border
        {
            Width = 44,
            Height = 44,
            CornerRadius = new CornerRadius(22),
            ClipToBounds = true,
            Background = CardBg,
            Child = image
        };
        if (!string.IsNullOrWhiteSpace(artist.CoverId)) pending.Add((artist.CoverId, image));

        var name = new TextBlock { Text = artist.Name, FontSize = 13.5, Foreground = TextPrimary, TextTrimming = TextTrimming.CharacterEllipsis };
        var sub = new TextBlock { Text = $"{artist.TrackCount} 首 · {artist.AlbumCount} 张专辑", FontSize = 11, Foreground = TextSecondary };
        var texts = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
        texts.Children.Add(name);
        texts.Children.Add(sub);

        var row = new Border
        {
            Padding = new Thickness(8, 5),
            CornerRadius = new CornerRadius(9),
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        grid.Children.Add(avatar);
        Grid.SetColumn(texts, 1);
        grid.Children.Add(texts);
        row.Child = grid;

        row.PointerEntered += (_, _) => row.Background = RowHover;
        row.PointerExited += (_, _) => row.Background = Brushes.Transparent;
        row.PointerPressed += async (_, _) => await OpenArtistAsync(artist);
        return row;
    }

    // ---- 收藏 / 歌单操作 ----

    async Task ToggleFavoriteAsync(Track track, SymbolIcon heartIcon)
    {
        var wasFavorite = _favorites.Contains(track.Guid);
        try
        {
            if (wasFavorite)
            {
                await Api().RemoveFavoriteAsync(track.Guid);
                _favorites.Remove(track.Guid);
            }
            else
            {
                await Api().AddFavoriteAsync(track.Guid);
                _favorites.Add(track.Guid);
            }

            ApplyHeart(heartIcon, !wasFavorite);
            StatusText.Text = wasFavorite ? "已取消收藏" : "已加入收藏";

            if (wasFavorite && _mode == Mode.Favorites)
            {
                await LoadFavoritesAsync();
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, wasFavorite ? "取消收藏失败" : "收藏失败");
        }
    }

    async Task AddToPlaylistAsync(Track track)
    {
        try
        {
            var picker = new PlaylistPickerWindow(Api(), track);
            await picker.ShowDialog(this);
            if (picker.SelectedGuid is null) return;

            await Api().AddTrackToPlaylistAsync(picker.SelectedGuid, track.Guid);
            StatusText.Text = $"已把「{track.Title}」加入歌单";
        }
        catch (Exception ex)
        {
            HandleError(ex, "加入歌单失败");
        }
    }

    // ---- 播放 ----

    bool IsPlayingTrack(Track track) => _currentTrack?.Guid == track.Guid;

    void PlayFromQueue(IReadOnlyList<Track> queue, int index)
    {
        if (index < 0 || index >= queue.Count) return;
        _roaming = false;
        _roamId = null;
        _queue = queue.ToList();
        _queueIndex = index;
        PlayTrack(queue[index]);
    }

    void TogglePlay()
    {
        if (_player is null) return;

        if (_player.IsPlaying)
        {
            _player.TogglePause();
            _paused = true;
            SetPlayIcon(false);
            SyncTimer();
            return;
        }

        if (_paused && _currentTrack is not null)
        {
            _player.TogglePause();
            _paused = false;
            SetPlayIcon(true);
            SyncTimer();
            return;
        }

        if (_queue.Count > 0)
        {
            _queueIndex = Math.Clamp(_queueIndex, 0, _queue.Count - 1);
            PlayTrack(_queue[_queueIndex]);
        }
        else if (_currentTrack is not null)
        {
            PlayTrack(_currentTrack);
        }
    }

    void PlayTrack(Track track)
    {
        if (_player is null) return;
        if (_proxy is null) Api();

        _player.Play(_proxy!.BaseUrl + track.Guid);
        _paused = false;
        _ended = false;
        _currentTrack = track;

        // 后台播放时跳过所有界面构建（封面、行高亮、全屏歌词），只保留播放本身
        if (!IsBackgrounded)
        {
            SetPlayIcon(true);
            Title = $"{track.Title} · {track.Artist} — 25DogMusic";
            NowTitle.Text = track.Title;
            NowSub.Text = BuildNowSub(track);

            UpdateRowHighlight();

            if (NowPlayingOverlay.IsVisible)
            {
                OverlayTitle.Text = track.Title;
                OverlaySub.Text = NowSub.Text;
                _ = RefreshOverlayAsync(track);
            }

            _ = LoadNowCoverAsync(track.CoverId);
        }

        _ = Api().ReportTrackPlayAsync(track.Guid).ContinueWith(_ => { });

        StartTimer();
    }

    void UpdateRowHighlight()
    {
        foreach (var (guid, border) in _rowBorders)
        {
            border.Background = guid == _currentTrack?.Guid ? RowActive : Brushes.Transparent;
        }

        foreach (var (guid, number) in _rowNumbers)
        {
            number.Foreground = guid == _currentTrack?.Guid ? AccentSoft : TextMuted;
            number.FontWeight = guid == _currentTrack?.Guid ? FontWeight.SemiBold : FontWeight.Normal;
        }

        UpdateQueueDrawerHighlight();
    }

    async Task LoadNowCoverAsync(string? coverId)
    {
        var cover = await GetCoverAsync(coverId, 480);
        if (cover is null) return;

        NowCover.Source = cover;
        Backdrop.Source = cover;
        await UpdateCoverTintAsync(coverId);
    }

    async Task<bool> AdvanceAsync(int dir, bool allowWrap)
    {
        if (_roaming) return await RoamStepAsync(dir);

        if (_queue.Count == 0) return false;

        if (_shuffle)
        {
            if (_queue.Count == 1)
            {
                PlayTrack(_queue[0]);
                _queueIndex = 0;
                return true;
            }

            var pick = Random.Shared.Next(_queue.Count - 1);
            var next = pick >= _queueIndex ? pick + 1 : pick;
            _queueIndex = next;
            PlayTrack(_queue[next]);
            return true;
        }

        var nextIndex = _queueIndex + dir;
        if (nextIndex < 0 || nextIndex >= _queue.Count)
        {
            if (!allowWrap) return false;
            nextIndex = nextIndex < 0 ? _queue.Count - 1 : 0;
        }

        _queueIndex = nextIndex;
        PlayTrack(_queue[nextIndex]);
        return true;
    }

    async void OnEndReached()
    {
        if (_currentTrack is null) return;

        if (_repeat == 2)
        {
            PlayTrack(_currentTrack);
            return;
        }

        if (!await AdvanceAsync(1, allowWrap: _repeat == 1))
        {
            _paused = false;
            _ended = true;
            SetPlayIcon(false);
            SyncTimer();
        }
    }

    bool IsBackgrounded => _hiddenToTray || WindowState == WindowState.Minimized;

    void EnsureTimer()
    {
        if (_timer is not null) return;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += (_, _) => UpdatePlaybackUi();
    }

    void StartTimer()
    {
        EnsureTimer();
        SyncTimer();
        UpdatePlaybackUi();
    }

    /// <summary>播放中（含 VLC 正在 Open/缓冲）刷新界面；暂停、播完或后台时停表。</summary>
    void SyncTimer()
    {
        EnsureTimer();
        var active = _currentTrack is not null && !_paused && !_ended;
        if (active && !IsBackgrounded)
        {
            if (!_timer!.IsEnabled) _timer.Start();
        }
        else
        {
            _timer?.Stop();
        }
    }

    void UpdatePlaybackUi()
    {
        if (_player is null || IsBackgrounded) return;

        var position = TimeSpan.FromMilliseconds(Math.Max(0, _player.Position));
        var durationMs = _currentTrack is not null && _currentTrack.DurationMs > 0
            ? _currentTrack.DurationMs
            : Math.Max(0, _player.Duration);
        var duration = TimeSpan.FromMilliseconds(durationMs);
        var positionSeconds = position.TotalSeconds;
        var durationSeconds = duration.TotalSeconds;

        var clock = $"{FormatClock(position)} / {FormatClock(duration)}";
        ClockText.Text = clock;
        OverlayClock.Text = clock;

        if (!_seeking && !_suppressSeek)
        {
            _suppressSeek = true;
            try
            {
                var max = Math.Max(1, durationSeconds);
                SeekSlider.Maximum = max;
                SeekSlider.Value = Math.Clamp(positionSeconds, 0, max);
                OverlaySeek.Maximum = max;
                OverlaySeek.Value = Math.Clamp(positionSeconds, 0, max);
            }
            finally
            {
                _suppressSeek = false;
            }
        }

        UpdateOverlayLyrics();
    }

    static string FormatClock(TimeSpan value)
    {
        if (value.TotalHours >= 1)
        {
            return $"{(int)value.TotalHours}:{value.Minutes:00}:{value.Seconds:00}";
        }
        return $"{value.Minutes:00}:{value.Seconds:00}";
    }

    static string FormatDuration(long ms)
    {
        if (ms <= 0) return "–";
        var value = TimeSpan.FromMilliseconds(ms);
        return FormatClock(value);
    }

    void WireSeek(Slider slider)
    {
        slider.PointerPressed += (_, _) => _seeking = true;
        slider.PointerReleased += (_, _) =>
        {
            _seeking = false;
            _player?.SeekTo((long)(slider.Value * 1000));
        };
        slider.ValueChanged += (_, _) =>
        {
            if (_suppressSeek || _seeking) return;
            _player?.SeekTo((long)(slider.Value * 1000));
        };
    }

    void UpdatePlaybackModeUi()
    {
        ShuffleIcon.Foreground = _shuffle ? AccentSoft : IconIdle;
        OverlayShuffleIcon.Foreground = _shuffle ? AccentSoft : IconIdle;

        RepeatIcon.Foreground = _repeat != 0 ? AccentSoft : IconIdle;
        OverlayRepeatIcon.Foreground = _repeat != 0 ? AccentSoft : IconIdle;
        RepeatOneBadge.IsVisible = _repeat == 2;
        OverlayRepeatBadge.IsVisible = _repeat == 2;
    }

    // ---- 全屏播放 / 歌词 ----

    void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && QueuePanel.IsVisible)
        {
            HideQueueDrawer();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && NowPlayingOverlay.IsVisible)
        {
            HideOverlay();
            e.Handled = true;
            return;
        }

        // 输入框内不拦截空格与方向键，避免边输入边暂停/拖动进度
        if (e.Source is TextBox) return;

        if (e.Key == Key.Space)
        {
            TogglePlay();
            e.Handled = true;
        }
        else if (e.Key == Key.Left && _player is not null)
        {
            _player.SeekTo(Math.Max(0, _player.Position - 5000));
            e.Handled = true;
        }
        else if (e.Key == Key.Right && _player is not null)
        {
            _player.SeekTo(_player.Position + 5000);
            e.Handled = true;
        }
    }

    void OpenOverlay()
    {
        if (_currentTrack is null)
        {
            StatusText.Text = "先播放一首歌再打开全屏";
            return;
        }

        HideQueueDrawer();
        NowPlayingOverlay.IsVisible = true;
        OverlayTitle.Text = _currentTrack.Title;
        OverlaySub.Text = NowSub.Text;
        SetPlayIcon(_player?.IsPlaying == true);

        _ = RefreshOverlayAsync(_currentTrack);
    }

    void HideOverlay() => NowPlayingOverlay.IsVisible = false;

    async Task RefreshOverlayAsync(Track track)
    {
        var cover = await GetCoverAsync(track.CoverId, 600);
        if (cover is not null)
        {
            OverlayCover.Source = cover;
            OverlayBackdrop.Source = cover;
        }

        await LoadLyricsAsync(track.Guid);
    }

    async Task LoadLyricsAsync(string trackGuid)
    {
        _lyrics = new List<LyricLine>();
        _lyricIndex = -1;
        _lyricBlocks.Clear();
        LyricsPanel.Children.Clear();
        LyricsScroll.Offset = new Vector(0, 0);

        try
        {
            _lyrics = await Api().GetLyricsAsync(trackGuid);
        }
        catch
        {
            _lyrics = new List<LyricLine>();
        }

        if (_lyrics.Count == 0)
        {
            LyricsPanel.Children.Add(new TextBlock
            {
                Text = "暂无歌词",
                FontSize = 16,
                Foreground = TextSecondary
            });
            return;
        }

        foreach (var line in _lyrics)
        {
            var block = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(line.Text) ? " " : line.Text,
                FontSize = 18,
                Foreground = LyricIdle,
                TextWrapping = TextWrapping.Wrap
            };

            _lyricBlocks.Add(block);
            LyricsPanel.Children.Add(block);
        }

        UpdateOverlayLyrics(force: true);
    }

    void UpdateOverlayLyrics(bool force = false)
    {
        if (!NowPlayingOverlay.IsVisible || _lyrics.Count == 0 || _player is null) return;

        var position = TimeSpan.FromMilliseconds(Math.Max(0, _player.Position));
        var index = -1;

        for (var i = 0; i < _lyrics.Count; i++)
        {
            if (_lyrics[i].Time <= position) index = i;
            else break;
        }

        if (!force && index == _lyricIndex) return;

        _lyricIndex = index;
        var active = LyricActive;
        var idle = LyricIdle;

        for (var i = 0; i < _lyricBlocks.Count; i++)
        {
            var isActive = i == index;
            _lyricBlocks[i].Foreground = isActive ? active : idle;
            _lyricBlocks[i].FontWeight = isActive ? FontWeight.Bold : FontWeight.Normal;
            _lyricBlocks[i].FontSize = isActive ? 20 : 17;
        }

        if (index >= 0)
        {
            LyricsScroll.Offset = new Vector(0, Math.Max(0, index * 46 - 150));
        }
    }

    void SetPlayIcon(bool playing)
    {
        var symbol = playing ? Symbol.Pause : Symbol.Play;
        PlayIcon.Symbol = symbol;
        OverlayPlayIcon.Symbol = symbol;
        if (_roamPlayIcon is not null) _roamPlayIcon.Symbol = symbol;
    }

    // ---- 后台播放 / 托盘 ----

    TrayIcon CreateTrayIcon()
    {
        var tray = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://25DogMusic.Desktop/Assets/appicon.ico"))),
            ToolTipText = "25DogMusic",
            IsVisible = true
        };

        var menu = new NativeMenu();

        var show = new NativeMenuItem("显示窗口");
        show.Click += (_, _) => ShowFromTray();
        menu.Items.Add(show);

        var toggle = new NativeMenuItem("播放 / 暂停");
        toggle.Click += (_, _) => TogglePlay();
        menu.Items.Add(toggle);

        var prev = new NativeMenuItem("上一首");
        prev.Click += (_, _) => _ = AdvanceAsync(-1, allowWrap: true);
        menu.Items.Add(prev);

        var next = new NativeMenuItem("下一首");
        next.Click += (_, _) => _ = AdvanceAsync(1, allowWrap: true);
        menu.Items.Add(next);

        menu.Items.Add(new NativeMenuItemSeparator());

        var exit = new NativeMenuItem("退出 25DogMusic");
        exit.Click += (_, _) => ExitApplication();
        menu.Items.Add(exit);

        tray.Menu = menu;
        tray.Clicked += (_, _) => ShowFromTray();

        TrayIcon.SetIcons(Application.Current!, new TrayIcons { tray });
        return tray;
    }

    void ShowFromTray()
    {
        if (_hiddenToTray)
        {
            _hiddenToTray = false;
            Show();
        }

        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
        RestoreUiAfterBackground();
    }

    /// <summary>隐藏到托盘：音频继续在 VLC 线程播放，界面刷新与界面构建全部暂停。</summary>
    void HideToTray()
    {
        _hiddenToTray = true;
        SyncTimer();
        Hide();
        ClearCoverCaches();
    }

    void RestoreUiAfterBackground()
    {
        if (_currentTrack is null)
        {
            UpdatePlaybackUi();
            SyncTimer();
            return;
        }

        SetPlayIcon(_player?.IsPlaying == true);
        Title = $"{_currentTrack.Title} · {_currentTrack.Artist} — 25DogMusic";
        NowTitle.Text = _currentTrack.Title;
        NowSub.Text = BuildNowSub(_currentTrack);
        UpdateRowHighlight();
        _ = LoadNowCoverAsync(_currentTrack.CoverId);
        UpdatePlaybackUi();
        SyncTimer();
    }

    void ExitApplication()
    {
        _reallyExit = true;
        Close();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // 后台播放开启时，关闭窗口只是隐藏到托盘，播放不中断
        if (!_reallyExit && _config.BackgroundPlayback && _currentTrack is not null)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WindowStateProperty)
        {
            if (WindowState == WindowState.Minimized)
            {
                SyncTimer();
                ClearCoverCaches();
            }
            else
            {
                RestoreUiAfterBackground();
            }
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        UpdateThemeChrome();
    }

    protected override void OnClosed(EventArgs e)
    {
        _timer?.Stop();
        _pageLoadCts?.Cancel();
        _pageLoadCts?.Dispose();
        _config.Volume = (int)VolumeSlider.Value;
        _config.Save();
        if (_tray is not null) _tray.IsVisible = false;
        _player?.Dispose();
        _proxy?.Dispose();
        _api?.Dispose();
        base.OnClosed(e);
    }
}
