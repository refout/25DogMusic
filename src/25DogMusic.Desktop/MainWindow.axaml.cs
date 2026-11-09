using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
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

/// <summary>
/// 主窗口：负责导航、列表分页、播放控制、全屏播放页与设置页。
/// 界面按页面动态构建，切页时会清理上一页的控件引用以配合 GC。
/// </summary>
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

    /// <summary>Win11（22000+）才支持云母/亚克力等窗口材质。</summary>
    static bool IsWindows11 => OperatingSystem.IsWindows() && Environment.OSVersion.Version.Build >= 22000;

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
    readonly SpectrumAnalyzer _spectrum = new();
    DesktopLyricsWindow? _desktopLyrics;
    bool _desktopLyricsAutoClose;
    StreamProxy? _proxy;
    DispatcherTimer? _timer;
    CancellationTokenSource? _pageLoadCts;
    TrayIcon? _tray;
    bool _hiddenToTray;
    bool _reallyExit;

    List<Album> _albums = new();
    List<Track> _tracks = new();
    List<Track> _queue = new();
    Playlist? _currentPlaylist;
    int _queueIndex = -1;
    readonly List<Track> _roamTracks = new();
    readonly List<(Track Track, string RoamId)> _roamUpcoming = new();
    string? _roamPrefetchCursor;
    bool _roamPrefetching;
    Control? _roamQueueSection;
    ScrollViewer? _roamQueueScroll;
    Control? _homePage;
    TabControl? _settingsTabs;

    // 歌单「添加歌曲」侧边抽屉
    Playlist? _pickerPlaylist;
    ScrollViewer? _searchListScroll;
    Grid? _splitGrid;
    readonly HashSet<string> _pickerSelected = new();
    readonly List<Track> _pickerTracks = new();
    string _pickerKeyword = string.Empty;
    int _pickerPage;
    int _pickerTotal = -1;
    bool _pickerLoading;
    bool _pickerFinished;

    bool _roaming;
    string? _roamId;
    string _lastSearch = string.Empty;
    Image? _roamCover;
    TextBlock? _roamTitle;
    TextBlock? _roamSub;
    TextBlock? _roamMeta;
    SymbolIcon? _roamPlayIcon;

    enum Mode { Home, Albums, Tracks, Genres, History, Favorites, Artists, Folders, Search, Roam, Settings }

    /// <summary>歌手 / 风格左右分栏页左侧的列表项。</summary>
    sealed record BrowseItem(string Guid, string Name, int TrackCount, int AlbumCount, string CoverId);

    Mode _mode = Mode.Albums;

    Track? _currentTrack;
    string _lyricsGuid = string.Empty;
    List<LyricLine> _lyrics = new();
    readonly List<TextBlock> _lyricBlocks = new();
    int _lyricIndex = -1;

    Panel _pageHost = null!;

    // 通用分页（滚动到底部继续加载）：音乐库、收藏、最近、专辑/歌手/流派/歌单曲目共用
    readonly HashSet<string> _pagedSeen = new();
    int _pagedPage;
    int _pagedTotal = -1;
    bool _pagedLoading;
    bool _pagedActive;
    Func<int, Task<JsonDocument>>? _pagedFetch;
    string _pagedTitle = string.Empty;
    string _pagedSubtitle = string.Empty;
    Control? _pagedAction;
    TextBlock? _pagedFooter;
    bool _pagedEmpty;

    // 卡片网格（专辑/风格）与歌手行的分页状态
    Func<int, Task<JsonDocument>>? _gridFetch;
    Func<JsonDocument, List<(string?, Image)>, List<Control>>? _gridBuild;
    int _gridPage;
    int _gridTotal = -1;
    int _gridLoaded;
    bool _gridLoading;
    bool _gridActive;
    Panel? _gridPanel;
    TextBlock? _gridFooter;
    string _gridUnit = "项";
    int _gridVersion;

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

        ContentScroll.SizeChanged += (_, _) =>
        {
            UpdateSearchListHeight();
            UpdateSplitHeight();
            UpdateRoamQueueHeight();
            UpdateHomePageHeight();
            UpdateSettingsTabHeight();
        };

        NavHome.Click += async (_, _) => await LoadHomeAsync();
        NavFavorites.Click += async (_, _) => await LoadFavoritesAsync();
        NavRecent.Click += async (_, _) => await LoadHistoryAsync();
        NavAlbums.Click += async (_, _) => await LoadAlbumsAsync();
        NavArtists.Click += async (_, _) => await LoadArtistsAsync();
        NavGenres.Click += async (_, _) => await LoadGenresAsync();
        NavLibrary.Click += async (_, _) => await LoadAllTracksAsync();
        NavFolders.Click += (_, _) => ShowFoldersPage();
        NavSettings.Click += (_, _) => ShowSettingsPage();
        SidebarToggle.Click += (_, _) => SetSidebarCollapsed(!_sidebarCollapsed);
        SetSidebarCollapsed(_config.SidebarCollapsed, save: false);
        SizeChanged += (_, e) => UpdateSidebarForWidth(e.NewSize.Width);

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

        PickerPanelClose.Click += (_, _) => HidePickerDrawer();
        PickerPanelCancel.Click += (_, _) => HidePickerDrawer();
        PickerPanelAdd.Click += async (_, _) => await AddSelectedFromPickerAsync();
        PickerSearchBox.KeyDown += async (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            _pickerKeyword = (PickerSearchBox.Text ?? string.Empty).Trim();
            await ReloadPickerAsync();
        };
        PickerPanelScroll.ScrollChanged += (_, _) => OnPickerScrolled();

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
        ContentScroll.ScrollChanged += (_, _) => OnContentScrolled();

        // VLC 初始化推迟到窗口显示后（或首次播放时），避免拖慢启动
        _tray = CreateTrayIcon();

        _shuffle = _config.StartShuffle;
        _repeat = Math.Clamp(_config.StartRepeat, 0, 2);
        ApplyBackground();
        UpdatePlaybackModeUi();

        OverlaySpectrum.Analyzer = _spectrum;
        OverlaySpectrum.BarBrush = AccentSoft;

        DesktopLyricsButton.Click += (_, _) =>
        {
            _config.DesktopLyrics = !_config.DesktopLyrics;
            _config.Save();
            ApplyDesktopLyrics();
            UpdateDesktopLyricsButton();
            StatusText.Text = _config.DesktopLyrics
                ? _lyrics.Count == 0 ? "当前歌曲暂无歌词，有歌词时会自动显示" : "桌面歌词已开启"
                : "桌面歌词已关闭";
        };

        Opened += (_, _) =>
        {
            UpdateDesktopLyricsButton();
            if (_config.DesktopLyrics) ApplyDesktopLyrics();
            // 窗口句柄就绪后再设置一次系统标题栏，Win10 也能拿到深色标题栏
            WindowChrome.ApplyTitleBar(this, !IsLight, _windowBaseColor);
            // 空闲时预热播放器，避免首次点播放时才有几百毫秒延迟
            Dispatcher.UIThread.Post(() => EnsurePlayer(), DispatcherPriority.Background);
        };

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

    /// <summary>懒初始化 VLC：窗口显示后再创建，首次播放前也会自动创建。</summary>
    VlcPlayer EnsurePlayer()
    {
        if (_player is not null) return _player;

        var player = new VlcPlayer();
        player.Volume = MapVolume(_config.Volume);
        player.EndReached += (_, _) => Dispatcher.UIThread.Post(OnEndReached);
        player.ApplyEqualizer(_config.EqPresetIndex);
        _player = player;
        return player;
    }

    async Task InitialLoadAsync()
    {
        // 收藏状态、侧边歌单、首页数据互不依赖，并行加载以缩短首屏时间
        await Task.WhenAll(
            RefreshFavoriteGuidsAsync(),
            RefreshSidebarPlaylistsAsync(),
            LoadHomeAsync());
    }

    /// <summary>滑块 0-100 按平方曲线映射到播放器音量：听感更接近线性，两端不会突变。</summary>
    static int MapVolume(double sliderValue)
    {
        var fraction = Math.Clamp(sliderValue, 0, 100) / 100.0;
        return (int)Math.Round(Math.Pow(fraction, 2) * 100);
    }


    // ---- 页面搭建 ----

    void ResetPageContent()
    {
        // 切换页面时取消上一页仍在进行的封面加载，避免无用的网络与解码开销
        _pageLoadCts?.Cancel();
        _pageLoadCts?.Dispose();
        _pageLoadCts = new CancellationTokenSource();

        // 离开当前歌单时收起「添加歌曲」抽屉
        if (PickerPanel.IsVisible) HidePickerDrawer();
        _searchListScroll = null;
        _splitGrid = null;

        ContentPanel.Children.Clear();
        ListHeader.Children.Clear();
        ListRows.Children.Clear();
        // 离开首页/漫游页后恢复页面滚动（这些页面会临时禁用外层滚动）
        ContentScroll.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
        _homePage = null;
        _settingsTabs = null;
        _rowImages.Clear();
        _rowBorders.Clear();
        _heartIcons.Clear();
        _rowNumbers.Clear();

        TrimHeapAfterNavigation();
    }

    /// <summary>
    /// 整页控件在切换后会集中失效，但位图、样式的托管堆不会自行回落。
    /// 堆增长到一定规模后延迟做一次后台回收，避免长时间浏览内存持续上涨。
    /// </summary>
    static void TrimHeapAfterNavigation()
    {
        if (GC.GetTotalMemory(forceFullCollection: false) < 24 * 1024 * 1024) return;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            GC.Collect(2, GCCollectionMode.Forced, blocking: false, compacting: true);
        };
        timer.Start();
    }

    /// <summary>页面顶部只保留右上角操作按钮，标题与副标题已从界面移除。</summary>
    Control? BuildPageHeader(string title, string? subtitle, Control? action)
    {
        if (action is null) return null;

        var head = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(action, 1);
        head.Children.Add(action);
        return head;
    }

    Control BuildPageDivider() => new Border { Height = 1, Background = Divider, Margin = new Thickness(0, 4, 0, 0) };

    void BeginPage(string title, string? subtitle = null, Control? action = null)
    {
        ResetPageContent();
        _currentPlaylist = null;
        _pagedActive = false;
        _pagedFooter = null;
        _gridActive = false;
        _gridFooter = null;
        _gridVersion++;
        ContentScroll.IsVisible = true;
        ListPageHost.IsVisible = false;
        _pageHost = ContentPanel;

        if (BuildPageHeader(title, subtitle, action) is { } header)
        {
            ContentPanel.Children.Add(header);
            ContentPanel.Children.Add(BuildPageDivider());
        }

        ScrollToTop();
    }

    /// <summary>列表页：操作区域固定，只让曲目列表滚动。</summary>
    void BeginListPage(string title, string? subtitle = null, Control? action = null)
    {
        ResetPageContent();
        _pagedActive = false;
        _pagedFooter = null;
        _gridActive = false;
        _gridFooter = null;
        _gridVersion++;
        ContentScroll.IsVisible = false;
        ListPageHost.IsVisible = true;
        _pageHost = ListRows;

        if (BuildPageHeader(title, subtitle, action) is { } header)
        {
            ListHeader.Children.Add(header);
            ListHeader.Children.Add(BuildPageDivider());
        }

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

    /// <summary>搜索结果列表占据内容区剩余高度，页面本身不滚动。</summary>
    void UpdateSearchListHeight()
    {
        var scroll = _searchListScroll;
        if (scroll is null || scroll.Parent is null) return;
        var top = scroll.TranslatePoint(new Point(0, 0), ContentScroll)?.Y ?? 0;
        var height = ContentScroll.Bounds.Height - top - 12;
        if (height <= 0) return;
        scroll.Height = Math.Max(240, height);
    }

    /// <summary>歌手 / 风格左右分栏页占满内容区高度，左右两栏各自内部滚动。</summary>
    void UpdateSplitHeight()
    {
        var grid = _splitGrid;
        if (grid?.Parent is null) return;
        var top = grid.TranslatePoint(new Point(0, 0), ContentScroll)?.Y ?? 0;
        var height = ContentScroll.Bounds.Height - top - 12;
        if (height <= 0) return;
        grid.Height = Math.Max(260, height);
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

        // 歌单详情页里把「加入歌单」换成「移出歌单」
        Button action;
        if (_currentPlaylist is not null)
        {
            action = new Button
            {
                Content = MakeIcon(Symbol.SubtractCircle, 15, IconIdle),
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTip.SetTip(action, "从歌单移除");
            action.Classes.Add("tool");
            action.PointerPressed += (_, e) => e.Handled = true;
            action.Click += async (_, _) => await RemoveFromCurrentPlaylistAsync(track);
        }
        else
        {
            action = new Button
            {
                Content = MakeIcon(Symbol.AddCircle, 15, IconIdle),
                VerticalAlignment = VerticalAlignment.Center
            };
            ToolTip.SetTip(action, "加入歌单");
            action.Classes.Add("tool");
            action.PointerPressed += (_, e) => e.Handled = true;
            action.Click += async (_, _) => await AddToPlaylistAsync(track);
        }

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
        Grid.SetColumn(action, 5);
        grid.Children.Add(action);

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
        BeginPage("专辑", "按专辑浏览曲库");
        _albums = new List<Album>();

        var panel = new WrapPanel { Margin = new Thickness(-4, 6, 0, 0) };
        ContentPanel.Children.Add(panel);
        _gridPanel = panel;

        StartGridPaging(p => Api().GetAlbumsAsync(p, 200), (doc, pending) =>
        {
            var batch = Library.ParseAlbums(doc);
            _albums.AddRange(batch);
            var controls = new List<Control>();
            foreach (var album in batch)
            {
                controls.Add(BuildCard(album.Name, album.Artist, album.CoverId,
                    () => OpenAlbumAsync(album), pending));
            }
            return controls;
        }, "张专辑");

        await LoadGridPageAsync();

        if (_albums.Count == 0)
        {
            AddEmptyState("暂无专辑", "曲库还没有专辑信息", Symbol.Album);
        }
        else
        {
            var first = _albums.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.CoverId));
            if (first is not null) _ = SetBackdropAsync(first.CoverId);
        }

        StatusText.Text = $"专辑 {_albums.Count} 张";
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
            StartPagedTracks(p => Api().GetTracksByAlbumAsync(album.Guid, p, 200),
                album.Name, $"专辑 · {album.Artist} · ", Mode.Tracks);
            _pagedAction = BuildDetailActions(back, _tracks);
            await LoadPagedPageAsync();
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
        SetActiveNav(NavLibrary);
        StartPagedTracks(p => Api().GetTracksAsync(p, 200), "全部歌曲", string.Empty, Mode.Tracks);
        await LoadPagedPageAsync();
    }

    /// <summary>开始一个可无限滚动的曲目列表：先清空状态，首屏数据由 LoadPagedPageAsync 拉取。</summary>
    void StartPagedTracks(Func<int, Task<JsonDocument>> fetch, string title, string subtitlePrefix, Mode mode)
    {
        _mode = mode;
        _currentPlaylist = null;
        _pagedFetch = fetch;
        _pagedTitle = title;
        _pagedSubtitle = subtitlePrefix;
        _pagedAction = null;
        _pagedActive = true;
        _pagedPage = 0;
        _pagedTotal = -1;
        _pagedSeen.Clear();
        _pagedFooter = null;
        _pagedEmpty = false;
        _tracks = new List<Track>();
        BeginListPage(title, string.IsNullOrEmpty(subtitlePrefix) ? "正在加载…" : subtitlePrefix + "正在加载…");
        _pagedActive = true;
    }

    void OnListScrolled()
    {
        if (!_pagedActive || _pagedLoading) return;
        if (_pagedTotal >= 0 && _tracks.Count >= _pagedTotal) return;
        if (ListScroll.Extent.Height <= 0) return;

        if (ListScroll.Offset.Y + ListScroll.Viewport.Height >= ListScroll.Extent.Height - 160)
        {
            _ = LoadPagedPageAsync();
        }
    }

    async Task LoadPagedPageAsync()
    {
        if (!_pagedActive || _pagedLoading || _pagedFetch is null) return;
        if (_pagedTotal >= 0 && _tracks.Count >= _pagedTotal) return;

        var token = _pageLoadCts?.Token;
        _pagedLoading = true;
        RefreshPagedFooter();

        try
        {
            _pagedPage++;
            using var doc = await _pagedFetch(_pagedPage);
            if (token is { IsCancellationRequested: true }) return;

            var batch = Library.ParseTracks(doc);
            if (batch.Count == 0)
            {
                if (_pagedTotal < 0) _pagedTotal = _tracks.Count;
                if (_tracks.Count == 0)
                {
                    // 第一页就是空列表：仍要构建带操作按钮的页头，否则返回/添加歌曲会缺失
                    _pagedEmpty = true;
                    BeginListPage(_pagedTitle, $"{_pagedSubtitle}共 0 首",
                        _pagedAction ?? CreatePlayAllAction(_tracks));
                    _pagedActive = true;
                }
                return;
            }

            if (_pagedTotal < 0) _pagedTotal = PageTotal(doc);

            var added = new List<Track>();
            var startIndex = _tracks.Count;
            foreach (var track in batch)
            {
                if (!_pagedSeen.Add(track.Guid)) continue;
                _tracks.Add(track);
                added.Add(track);
            }

            if (startIndex == 0)
            {
                // 第一页拿到后确定标题与总数，再逐页追加
                var subtitle = _pagedTotal >= 0
                    ? $"{_pagedSubtitle}共 {_pagedTotal} 首"
                    : $"{_pagedSubtitle}已加载 {_tracks.Count} 首";
                BeginListPage(_pagedTitle, subtitle, _pagedAction ?? CreatePlayAllAction(_tracks));
                _pagedActive = true;
                AddTrackRows(_tracks, _tracks);
            }
            else if (added.Count > 0)
            {
                AddTrackRows(added, _tracks, startIndex);
            }

            // 收藏列表：把已加载页的收藏同步到本地集合，保证心形状态正确
            if (_mode == Mode.Favorites)
            {
                foreach (var track in added) _favorites.Add(track.Guid);
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载歌曲失败");
        }
        finally
        {
            _pagedLoading = false;
            RefreshPagedFooter();
        }
    }

    void RefreshPagedFooter()
    {
        if (!_pagedActive) return;

        if (_pagedEmpty)
        {
            if (_pagedFooter?.Parent is Panel emptyParent) emptyParent.Children.Remove(_pagedFooter);
            return;
        }

        _pagedFooter ??= new TextBlock
        {
            FontSize = 11.5,
            Foreground = TextMuted,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 14, 0, 6)
        };

        if (_pagedFooter.Parent is Panel parent) parent.Children.Remove(_pagedFooter);

        _pagedFooter.Text = _pagedLoading
            ? "正在加载…"
            : _pagedTotal >= 0 && _tracks.Count >= _pagedTotal
                ? $"已加载全部 {_tracks.Count} 首"
                : "滚动到底部加载更多";

        _pageHost.Children.Add(_pagedFooter);
    }

}
