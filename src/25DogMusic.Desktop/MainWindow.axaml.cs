using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    Control? _roamQueueSection;

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

    // ---- 登录 / 客户端 ----

    int _settingsTab;

    static TextBlock SettingsTabHeader(string text) => new()
    {
        Text = text,
        FontSize = 15.5,
        FontWeight = FontWeight.Medium,
        VerticalAlignment = VerticalAlignment.Center
    };

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

        tabs.Items.Add(new TabItem { Header = SettingsTabHeader("账号与连接"), Content = BuildAccountCard(message) });
        tabs.Items.Add(new TabItem { Header = SettingsTabHeader("外观"), Content = BuildAppearanceCard() });
        tabs.Items.Add(new TabItem { Header = SettingsTabHeader("音质与播放"), Content = BuildPlaybackCard() });

        tabs.SelectedIndex = Math.Clamp(_settingsTab, 0, tabs.ItemCount - 1);
        tabs.SelectionChanged += (_, _) =>
        {
            if (tabs.SelectedIndex >= 0) _settingsTab = tabs.SelectedIndex;
            AnimateSettingsTab(tabs.SelectedItem as TabItem);
        };

        ContentPanel.Children.Add(tabs);

        // 首次进入也播放一次淡入过渡
        AnimateSettingsTab(tabs.SelectedItem as TabItem);
    }

    /// <summary>设置页切换 Tab 时内容淡入并轻微上移。</summary>
    void AnimateSettingsTab(TabItem? item)
    {
        if (item?.Content is not Control content) return;

        content.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = OpacityProperty,
                Duration = TimeSpan.FromMilliseconds(220),
                Easing = new CubicEaseOut()
            },
            new TransformOperationsTransition
            {
                Property = RenderTransformProperty,
                Duration = TimeSpan.FromMilliseconds(220),
                Easing = new CubicEaseOut()
            }
        };

        content.Opacity = 0;
        content.RenderTransform = new TranslateTransform(0, 10);
        Dispatcher.UIThread.Post(() =>
        {
            content.Opacity = 1;
            content.RenderTransform = new TranslateTransform(0, 0);
        }, DispatcherPriority.Background);
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
        HidePickerDrawer();
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

    // ---- 歌单「添加歌曲」侧边抽屉 ----

    async Task ShowPickerDrawerAsync(Playlist playlist)
    {
        HideQueueDrawer();
        _pickerPlaylist = playlist;
        PickerPanelName.Text = $"「{playlist.Name}」";
        PickerSearchBox.Text = string.Empty;
        _pickerKeyword = string.Empty;
        _pickerSelected.Clear();
        UpdatePickerSummary();
        PickerPanel.IsVisible = true;
        await ReloadPickerAsync();
    }

    void HidePickerDrawer()
    {
        PickerPanel.IsVisible = false;
        _pickerPlaylist = null;
    }

    async Task ReloadPickerAsync()
    {
        _pickerTracks.Clear();
        _pickerPage = 0;
        _pickerTotal = -1;
        _pickerFinished = false;
        _pickerLoading = false;
        PickerPanelRows.Children.Clear();
        PickerPanelScroll.Offset = new Vector(0, 0);
        await LoadPickerPageAsync();
    }

    void OnPickerScrolled()
    {
        if (_pickerLoading || _pickerFinished) return;
        if (PickerPanelScroll.Extent.Height <= 0) return;
        if (PickerPanelScroll.Offset.Y + PickerPanelScroll.Viewport.Height >= PickerPanelScroll.Extent.Height - 160)
        {
            _ = LoadPickerPageAsync();
        }
    }

    async Task LoadPickerPageAsync()
    {
        if (_pickerLoading || _pickerFinished || _pickerPlaylist is null) return;
        _pickerLoading = true;

        try
        {
            _pickerPage++;
            using var doc = _pickerKeyword.Length > 0
                ? await Api().SearchTracksAsync(_pickerKeyword, _pickerPage, 100)
                : await Api().GetTracksAsync(_pickerPage, 100);

            if (_pickerTotal < 0) _pickerTotal = PageTotal(doc);
            var batch = Library.ParseTracks(doc);

            if (batch.Count == 0)
            {
                _pickerFinished = true;
                if (_pickerTracks.Count == 0)
                {
                    PickerPanelRows.Children.Add(new TextBlock
                    {
                        Text = "没有找到歌曲",
                        FontSize = 12.5,
                        Foreground = TextMuted,
                        Margin = new Thickness(4, 16, 0, 0)
                    });
                }
                return;
            }

            foreach (var track in batch)
            {
                _pickerTracks.Add(track);
                PickerPanelRows.Children.Add(BuildPickerRow(track));
            }

            if (_pickerTotal >= 0 && _pickerTracks.Count >= _pickerTotal) _pickerFinished = true;
        }
        catch (Exception ex)
        {
            _pickerFinished = true;
            HandleError(ex, "加载歌曲失败");
        }
        finally
        {
            _pickerLoading = false;
        }
    }

    Control BuildPickerRow(Track track)
    {
        var box = new CheckBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            IsChecked = _pickerSelected.Contains(track.Guid)
        };
        box.IsCheckedChanged += (_, _) =>
        {
            if (box.IsChecked == true) _pickerSelected.Add(track.Guid);
            else _pickerSelected.Remove(track.Guid);
            UpdatePickerSummary();
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
            Text = string.IsNullOrWhiteSpace(track.Artist) ? track.AlbumName : $"{track.Artist} · {track.AlbumName}",
            FontSize = 10.5,
            Foreground = TextSecondary,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var texts = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        texts.Children.Add(title);
        texts.Children.Add(sub);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        grid.Children.Add(box);
        Grid.SetColumn(texts, 1);
        grid.Children.Add(texts);

        var row = new Border
        {
            Padding = new Thickness(8, 7),
            CornerRadius = new CornerRadius(10),
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = grid
        };
        row.PointerEntered += (_, _) => row.Background = RowHover;
        row.PointerExited += (_, _) => row.Background = Brushes.Transparent;
        row.PointerPressed += (_, _) => box.IsChecked = !(box.IsChecked == true);
        return row;
    }

    void UpdatePickerSummary()
    {
        PickerPanelSummary.Text = $"已选 {_pickerSelected.Count} 首";
        PickerPanelAdd.IsEnabled = _pickerSelected.Count > 0;
    }

    async Task AddSelectedFromPickerAsync()
    {
        var playlist = _pickerPlaylist;
        if (playlist is null || _pickerSelected.Count == 0) return;

        PickerPanelAdd.IsEnabled = false;
        PickerPanelAdd.Content = "正在添加…";

        var added = 0;
        foreach (var guid in _pickerSelected.ToList())
        {
            try
            {
                await Api().AddTrackToPlaylistAsync(playlist.Guid, guid);
                added++;
            }
            catch
            {
                // 单首失败不影响其余歌曲
            }
        }

        PickerPanelAdd.Content = "添加";
        UpdatePickerSummary();

        if (added > 0)
        {
            StatusText.Text = $"已添加 {added} 首到歌单「{playlist.Name}」";
            HidePickerDrawer();
            await OpenPlaylistAsync(playlist);
        }
        else
        {
            StatusText.Text = "添加失败，请稍后再试";
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

        // 已登录：只展示账号信息，不显示登录表单
        if (loggedIn)
        {
            TextBlock InfoValue(string text) => new()
            {
                Text = text,
                FontSize = 12.5,
                Foreground = TextSecondary,
                VerticalAlignment = VerticalAlignment.Center
            };

            var infoBody = new StackPanel { Spacing = 14 };
            infoBody.Children.Add(header);
            infoBody.Children.Add(BuildSettingRow("服务器地址", null, InfoValue(_config.BaseUrl)));
            infoBody.Children.Add(BuildSettingRow("当前账号", null,
                InfoValue(string.IsNullOrWhiteSpace(_config.LastUsername) ? "飞牛音乐用户" : _config.LastUsername)));
            infoBody.Children.Add(new TextBlock
            {
                Text = "需要切换账号？先点击右上角「退出登录」，登录表单会重新出现。",
                FontSize = 11.5,
                Foreground = TextMuted,
                TextWrapping = TextWrapping.Wrap
            });
            return MakeSettingsCard(infoBody);
        }

        var serverBox = MakeFormBox(_config.BaseUrl, "https://nas.example.com");
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

        var codeRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,110") };
        codeRow.Children.Add(codeBox);
        Grid.SetColumn(submitButton, 2);
        codeRow.Children.Add(submitButton);

        TextBlock SectionHeading(string text) => new()
        {
            Text = text,
            FontSize = 12.5,
            FontWeight = FontWeight.SemiBold,
            Foreground = CardLabel,
            Margin = new Thickness(0, 0, 0, 2)
        };

        var passwordPane = new StackPanel { Spacing = 10 };
        passwordPane.Children.Add(SectionHeading("账号密码登录"));
        passwordPane.Children.Add(BuildField("服务器地址", serverBox));
        passwordPane.Children.Add(credentials);
        passwordPane.Children.Add(loginButton);

        var oauthPane = new StackPanel { Spacing = 10 };
        oauthPane.Children.Add(SectionHeading("浏览器授权"));
        oauthPane.Children.Add(new TextBlock
        {
            Text = "打开飞牛 NAS 授权页，登录后把回跳地址或 code 粘贴到下方。",
            FontSize = 11.5,
            Foreground = TextMuted,
            TextWrapping = TextWrapping.Wrap
        });
        oauthPane.Children.Add(oauthButton);
        oauthPane.Children.Add(codeRow);

        var divider = new Border
        {
            Width = 1,
            Background = Divider,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 2)
        };

        var columns = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,1,*"),
            ColumnSpacing = 22
        };
        columns.Children.Add(passwordPane);
        Grid.SetColumn(divider, 1);
        columns.Children.Add(divider);
        Grid.SetColumn(oauthPane, 2);
        columns.Children.Add(oauthPane);

        var body = new StackPanel { Spacing = 16 };
        body.Children.Add(header);
        body.Children.Add(columns);

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
            var coverImage = new Image { Stretch = Stretch.UniformToFill };

            var fillContent = new Grid();
            fillContent.Children.Add(new Border { Background = option.Brush });
            if (option.Mode == "cover") fillContent.Children.Add(coverImage);

            var fill = new Border
            {
                Width = 58,
                Height = 58,
                CornerRadius = new CornerRadius(12),
                ClipToBounds = true,
                Background = Brushes.Transparent,
                BorderBrush = SwatchBorder,
                BorderThickness = new Thickness(1),
                IsHitTestVisible = false,
                Child = fillContent
            };

            var swatch = new Button
            {
                Content = fill,
                Width = 58,
                Height = 58,
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

            if (option.Mode == "cover") _ = LoadCoverSwatchAsync(coverImage);
        }

        // 末尾的颜色选择方框：点击打开调色板
        var customColor = Color.TryParse(_config.BackgroundCustomColor, out var parsed)
            ? parsed
            : Color.Parse("#2B1B33");
        var customFill = new Border
        {
            Width = 58,
            Height = 58,
            CornerRadius = new CornerRadius(12),
            ClipToBounds = true,
            Background = new SolidColorBrush(customColor),
            BorderBrush = SwatchBorder,
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false
        };

        var customButton = new Button
        {
            Content = customFill,
            Width = 58,
            Height = 58,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        customButton.Classes.Add("swatch");

        var palette = new WrapPanel { Width = 210, Margin = new Thickness(2) };
        foreach (var hex in new[]
                 {
                     "#FF375F", "#FF7A45", "#F5A524", "#34C759", "#0FA3A3", "#2E7CF6",
                     "#7A5CFF", "#C04AB8", "#8A5A44", "#6B7280", "#2B1B33", "#0B0B0E"
                 })
        {
            var color = Color.Parse(hex);
            var chip = new Border
            {
                Width = 30,
                Height = 30,
                Margin = new Thickness(3),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(color),
                BorderBrush = SwatchBorder,
                BorderThickness = new Thickness(1),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            chip.PointerPressed += (_, _) =>
            {
                _config.BackgroundCustomColor = hex;
                _config.BackgroundMode = "custom";
                _config.Save();
                customFill.Background = new SolidColorBrush(color);
                ApplyBackground();
                UpdateSelection();
                StatusText.Text = $"背景：自定义 {hex}";
                customButton.Flyout?.Hide();
            };
            palette.Children.Add(chip);
        }

        customButton.Flyout = new Flyout { Content = palette };

        var customItem = new StackPanel { Spacing = 0, Margin = new Thickness(0, 0, 16, 8) };
        customItem.Children.Add(customButton);
        customItem.Children.Add(new TextBlock
        {
            Text = "自定义",
            FontSize = 11.5,
            Foreground = TextSecondary,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0)
        });
        wrap.Children.Add(customItem);
        swatches["custom"] = customFill;

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
        body.Children.Add(BuildSettingRow("窗口材质",
            IsWindows11 ? "Windows 11 的云母 / 亚克力 / 液态玻璃效果" : "Windows 10 不支持窗口材质，将使用纯色背景",
            materialBox));

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
            var active = materialBox.IsEnabled && materialBox.SelectedIndex > 0;
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

        if (!IsWindows11) materialBox.IsEnabled = false;

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

        var spectrumSwitch = new ToggleSwitch
        {
            IsChecked = _config.SpectrumEnabled,
            OnContent = string.Empty,
            OffContent = string.Empty,
            VerticalAlignment = VerticalAlignment.Center
        };
        spectrumSwitch.IsCheckedChanged += (_, _) =>
        {
            _config.SpectrumEnabled = spectrumSwitch.IsChecked == true;
            _config.Save();
            SyncSpectrum(_player?.IsPlaying == true && !_paused);
        };
        body.Children.Add(BuildSettingRow("频谱可视化", "全屏播放页显示跟随音频的实时频谱", spectrumSwitch));

        var lyricsSwitch = new ToggleSwitch
        {
            IsChecked = _config.DesktopLyrics,
            OnContent = string.Empty,
            OffContent = string.Empty,
            VerticalAlignment = VerticalAlignment.Center
        };
        lyricsSwitch.IsCheckedChanged += (_, _) =>
        {
            _config.DesktopLyrics = lyricsSwitch.IsChecked == true;
            _config.Save();
            ApplyDesktopLyrics();
        };
        body.Children.Add(BuildSettingRow("桌面歌词", "在屏幕底部显示可拖动的浮动歌词", lyricsSwitch));

        var presetNames = new List<string> { "关闭" };
        presetNames.AddRange(_player?.EqualizerPresets ?? Array.Empty<string>());
        var equalizerBox = new ComboBox
        {
            ItemsSource = presetNames,
            SelectedIndex = Math.Clamp(_config.EqPresetIndex + 1, 0, presetNames.Count - 1),
            Width = 170,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        equalizerBox.SelectionChanged += (_, _) =>
        {
            _config.EqPresetIndex = equalizerBox.SelectedIndex - 1;
            _player?.ApplyEqualizer(_config.EqPresetIndex);
            _config.Save();
        };
        body.Children.Add(BuildSettingRow("均衡器", "使用播放器内置的音效预设", equalizerBox));

        return MakeSettingsCard(body);
    }

    void ApplyBackground(bool announceMaterial = false)
    {
        var mode = string.IsNullOrWhiteSpace(_config.BackgroundMode) ? "cover" : _config.BackgroundMode;
        var material = string.IsNullOrWhiteSpace(_config.Material) ? "off" : _config.Material;
        // Windows 10 不支持 Mica / Acrylic 背景，强制退回纯色，避免透出黑块
        var useMaterial = material != "off" && IsWindows11;
        var opacity = Math.Clamp(_config.UiOpacity <= 0 ? 0.75 : _config.UiOpacity, 0.3, 1.0);

        var customColor = Color.TryParse(_config.BackgroundCustomColor, out var parsedCustom)
            ? parsedCustom
            : Color.Parse(IsLight ? "#F5F5F9" : "#0B0B0E");

        var page = mode == "custom"
            ? customColor
            : Color.Parse(IsLight
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
        // 播放列表与加歌抽屉：和播放栏旁的胶囊按钮保持同一种半透明白玻璃质感，
        // 不再使用由侧边栏派生的灰蓝色面板
        var panelBrush = IsLight
            ? new SolidColorBrush(Color.Parse("#F2F2F7"), 0.97)
            : new SolidColorBrush(Colors.White, 0.10);
        QueuePanel.Background = panelBrush;
        PickerPanel.Background = panelBrush;

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

        // 离开当前歌单时收起「添加歌曲」抽屉
        if (PickerPanel.IsVisible) HidePickerDrawer();
        _searchListScroll = null;
        _splitGrid = null;

        ContentPanel.Children.Clear();
        ListHeader.Children.Clear();
        ListRows.Children.Clear();
        _rowImages.Clear();
        _rowBorders.Clear();
        _heartIcons.Clear();
        _rowNumbers.Clear();
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

    // ---- 卡片网格分页（专辑 / 风格 / 歌手）----

    /// <summary>开始网格分页：调用前需先把承载卡片的 Panel 加入页面并赋给 _gridPanel。</summary>
    void StartGridPaging(Func<int, Task<JsonDocument>> fetch,
        Func<JsonDocument, List<(string?, Image)>, List<Control>> build, string unit)
    {
        _gridFetch = fetch;
        _gridBuild = build;
        _gridUnit = unit;
        _gridPage = 0;
        _gridTotal = -1;
        _gridLoaded = 0;
        _gridLoading = false;
        _gridActive = true;
        if (_gridFooter?.Parent is Panel oldFooterParent) oldFooterParent.Children.Remove(_gridFooter);
        _gridFooter = null;
        _gridVersion++;
    }

    void OnContentScrolled()
    {
        if (!_gridActive || _gridLoading) return;
        if (_gridTotal >= 0 && _gridLoaded >= _gridTotal) return;
        if (ContentScroll.Extent.Height <= 0) return;

        if (ContentScroll.Offset.Y + ContentScroll.Viewport.Height >= ContentScroll.Extent.Height - 220)
        {
            _ = LoadGridPageAsync();
        }
    }

    async Task LoadGridPageAsync()
    {
        if (!_gridActive || _gridLoading || _gridFetch is null || _gridBuild is null || _gridPanel is null) return;
        if (_gridTotal >= 0 && _gridLoaded >= _gridTotal) return;

        var token = _pageLoadCts?.Token;
        var version = _gridVersion;
        var fetch = _gridFetch;
        var build = _gridBuild;
        var panel = _gridPanel;
        _gridLoading = true;
        RefreshGridFooter();

        try
        {
            _gridPage++;
            using var doc = await fetch(_gridPage);
            if (version != _gridVersion) return;
            if (token is { IsCancellationRequested: true }) return;

            if (_gridTotal < 0) _gridTotal = PageTotal(doc);

            var pending = new List<(string?, Image)>();
            var controls = build(doc, pending);
            if (controls.Count == 0)
            {
                // 没有更多数据：停止继续请求
                _gridTotal = _gridLoaded;
                return;
            }

            foreach (var control in controls) panel.Children.Add(control);
            _gridLoaded += controls.Count;

            if (pending.Count > 0) _ = LoadCardCoversAsync(pending);
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载更多失败");
        }
        finally
        {
            if (version == _gridVersion)
            {
                _gridLoading = false;
                RefreshGridFooter();
            }
        }
    }

    void RefreshGridFooter()
    {
        if (!_gridActive || _gridPanel is null) return;

        if (_gridLoaded == 0 && !_gridLoading)
        {
            if (_gridFooter?.Parent is Panel emptyParent) emptyParent.Children.Remove(_gridFooter);
            return;
        }

        _gridFooter ??= new TextBlock
        {
            FontSize = 11.5,
            Foreground = TextMuted,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 6)
        };

        if (_gridFooter.Parent is Panel parent) parent.Children.Remove(_gridFooter);

        _gridFooter.Text = _gridLoading
            ? "正在加载…"
            : _gridTotal >= 0 && _gridLoaded >= _gridTotal
                ? $"已加载全部 {_gridLoaded} {_gridUnit}"
                : $"已加载 {_gridLoaded} {_gridUnit}，滚动到底部加载更多";

        _pageHost.Children.Add(_gridFooter);
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
        SetActiveNav(NavRecent);
        StartPagedTracks(p => Api().GetPlayHistoryAsync(p, 200), "最近播放", string.Empty, Mode.History);
        await LoadPagedPageAsync();
        if (_tracks.Count == 0) AddEmptyState("暂无播放记录", "在飞牛音乐中播放过的歌曲会出现在这里", Symbol.Clock);
        StatusText.Text = $"最近 {_tracks.Count} 首";
    }

    async Task LoadGenresAsync()
    {
        await ShowBrowseSplitAsync(
            Mode.Genres, NavGenres,
            p => Api().GetGenresAsync(p, 200),
            doc => Library.ParseGenres(doc)
                .Select(g => new BrowseItem(g.Guid, g.Name, g.TrackCount, 0, string.Empty))
                .ToList(),
            (guid, p) => Api().GetTracksByGenreAsync(guid, p, 200),
            null,
            "个风格", "暂无流派", "这个风格下还没有曲目", "这个风格下还没有专辑");
    }

    async Task OpenArtistAsync(Artist artist)
    {
        Func<Task> back = LoadAlbumsAsync;
        if (_mode == Mode.Search && _lastSearch.Length > 0) back = () => SearchAsync(_lastSearch);

        try
        {
            StartPagedTracks(p => Api().GetTracksByArtistAsync(artist.Guid, p, 200),
                artist.Name, "歌手 · ", Mode.Tracks);
            _pagedAction = BuildDetailActions(back, _tracks);
            await LoadPagedPageAsync();
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
            _roamQueueSection = null;
            if (!_roamTracks.Any(track => track.Guid == _currentTrack.Guid)) _roamTracks.Add(_currentTrack);
            BuildRoamHero(_currentTrack);
            BuildRoamQueueSection();
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
            _roamTracks.Clear();
            _roamTracks.Add(result.Value.Track);
            _roamQueueSection = null;

            BuildRoamHero(result.Value.Track);
            BuildRoamQueueSection();
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

    /// <summary>漫游页下方展示本次漫游已走过的曲目队列，点卡片可直接切换播放。</summary>
    void BuildRoamQueueSection()
    {
        if (_roamQueueSection is not null && _roamQueueSection.Parent is Panel oldParent)
        {
            oldParent.Children.Remove(_roamQueueSection);
        }
        _roamQueueSection = null;

        if (_roamTracks.Count == 0) return;

        var section = new StackPanel { Spacing = 8, Margin = new Thickness(0, 28, 0, 0) };

        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        header.Children.Add(new TextBlock
        {
            Text = "漫游队列",
            FontSize = 15,
            FontWeight = FontWeight.SemiBold,
            Foreground = TextPrimary
        });
        header.Children.Add(new TextBlock
        {
            Text = $"{_roamTracks.Count} 首 · 点击切换",
            FontSize = 11,
            Foreground = TextMuted,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 2)
        });
        section.Children.Add(header);

        var strip = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12, Margin = new Thickness(0, 2, 0, 0) };
        for (var i = 0; i < _roamTracks.Count; i++)
        {
            strip.Children.Add(BuildRoamQueueCard(_roamTracks[i], i));
        }

        section.Children.Add(new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = strip
        });

        ContentPanel.Children.Add(section);
        _roamQueueSection = section;
    }

    Control BuildRoamQueueCard(Track track, int index)
    {
        var isCurrent = _currentTrack is not null && _currentTrack.Guid == track.Guid;

        var image = new Image { Stretch = Stretch.UniformToFill };
        var coverGrid = new Grid();
        coverGrid.Children.Add(MakeIcon(Symbol.MusicNote2, 24, EmptyIcon));
        coverGrid.Children.Add(image);

        var cover = new Border
        {
            Width = 148,
            Height = 148,
            CornerRadius = new CornerRadius(12),
            ClipToBounds = true,
            Background = CoverPlaceholder,
            BorderThickness = new Thickness(isCurrent ? 2 : 0),
            BorderBrush = isCurrent ? AccentSoft : Brushes.Transparent,
            Child = coverGrid
        };

        var texts = new StackPanel { Spacing = 1, Margin = new Thickness(0, 7, 0, 0) };
        texts.Children.Add(new TextBlock
        {
            Text = track.Title,
            FontSize = 12,
            Foreground = TextPrimary,
            MaxWidth = 148,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        texts.Children.Add(new TextBlock
        {
            Text = isCurrent ? "正在播放" : track.Artist,
            FontSize = 10.5,
            Foreground = isCurrent ? AccentSoft : TextMuted,
            MaxWidth = 148,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        var stack = new StackPanel();
        stack.Children.Add(cover);
        stack.Children.Add(texts);

        var card = new Border
        {
            Width = 156,
            Padding = new Thickness(4),
            CornerRadius = new CornerRadius(12),
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = stack
        };
        card.PointerEntered += (_, _) => card.Background = RowHover;
        card.PointerExited += (_, _) => card.Background = Brushes.Transparent;
        card.PointerPressed += (_, _) =>
        {
            PlayFromQueue(_roamTracks, index);
            _queue = new List<Track>(_roamTracks);
            _queueIndex = index;
            SyncRoamHero(_roamTracks[index]);
            BuildRoamQueueSection();
        };

        _ = AssignRoamQueueCoverAsync(image, track.CoverId);
        return card;
    }

    async Task AssignRoamQueueCoverAsync(Image image, string? coverId)
    {
        var bitmap = await GetCoverAsync(coverId, 160);
        if (bitmap is not null) image.Source = bitmap;
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

            if (!_roamTracks.Any(track => track.Guid == result.Value.Track.Guid))
            {
                _roamTracks.Add(result.Value.Track);
            }

            SyncRoamHero(result.Value.Track);
            BuildRoamQueueSection();
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
        SetActiveNav(NavFavorites);
        _favorites.Clear();
        StartPagedTracks(p => Api().GetFavoritesAsync(p, 200), "我的收藏", string.Empty, Mode.Favorites);
        await LoadPagedPageAsync();
        if (_tracks.Count == 0) AddEmptyState("还没有收藏", "点击歌曲右侧的心形图标即可收藏", Symbol.Heart);
        StatusText.Text = $"收藏 {_tracks.Count} 首";
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
            var menu = new ContextMenu();
            var renameItem = new MenuItem { Header = "重命名歌单" };
            renameItem.Click += async (_, _) => await RenamePlaylistAsync(playlist);
            var deleteItem = new MenuItem { Header = "删除歌单" };
            deleteItem.Click += async (_, _) => await DeletePlaylistAsync(playlist);
            menu.Items.Add(renameItem);
            menu.Items.Add(deleteItem);
            button.ContextMenu = menu;

            button.Classes.Add("nav");
            ToolTip.SetTip(button, "点击打开 · 右键可重命名/删除");
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

    async Task RenamePlaylistAsync(Playlist playlist)
    {
        var name = await Dialogs.PromptAsync(this, "重命名歌单", playlist.Name, "歌单名称");
        if (string.IsNullOrWhiteSpace(name)) return;
        name = name.Trim();
        if (name == playlist.Name) return;

        try
        {
            await Api().EditPlaylistAsync(playlist.Guid, name);
            StatusText.Text = $"歌单已重命名为「{name}」";
            await RefreshSidebarPlaylistsAsync();
            if (_currentPlaylist?.Guid == playlist.Guid)
            {
                await OpenPlaylistAsync(new Playlist(playlist.Guid, name, playlist.TrackCount, playlist.CoverId));
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "重命名歌单失败");
        }
    }

    async Task DeletePlaylistAsync(Playlist playlist)
    {
        var ok = await Dialogs.ConfirmAsync(this, "删除歌单",
            $"确定删除歌单「{playlist.Name}」吗？歌单内的歌曲不会从曲库中删除。", "删除");
        if (!ok) return;

        try
        {
            await Api().DeletePlaylistAsync(playlist.Guid);
            StatusText.Text = $"已删除歌单「{playlist.Name}」";
            await RefreshSidebarPlaylistsAsync();
            if (_currentPlaylist?.Guid == playlist.Guid)
            {
                _currentPlaylist = null;
                await LoadHomeAsync();
            }
        }
        catch (Exception ex)
        {
            HandleError(ex, "删除歌单失败");
        }
    }

    // ---- 首页 / 歌手 / 文件夹 ----

    async Task LoadHomeAsync()
    {
        _mode = Mode.Home;
        SetActiveNav(NavHome);
        BeginPage("首页", string.Empty);

        // 最近播放 / 收藏 / 最近添加 / 专辑互不依赖，并行请求以缩短首屏时间
        async Task<List<Track>> FetchTracksAsync(Func<Task<JsonDocument>> fetch, int take, string what)
        {
            try
            {
                using var doc = await fetch();
                return Library.ParseTracks(doc).Take(take).ToList();
            }
            catch (Exception ex)
            {
                HandleError(ex, what);
                return new List<Track>();
            }
        }

        async Task<List<Album>> FetchAlbumListAsync(Func<Task<JsonDocument>> fetch, int take, string what)
        {
            try
            {
                using var doc = await fetch();
                return Library.ParseAlbums(doc).Take(take).ToList();
            }
            catch (Exception ex)
            {
                HandleError(ex, what);
                return new List<Album>();
            }
        }

        var recentTask = FetchTracksAsync(() => Api().GetPlayHistoryAsync(1, 20), 12, "加载最近播放失败");
        var favoritesTask = FetchTracksAsync(() => Api().GetFavoritesAsync(1, 12), 12, "加载收藏失败");
        var latestTask = FetchTracksAsync(() => Api().GetTracksAsync(1, 12), 12, "加载最近添加歌曲失败");
        var albumsTask = FetchAlbumListAsync(() => Api().GetAlbumsAsync(1, 8), 8, "加载专辑失败");

        await Task.WhenAll(recentTask, favoritesTask, latestTask, albumsTask);
        var recent = recentTask.Result;
        var favorites = favoritesTask.Result;
        var latest = latestTask.Result;
        var albums = albumsTask.Result;

        // 顶部四个入口卡片：封面拼贴 + 标题，正方形并随宽度自适应
        var shortcutCovers = new List<(string?, Image)>();
        var cards = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*,*"), ColumnSpacing = 12 };

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

        void FitSquareCards()
        {
            var width = (cards.Bounds.Width - 36) / 4;
            if (width <= 0) return;
            foreach (var child in cards.Children)
            {
                if (child is Border border) border.Height = width;
            }
        }
        cards.SizeChanged += (_, _) => FitSquareCards();
        Dispatcher.UIThread.Post(FitSquareCards, DispatcherPriority.Background);

        _ = LoadHomeCardCoversAsync(shortcutCovers);

        // 下方分节改为 Tab：最近播放 / 最近添加歌曲 / 最近添加专辑
        var definitions = new List<(string Label, Func<Control> Build)>();
        if (recent.Count > 0)
        {
            definitions.Add(("最近播放", () => BuildTrackTab(recent)));
        }
        if (latest.Count > 0)
        {
            definitions.Add(("最近添加歌曲", () => BuildTrackTab(latest)));
        }
        if (albums.Count > 0)
        {
            definitions.Add(("最近添加专辑", () =>
            {
                var pending = new List<(string?, Image)>();
                var panel = new WrapPanel { Margin = new Thickness(-4, 2, 0, 0) };
                foreach (var album in albums)
                {
                    panel.Children.Add(BuildCard(album.Name, album.Artist, album.CoverId,
                        () => OpenAlbumAsync(album), pending));
                }
                _ = LoadCardCoversAsync(pending);
                return panel;
            }));
        }

        if (definitions.Count == 0)
        {
            AddEmptyState("欢迎使用 25DogMusic", "去「音乐库」挑一首歌开始播放吧", Symbol.MusicNote2);
        }
        else
        {
            var tabRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Margin = new Thickness(0, 18, 0, 4)
            };
            ContentPanel.Children.Add(tabRow);

            var host = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
            ContentPanel.Children.Add(host);

            var buttons = new Button[definitions.Count];
            var contents = new Control?[definitions.Count];

            void Activate(int index)
            {
                for (var i = 0; i < buttons.Length; i++)
                {
                    if (i == index) buttons[i].Classes.Add("active");
                    else buttons[i].Classes.Remove("active");
                }
                host.Children.Clear();
                contents[index] ??= definitions[index].Build();
                host.Children.Add(contents[index]!);
            }

            for (var i = 0; i < definitions.Count; i++)
            {
                var index = i;
                var button = new Button
                {
                    Content = new TextBlock
                    {
                        Text = definitions[i].Label,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                };
                button.Classes.Add("searchtab");
                button.Click += (_, _) => Activate(index);
                buttons[i] = button;
                tabRow.Children.Add(button);
            }

            Activate(0);
        }

        StatusText.Text = "首页";
    }

    Control BuildTrackTab(List<Track> tracks)
    {
        var panel = new StackPanel { Spacing = 2, Margin = new Thickness(0, 2, 0, 0) };
        for (var i = 0; i < tracks.Count; i++)
        {
            panel.Children.Add(BuildTrackRow(tracks[i], tracks, i));
        }
        _ = LoadTrackCoversAsync(tracks);
        return panel;
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
            MinHeight = 96,
            CornerRadius = new CornerRadius(14),
            ClipToBounds = true,
            Background = CoverPlaceholder,
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = content,
            RenderTransform = new TranslateTransform(0, 0),
            Transitions = new Transitions
            {
                new TransformOperationsTransition
                {
                    Property = RenderTransformProperty,
                    Duration = TimeSpan.FromMilliseconds(180),
                    Easing = new CubicEaseOut()
                }
            }
        };

        card.PointerEntered += (_, _) =>
        {
            card.RenderTransform = new TranslateTransform(0, -5);
            card.BoxShadow = BoxShadows.Parse("0 12 26 0 #4D000000");
            card.Opacity = 1.0;
        };
        card.PointerExited += (_, _) =>
        {
            card.RenderTransform = new TranslateTransform(0, 0);
            card.BoxShadow = default;
        };
        card.PointerPressed += async (_, _) => await onClick();
        return card;
    }

    async Task LoadCoverSwatchAsync(Image image)
    {
        var coverId = _currentTrack?.CoverId;
        if (string.IsNullOrWhiteSpace(coverId))
        {
            try
            {
                using var doc = await Api().GetAlbumsAsync(1, 20);
                var albums = Library.ParseAlbums(doc);
                if (albums.Count > 0) coverId = albums[Random.Shared.Next(albums.Count)].CoverId;
            }
            catch
            {
                // 取不到随机封面时保留渐变色块
            }
        }

        if (string.IsNullOrWhiteSpace(coverId)) return;
        var bitmap = await GetCoverAsync(coverId, 96);
        if (bitmap is not null) image.Source = bitmap;
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
        await ShowBrowseSplitAsync(
            Mode.Artists, NavArtists,
            p => Api().GetArtistsAsync(p, 200),
            doc => Library.ParseArtists(doc)
                .Select(a => new BrowseItem(a.Guid, a.Name, a.TrackCount, a.AlbumCount, a.CoverId))
                .ToList(),
            (guid, p) => Api().GetTracksByArtistAsync(guid, p, 200),
            (guid, p) => Api().GetAlbumsByArtistAsync(guid, p, 150),
            "位歌手", "暂无歌手", "这位歌手还没有曲目", "这位歌手还没有专辑");
    }

    /// <summary>歌手 / 风格左右分栏页：左侧列表选中后，右侧展示信息与「歌曲 / 专辑」两个 Tab。</summary>
    async Task ShowBrowseSplitAsync(
        Mode mode,
        Button nav,
        Func<int, Task<JsonDocument>> listFetch,
        Func<JsonDocument, List<BrowseItem>> listParse,
        Func<string, int, Task<JsonDocument>> tracksFetch,
        Func<string, int, Task<JsonDocument>>? albumsFetch,
        string unit,
        string emptyListTip,
        string emptyTracksTip,
        string emptyAlbumsTip)
    {
        _mode = mode;
        SetActiveNav(nav);
        BeginPage(string.Empty, string.Empty);

        var root = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("300,1,*"),
            ColumnSpacing = 16,
            Margin = new Thickness(0, 4, 0, 0)
        };
        ContentPanel.Children.Add(root);
        _splitGrid = root;
        Dispatcher.UIThread.Post(UpdateSplitHeight, DispatcherPriority.Background);

        // 左栏：可独立滚动的选中列表
        var listStack = new StackPanel { Spacing = 2 };
        var listScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = listStack
        };
        root.Children.Add(listScroll);

        var divider = new Border { Width = 1, Background = Divider, Margin = new Thickness(0, 2, 0, 2) };
        Grid.SetColumn(divider, 1);
        root.Children.Add(divider);

        // 右栏：信息 + Tab + 内容
        var right = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*"), RowSpacing = 10 };
        Grid.SetColumn(right, 2);
        root.Children.Add(right);

        var avatarImage = new Image { Stretch = Stretch.UniformToFill };
        var avatarHolder = new Grid();
        avatarHolder.Children.Add(MakeIcon(Symbol.MusicNote2, 22, TextSecondary));
        avatarHolder.Children.Add(avatarImage);
        var avatar = new Border
        {
            Width = 64,
            Height = 64,
            CornerRadius = new CornerRadius(16),
            ClipToBounds = true,
            Background = CardBg,
            Child = avatarHolder,
            VerticalAlignment = VerticalAlignment.Center
        };

        var nameText = new TextBlock
        {
            FontSize = 20,
            FontWeight = FontWeight.SemiBold,
            Foreground = TextPrimary,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var subText = new TextBlock { FontSize = 12, Foreground = TextSecondary, Margin = new Thickness(0, 3, 0, 0) };
        var heroTexts = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        heroTexts.Children.Add(nameText);
        heroTexts.Children.Add(subText);
        Grid.SetColumn(heroTexts, 1);

        var hero = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 14 };
        hero.Children.Add(avatar);
        hero.Children.Add(heroTexts);
        right.Children.Add(hero);

        var songTab = new Button { Content = new TextBlock { Text = "歌曲", VerticalAlignment = VerticalAlignment.Center } };
        songTab.Classes.Add("searchtab");
        var albumTab = new Button { Content = new TextBlock { Text = "专辑", VerticalAlignment = VerticalAlignment.Center } };
        albumTab.Classes.Add("searchtab");
        var tabRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        tabRow.Children.Add(songTab);
        tabRow.Children.Add(albumTab);
        Grid.SetRow(tabRow, 1);
        right.Children.Add(tabRow);

        var detailStack = new StackPanel { Spacing = 2, Margin = new Thickness(0, 2, 0, 0) };
        var detailScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = detailStack
        };
        Grid.SetRow(detailScroll, 2);
        right.Children.Add(detailScroll);

        var items = new List<BrowseItem>();
        var rows = new Dictionary<string, Border>();
        BrowseItem? current = null;
        var loadedTrackGuids = new List<string>();
        var panelVersion = 0;
        var listPage = 0;
        var listTotal = -1;
        var listLoading = false;

        var tabIndex = 0;
        var songPage = 0;
        var songTotal = -1;
        var songLoading = false;
        var songs = new List<Track>();
        var albumPage = 0;
        var albumTotal = -1;
        var albumLoading = false;
        var albums = new List<Album>();
        WrapPanel? albumWrap = null;
        var derivedLoaded = false;

        void AddDetailHint(string text)
        {
            detailStack.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 14,
                Foreground = TextSecondary,
                Margin = new Thickness(2, 28, 0, 0)
            });
        }

        void CleanupTrackMaps()
        {
            foreach (var guid in loadedTrackGuids)
            {
                _rowBorders.Remove(guid);
                _rowImages.Remove(guid);
                _rowNumbers.Remove(guid);
                _heartIcons.Remove(guid);
            }
            loadedTrackGuids.Clear();
        }

        void UpdateTabStyles()
        {
            if (tabIndex == 0)
            {
                songTab.Classes.Add("active");
                albumTab.Classes.Remove("active");
            }
            else
            {
                albumTab.Classes.Add("active");
                songTab.Classes.Remove("active");
            }
        }

        void EnsureAlbumWrap()
        {
            if (albumWrap is not null) return;
            albumWrap = new WrapPanel { Margin = new Thickness(-4, 4, 0, 0) };
            detailStack.Children.Add(albumWrap);
        }

        void RenderAlbums(IReadOnlyList<Album> list)
        {
            var pending = new List<(string?, Image)>();
            EnsureAlbumWrap();
            foreach (var album in list)
            {
                albumWrap!.Children.Add(BuildCard(album.Name, album.Artist, album.CoverId,
                    () => OpenAlbumAsync(album), pending));
            }
            if (pending.Count > 0) _ = LoadCardCoversAsync(pending);
        }

        async Task LoadSongsAsync(int version)
        {
            if (current is null || songLoading || (songTotal >= 0 && songs.Count >= songTotal)) return;
            songLoading = true;
            try
            {
                songPage++;
                using var doc = await tracksFetch(current.Guid, songPage);
                if (version != panelVersion) return;

                if (songTotal < 0) songTotal = PageTotal(doc);
                var batch = Library.ParseTracks(doc);
                if (batch.Count == 0)
                {
                    songTotal = songs.Count;
                    if (songs.Count == 0) AddDetailHint(emptyTracksTip);
                    return;
                }

                var start = songs.Count;
                songs.AddRange(batch);
                var stack = new StackPanel { Spacing = 2, Margin = new Thickness(0, 2, 0, 0) };
                for (var i = 0; i < batch.Count; i++)
                {
                    stack.Children.Add(BuildTrackRow(batch[i], songs, start + i));
                    loadedTrackGuids.Add(batch[i].Guid);
                }
                detailStack.Children.Add(stack);
                _ = LoadTrackCoversAsync(batch);
                Dispatcher.UIThread.Post(UpdateRowHighlight);
            }
            catch (Exception ex)
            {
                HandleError(ex, "加载歌曲失败");
            }
            finally
            {
                if (version == panelVersion) songLoading = false;
            }
        }

        async Task LoadAlbumsTabAsync(int version)
        {
            if (current is null) return;

            if (albumsFetch is null)
            {
                // 风格没有专辑接口：把该风格下的曲目按专辑归并后展示
                if (derivedLoaded) return;
                derivedLoaded = true;
                try
                {
                    var all = new List<Track>();
                    for (var page = 1; page <= 16; page++)
                    {
                        using var doc = await tracksFetch(current.Guid, page);
                        if (version != panelVersion) return;
                        var batch = Library.ParseTracks(doc);
                        if (batch.Count == 0) break;
                        all.AddRange(batch);
                        if (all.Count >= PageTotal(doc)) break;
                    }

                    var list = BuildAlbumsFromTracks(all);
                    if (version != panelVersion) return;
                    if (list.Count == 0) AddDetailHint(emptyAlbumsTip);
                    else RenderAlbums(list);
                }
                catch (Exception ex)
                {
                    HandleError(ex, "加载专辑失败");
                }
                return;
            }

            if (albumLoading || (albumTotal >= 0 && albums.Count >= albumTotal)) return;
            albumLoading = true;
            try
            {
                albumPage++;
                using var doc = await albumsFetch(current.Guid, albumPage);
                if (version != panelVersion) return;

                if (albumTotal < 0) albumTotal = PageTotal(doc);
                var batch = Library.ParseAlbums(doc);
                if (batch.Count == 0)
                {
                    albumTotal = albums.Count;
                    if (albums.Count == 0) AddDetailHint(emptyAlbumsTip);
                    return;
                }

                albums.AddRange(batch);
                RenderAlbums(batch);
            }
            catch (Exception ex)
            {
                HandleError(ex, "加载专辑失败");
            }
            finally
            {
                if (version == panelVersion) albumLoading = false;
            }
        }

        async Task ShowTabAsync(int index)
        {
            panelVersion++;
            var version = panelVersion;
            tabIndex = index;
            UpdateTabStyles();
            detailStack.Children.Clear();
            albumWrap = null;

            if (index == 0)
            {
                songs.Clear();
                songPage = 0;
                songTotal = -1;
                await LoadSongsAsync(version);
            }
            else
            {
                albums.Clear();
                albumPage = 0;
                albumTotal = -1;
                derivedLoaded = false;
                await LoadAlbumsTabAsync(version);
            }

            detailScroll.Offset = new Vector(0, 0);
        }

        async Task SelectAsync(BrowseItem item)
        {
            current = item;
            foreach (var (guid, row) in rows)
            {
                row.Background = guid == item.Guid ? RowActive : Brushes.Transparent;
            }

            nameText.Text = item.Name;
            subText.Text = item.AlbumCount > 0
                ? $"{item.TrackCount} 首 · {item.AlbumCount} 张专辑"
                : $"{item.TrackCount} 首";

            avatarImage.Source = null;
            if (!string.IsNullOrWhiteSpace(item.CoverId))
            {
                var bitmap = await GetCoverAsync(item.CoverId, 96);
                if (current == item) avatarImage.Source = bitmap;
            }

            songs.Clear();
            albums.Clear();
            CleanupTrackMaps();
            _ = SetBackdropAsync(item.CoverId);
            await ShowTabAsync(tabIndex);
        }

        Border MakeRow(BrowseItem item, List<(string?, Image)> pending)
        {
            var image = new Image { Stretch = Stretch.UniformToFill };
            var holder = new Grid();
            holder.Children.Add(MakeIcon(Symbol.MusicNote2, 18, TextSecondary));
            holder.Children.Add(image);
            if (!string.IsNullOrWhiteSpace(item.CoverId)) pending.Add((item.CoverId, image));

            var box = new Border
            {
                Width = 44,
                Height = 44,
                CornerRadius = new CornerRadius(22),
                ClipToBounds = true,
                Background = CardBg,
                Child = holder
            };

            var name = new TextBlock
            {
                Text = item.Name,
                FontSize = 13.5,
                Foreground = TextPrimary,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var sub = new TextBlock
            {
                Text = item.AlbumCount > 0 ? $"{item.TrackCount} 首 · {item.AlbumCount} 张专辑" : $"{item.TrackCount} 首",
                FontSize = 11,
                Foreground = TextSecondary
            };
            var texts = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
            texts.Children.Add(name);
            texts.Children.Add(sub);

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
            grid.Children.Add(box);
            Grid.SetColumn(texts, 1);
            grid.Children.Add(texts);

            var row = new Border
            {
                Padding = new Thickness(8, 5),
                CornerRadius = new CornerRadius(9),
                Background = Brushes.Transparent,
                Child = grid,
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            row.PointerEntered += (_, _) =>
            {
                if (item.Guid != current?.Guid) row.Background = RowHover;
            };
            row.PointerExited += (_, _) =>
            {
                row.Background = item.Guid == current?.Guid ? RowActive : Brushes.Transparent;
            };
            row.PointerPressed += async (_, _) => await SelectAsync(item);
            return row;
        }

        async Task LoadListPageAsync()
        {
            if (listLoading || (listTotal >= 0 && items.Count >= listTotal)) return;
            listLoading = true;
            try
            {
                listPage++;
                using var doc = await listFetch(listPage);
                if (listTotal < 0) listTotal = PageTotal(doc);

                var batch = listParse(doc);
                if (batch.Count == 0)
                {
                    listTotal = items.Count;
                    if (items.Count == 0) AddDetailHint(emptyListTip);
                    return;
                }

                var pending = new List<(string?, Image)>();
                foreach (var item in batch)
                {
                    items.Add(item);
                    var row = MakeRow(item, pending);
                    rows[item.Guid] = row;
                    listStack.Children.Add(row);
                }
                if (pending.Count > 0) _ = LoadCardCoversAsync(pending);

                StatusText.Text = $"已加载 {items.Count} {unit}";
                if (current is null) await SelectAsync(items[0]);
            }
            catch (Exception ex)
            {
                HandleError(ex, "加载列表失败");
            }
            finally
            {
                listLoading = false;
            }
        }

        listScroll.ScrollChanged += (_, _) =>
        {
            if (listScroll.Extent.Height <= 0) return;
            if (listScroll.Offset.Y + listScroll.Viewport.Height >= listScroll.Extent.Height - 120)
            {
                _ = LoadListPageAsync();
            }
        };

        detailScroll.ScrollChanged += (_, _) =>
        {
            if (detailScroll.Extent.Height <= 0) return;
            if (detailScroll.Offset.Y + detailScroll.Viewport.Height < detailScroll.Extent.Height - 160) return;

            var version = panelVersion;
            if (tabIndex == 0) _ = LoadSongsAsync(version);
            else _ = LoadAlbumsTabAsync(version);
        };

        songTab.Click += async (_, _) =>
        {
            if (tabIndex == 0) return;
            await ShowTabAsync(0);
        };
        albumTab.Click += async (_, _) =>
        {
            if (tabIndex == 1) return;
            await ShowTabAsync(1);
        };

        UpdateTabStyles();
        await LoadListPageAsync();
    }

    /// <summary>把曲目按专辑归并（风格页没有专辑接口时使用）。</summary>
    static List<Album> BuildAlbumsFromTracks(IEnumerable<Track> tracks)
    {
        var order = new List<string>();
        var map = new Dictionary<string, (string Name, string Artist, int Count, string CoverId)>(StringComparer.OrdinalIgnoreCase);

        foreach (var track in tracks)
        {
            var key = string.IsNullOrWhiteSpace(track.AlbumGuid) ? track.AlbumName : track.AlbumGuid;
            if (string.IsNullOrWhiteSpace(key)) continue;

            if (map.TryGetValue(key, out var entry))
            {
                map[key] = (entry.Name, entry.Artist, entry.Count + 1,
                    string.IsNullOrWhiteSpace(entry.CoverId) ? track.CoverId : entry.CoverId);
            }
            else
            {
                map[key] = (string.IsNullOrWhiteSpace(track.AlbumName) ? "未知专辑" : track.AlbumName,
                    track.Artist, 1, track.CoverId);
                order.Add(key);
            }
        }

        var result = new List<Album>(order.Count);
        foreach (var key in order)
        {
            var entry = map[key];
            result.Add(new Album(key, entry.Name, entry.Artist, entry.Count, entry.CoverId));
        }
        return result;
    }

    void ShowFoldersPage() => _ = LoadFolderRootsAsync();

    /// <summary>文件夹首页：列出所有共享库作为浏览根目录。</summary>
    async Task LoadFolderRootsAsync()
    {
        _mode = Mode.Folders;
        _currentPlaylist = null;
        SetActiveNav(NavFolders);
        BeginListPage("文件夹", "从共享库开始按目录浏览");

        try
        {
            using var doc = await Api().GetSharedLibrariesAsync();
            var libraries = Library.ParseSharedLibraries(doc);

            if (libraries.Count == 0)
            {
                AddEmptyState("暂无共享库", "在飞牛音乐里添加共享目录后即可在这里浏览", Symbol.Folder);
                return;
            }

            foreach (var library in libraries)
            {
                ListRows.Children.Add(BuildFolderRow(library.Name, library.Path, 0,
                    () => OpenFolderAsync(library.Path, library.Path)));
            }

            StatusText.Text = $"共享库 {libraries.Count} 个";
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载共享库失败");
        }
    }

    /// <summary>打开某个目录：显示子文件夹和其中的歌曲。</summary>
    async Task OpenFolderAsync(string path, string floor)
    {
        _mode = Mode.Folders;
        _currentPlaylist = null;
        SetActiveNav(NavFolders);

        try
        {
            using var doc = await Api().GetFolderViewAsync(path);
            var folder = Library.ParseFolderView(doc);
            if (folder is null)
            {
                BeginListPage("文件夹", path);
                AddEmptyState("打不开文件夹", "服务端没有返回目录内容", Symbol.Folder);
                return;
            }

            Func<Task> up = async () =>
            {
                if (path.TrimEnd('/') == floor.TrimEnd('/')) await LoadFolderRootsAsync();
                else await OpenFolderAsync(ParentPath(path), floor);
            };

            BeginListPage(
                string.IsNullOrWhiteSpace(folder.Name) ? path : folder.Name,
                $"{folder.Path} · {folder.Tracks.Count} 首",
                BuildDetailActions(up, folder.Tracks));

            foreach (var sub in folder.Folders)
            {
                ListRows.Children.Add(BuildFolderRow(sub.Name, sub.Path, sub.ItemCount,
                    () => OpenFolderAsync(sub.Path, floor)));
            }

            _tracks = folder.Tracks;
            if (_tracks.Count > 0)
            {
                AddTrackRows(_tracks, _tracks);
            }
            else if (folder.Folders.Count == 0)
            {
                AddEmptyState("空文件夹", "这里没有子目录或歌曲", Symbol.Folder);
            }

            StatusText.Text = $"{folder.Name} · {_tracks.Count} 首";
        }
        catch (Exception ex)
        {
            HandleError(ex, "打开文件夹失败");
        }
    }

    static string ParentPath(string path)
    {
        var trimmed = path.TrimEnd('/');
        var index = trimmed.LastIndexOf('/');
        return index > 0 ? trimmed[..index] : "/";
    }

    /// <summary>目录行：图标 + 名称 + 路径/条目数 + 右侧箭头。</summary>
    Control BuildFolderRow(string name, string path, int itemCount, Func<Task> onClick)
    {
        var icon = MakeIcon(Symbol.Folder, 17, IconIdle);

        var title = new TextBlock
        {
            Text = name,
            FontSize = 13.5,
            Foreground = TextPrimary,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var sub = new TextBlock
        {
            Text = itemCount > 0 ? $"{path} · {itemCount} 项" : path,
            FontSize = 10.5,
            Foreground = TextSecondary,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var texts = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 0, 0) };
        texts.Children.Add(title);
        texts.Children.Add(sub);

        var chevron = MakeIcon(Symbol.ChevronRight, 12, IconIdle);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("30,*,Auto"), VerticalAlignment = VerticalAlignment.Center };
        grid.Children.Add(icon);
        Grid.SetColumn(texts, 1);
        grid.Children.Add(texts);
        Grid.SetColumn(chevron, 2);
        grid.Children.Add(chevron);

        var row = new Border
        {
            Padding = new Thickness(8, 7),
            CornerRadius = new CornerRadius(10),
            Background = Brushes.Transparent,
            Child = grid,
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        row.PointerEntered += (_, _) => row.Background = RowHover;
        row.PointerExited += (_, _) => row.Background = Brushes.Transparent;
        row.PointerPressed += async (_, _) => await onClick();
        return row;
    }

    async Task OpenPlaylistAsync(Playlist playlist)
    {
        Func<Task> back = LoadHomeAsync;
        if (_mode == Mode.Search && _lastSearch.Length > 0) back = () => SearchAsync(_lastSearch);

        SetActiveNav(null);
        try
        {
            StartPagedTracks(p => Api().GetPlaylistTracksAsync(playlist.Guid, p, 200),
                playlist.Name, "歌单 · ", Mode.Tracks);
            _currentPlaylist = playlist;

            var actions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center
            };
            actions.Children.Add(MakePill("返回", Symbol.ArrowLeft, back));
            actions.Children.Add(MakePill("添加歌曲", Symbol.Add, AddSongsToCurrentPlaylistAsync));
            actions.Children.Add(CreatePlayAllAction(_tracks));
            _pagedAction = actions;

            await LoadPagedPageAsync();
            if (_tracks.Count == 0) AddEmptyState("歌单是空的", "点「添加歌曲」从曲库挑选，或在歌曲右侧点「+」加入歌单", Symbol.TextBulletList);
            if (!string.IsNullOrWhiteSpace(playlist.CoverId)) _ = SetBackdropAsync(playlist.CoverId);
        }
        catch (Exception ex)
        {
            HandleError(ex, "加载歌单失败");
        }
    }

    async Task AddSongsToCurrentPlaylistAsync()
    {
        var playlist = _currentPlaylist;
        if (playlist is null) return;
        await ShowPickerDrawerAsync(playlist);
    }

    /// <summary>从当前正在浏览的歌单里移除一首歌。</summary>
    async Task RemoveFromCurrentPlaylistAsync(Track track)
    {
        var playlist = _currentPlaylist;
        if (playlist is null) return;

        try
        {
            await Api().RemoveTrackFromPlaylistAsync(playlist.Guid, track.Guid);
            StatusText.Text = $"已从歌单移除「{track.Title}」";
            await OpenPlaylistAsync(playlist);
        }
        catch (Exception ex)
        {
            HandleError(ex, "移出歌单失败");
        }
    }

    /// <summary>搜索结果：歌曲 / 专辑 / 歌手 / 歌单 四个 Tab，各自滚动到底部自动加载下一页。</summary>
    async Task SearchAsync(string keyword)
    {
        keyword = keyword.Trim();
        if (string.IsNullOrWhiteSpace(keyword)) return;

        _mode = Mode.Search;
        _lastSearch = keyword;
        SetActiveNav(null);
        BeginPage($"搜索 “{keyword}”", "歌曲 · 专辑 · 歌手 · 歌单");

        var tabRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 10, 0, 2)
        };
        ContentPanel.Children.Add(tabRow);

        var host = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        var listScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = host
        };
        ContentPanel.Children.Add(listScroll);
        _searchListScroll = listScroll;
        Dispatcher.UIThread.Post(UpdateSearchListHeight, DispatcherPriority.Background);

        // 页面本身不再滚动：列表区内部滚动，滚到底自动加载下一页
        listScroll.ScrollChanged += (_, _) =>
        {
            if (listScroll.Extent.Height <= 0) return;
            if (listScroll.Offset.Y + listScroll.Viewport.Height >= listScroll.Extent.Height - 200)
            {
                _ = LoadGridPageAsync();
            }
        };

        var tabs = new (string Label, Func<int, Task<JsonDocument>> Fetch, string Unit)[]
        {
            ("歌曲", p => Api().SearchTracksAsync(keyword, p, 100), "首"),
            ("专辑", p => Api().SearchAlbumsAsync(keyword, p, 40), "张专辑"),
            ("歌手", p => Api().SearchArtistsAsync(keyword, p, 40), "位歌手"),
            ("歌单", p => Api().SearchPlaylistsAsync(keyword, p, 40), "个歌单")
        };

        var buttons = new Button[tabs.Length];

        async Task ActivateAsync(int index)
        {
            for (var i = 0; i < buttons.Length; i++)
            {
                if (i == index) buttons[i].Classes.Add("active");
                else buttons[i].Classes.Remove("active");
            }

            _gridActive = false;
            host.Children.Clear();

            Panel panel;
            if (index is 1 or 3)
            {
                var wrap = new WrapPanel { Margin = new Thickness(-4, 4, 0, 0) };
                host.Children.Add(wrap);
                panel = wrap;
            }
            else
            {
                var stack = new StackPanel { Spacing = 2, Margin = new Thickness(0, 2, 0, 0) };
                host.Children.Add(stack);
                panel = stack;
            }
            _gridPanel = panel;

            switch (index)
            {
                case 0:
                {
                    // 歌曲：行列表，队列随分页增长，后续页的行仍按整队列连播
                    var queue = new List<Track>();
                    StartGridPaging(tabs[index].Fetch, (doc, pending) =>
                    {
                        var batch = Library.ParseTracks(doc);
                        var controls = new List<Control>();
                        foreach (var track in batch)
                        {
                            queue.Add(track);
                            controls.Add(BuildTrackRow(track, queue, queue.Count - 1));
                        }
                        if (batch.Count > 0)
                        {
                            _ = LoadTrackCoversAsync(batch);
                            Dispatcher.UIThread.Post(UpdateRowHighlight);
                        }
                        return controls;
                    }, tabs[index].Unit);
                    break;
                }
                case 1:
                    StartGridPaging(tabs[index].Fetch, (doc, pending) =>
                    {
                        var controls = new List<Control>();
                        foreach (var album in Library.ParseAlbums(doc))
                        {
                            controls.Add(BuildCard(album.Name, album.Artist, album.CoverId,
                                () => OpenAlbumAsync(album), pending));
                        }
                        return controls;
                    }, tabs[index].Unit);
                    break;
                case 2:
                    StartGridPaging(tabs[index].Fetch, (doc, pending) =>
                    {
                        var controls = new List<Control>();
                        foreach (var artist in Library.ParseArtists(doc))
                        {
                            controls.Add(BuildArtistRow(artist, pending));
                        }
                        return controls;
                    }, tabs[index].Unit);
                    break;
                default:
                    StartGridPaging(tabs[index].Fetch, (doc, pending) =>
                    {
                        var controls = new List<Control>();
                        foreach (var playlist in Library.ParsePlaylists(doc))
                        {
                            controls.Add(BuildCard(playlist.Name, $"{playlist.TrackCount} 首", playlist.CoverId,
                                () => OpenPlaylistAsync(playlist), pending));
                        }
                        return controls;
                    }, tabs[index].Unit);
                    break;
            }

            listScroll.Offset = new Vector(0, 0);
            UpdateSearchListHeight();
            await LoadGridPageAsync();

            if (_gridLoaded == 0)
            {
                host.Children.Add(new TextBlock
                {
                    Text = $"没有找到{tabs[index].Label}",
                    FontSize = 13,
                    Foreground = TextSecondary,
                    Margin = new Thickness(2, 24, 0, 0)
                });
            }
        }

        for (var i = 0; i < tabs.Length; i++)
        {
            var index = i;
            var button = new Button
            {
                Content = new TextBlock
                {
                    Text = tabs[i].Label,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            button.Classes.Add("searchtab");
            button.Click += async (_, _) => await ActivateAsync(index);
            buttons[i] = button;
            tabRow.Children.Add(button);
        }

        await ActivateAsync(0);
        StatusText.Text = $"搜索：{keyword}";
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
        if (_currentTrack is null && _queue.Count == 0) return;
        var player = EnsurePlayer();

        if (player.IsPlaying)
        {
            player.TogglePause();
            _paused = true;
            SetPlayIcon(false);
            SyncTimer();
            return;
        }

        if (_paused && _currentTrack is not null)
        {
            player.TogglePause();
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
        if (_proxy is null) Api();
        var player = EnsurePlayer();

        player.Play(_proxy!.BaseUrl + track.Guid);
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
            // 切歌即加载歌词，桌面歌词据此决定是否显示
            _ = LoadLyricsAsync(track.Guid);
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

        SyncSpectrum(active && !IsBackgrounded);
    }

    /// <summary>桌面歌词按钮的开启状态配色。</summary>
    void UpdateDesktopLyricsButton()
    {
        DesktopLyricsIcon.Foreground = _config.DesktopLyrics ? AccentSoft : IconIdle;
    }

    /// <summary>频谱只在全屏页可见且正在播放时采集，避免后台无谓占用。</summary>
    void SyncSpectrum(bool playing)
    {
        var shouldRun = playing && _config.SpectrumEnabled && NowPlayingOverlay.IsVisible;
        if (shouldRun)
        {
            _spectrum.Start();
        }
        else
        {
            _spectrum.Stop();
        }
        OverlaySpectrum.SetActive(shouldRun);
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
        if (e.Key == Key.Escape && PickerPanel.IsVisible)
        {
            HidePickerDrawer();
            e.Handled = true;
            return;
        }

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
        SyncSpectrum(_player?.IsPlaying == true && !_paused);
        OverlayTitle.Text = _currentTrack.Title;
        OverlaySub.Text = NowSub.Text;
        SetPlayIcon(_player?.IsPlaying == true);

        _ = RefreshOverlayAsync(_currentTrack);
        if (_lyricsGuid != _currentTrack.Guid) _ = LoadLyricsAsync(_currentTrack.Guid);
    }

    void HideOverlay()
    {
        NowPlayingOverlay.IsVisible = false;
        SyncSpectrum(playing: false);
    }

    async Task RefreshOverlayAsync(Track track)
    {
        var cover = await GetCoverAsync(track.CoverId, 600);
        if (cover is not null)
        {
            OverlayCover.Source = cover;
            OverlayBackdrop.Source = cover;
        }
    }

    async Task LoadLyricsAsync(string trackGuid)
    {
        _lyricsGuid = trackGuid;
        _lyrics = new List<LyricLine>();
        _lyricIndex = -1;
        _lyricBlocks.Clear();
        LyricsPanel.Children.Clear();
        LyricsScroll.Offset = new Vector(0, 0);
        // 切歌瞬间先收起旧歌词浮窗，等新歌词加载成功再出现
        ApplyDesktopLyrics();

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
            ApplyDesktopLyrics();
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
        // 歌词已就绪：若桌面歌词开关开着，此时才让浮窗出现
        ApplyDesktopLyrics();
    }

    void UpdateOverlayLyrics(bool force = false)
    {
        if (_lyrics.Count == 0 || _player is null) return;

        var position = TimeSpan.FromMilliseconds(Math.Max(0, _player.Position));
        var index = -1;

        for (var i = 0; i < _lyrics.Count; i++)
        {
            if (_lyrics[i].Time <= position) index = i;
            else break;
        }

        if (!force && index == _lyricIndex) return;

        _lyricIndex = index;
        UpdateDesktopLyricLines(index);

        if (!NowPlayingOverlay.IsVisible) return;

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

    void UpdateDesktopLyricLines(int index)
    {
        if (_desktopLyrics is null) return;
        var current = index >= 0 && index < _lyrics.Count ? _lyrics[index].Text : string.Empty;
        var next = index + 1 < _lyrics.Count ? _lyrics[index + 1].Text : string.Empty;
        _desktopLyrics.SetLines(current, next);
    }

    /// <summary>按配置显示/隐藏桌面歌词浮窗；没有歌词时不显示。</summary>
    void ApplyDesktopLyrics()
    {
        if (!_config.DesktopLyrics || _lyrics.Count == 0)
        {
            CloseDesktopLyricsSilently();
            return;
        }

        if (_desktopLyrics is null)
        {
            var window = new DesktopLyricsWindow();
            window.Closed += (_, _) =>
            {
                _desktopLyrics = null;
                if (_desktopLyricsAutoClose)
                {
                    _desktopLyricsAutoClose = false;
                    return;
                }
                _config.DesktopLyrics = false;
                _config.Save();
                UpdateDesktopLyricsButton();
            };
            _desktopLyrics = window;
            window.ShowBottomCenter();
        }
        else if (!_desktopLyrics.IsVisible)
        {
            _desktopLyrics.ShowBottomCenter();
        }

        UpdateDesktopLyricLines(_lyricIndex);
    }

    /// <summary>因切歌/无歌词自动关闭浮窗，不改变用户的开关偏好。</summary>
    void CloseDesktopLyricsSilently()
    {
        if (_desktopLyrics is null) return;
        _desktopLyricsAutoClose = true;
        _desktopLyrics.Close();
        _desktopLyrics = null;
    }

    void SetPlayIcon(bool playing)
    {
        var symbol = playing ? Symbol.Pause : Symbol.Play;
        PlayIcon.Symbol = symbol;
        OverlayPlayIcon.Symbol = symbol;
        if (_roamPlayIcon is not null) _roamPlayIcon.Symbol = symbol;
    }

    // ---- 后台播放 / 托盘 ----

    TrayIcon? CreateTrayIcon()
    {
        try
        {
            var tray = new TrayIcon
            {
                Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://25DogMusic/Assets/appicon.ico"))),
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
        catch
        {
            // Linux 等缺少托盘支持的环境下静默跳过，窗口关闭即退出
            return null;
        }
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
        // 后台播放开启且存在托盘时，关闭窗口只是隐藏到托盘，播放不中断
        if (!_reallyExit && _tray is not null && _config.BackgroundPlayback && _currentTrack is not null)
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
