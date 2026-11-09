// 音乐库相关页面：分页网格、首页卡片、歌手 / 文件夹、收藏与歌单操作。

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

public partial class MainWindow : Window
{

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

        // 漫游页由队列区域自行滚动，禁用页面滚动避免出现双层滚动条
        if (_roaming && _roamId is not null && _currentTrack is not null)
        {
            BeginPage("漫游", "根据收藏与播放记录持续推荐");
            ContentScroll.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
            _roamQueueSection = null;
            if (!_roamTracks.Any(track => track.Guid == _currentTrack.Guid)) _roamTracks.Add(_currentTrack);
            BuildRoamHero(_currentTrack);
            BuildRoamQueueSection();
            StatusText.Text = "漫游进行中";
            return;
        }

        BeginPage("漫游", "根据收藏与播放记录持续推荐");
        ContentScroll.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;

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
            _roamUpcoming.Clear();
            _roamPrefetchCursor = result.Value.RoamId;
            _roamQueueSection = null;

            BuildRoamHero(result.Value.Track);
            BuildRoamQueueSection();
            PlayTrack(result.Value.Track);
            StatusText.Text = "漫游已开始";

            // 预取后续若干首，让下方队列一次能展示多条而不是只有当前一首
            _ = PrefetchRoamAsync();
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
            Foreground = AccentSoft,
            HorizontalAlignment = HorizontalAlignment.Left,
            TextAlignment = TextAlignment.Left
        };

        var title = new TextBlock
        {
            Text = track.Title,
            FontSize = 30,
            FontWeight = FontWeight.Bold,
            Foreground = TextPrimary,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 540,
            HorizontalAlignment = HorizontalAlignment.Left,
            TextAlignment = TextAlignment.Left
        };
        _roamTitle = title;

        var sub = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(track.AlbumName)
                ? track.Artist
                : $"{track.Artist} · {track.AlbumName}",
            FontSize = 13,
            Foreground = TextSecondary,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Left,
            TextAlignment = TextAlignment.Left
        };
        _roamSub = sub;

        var meta = new TextBlock
        {
            Text = $"时长 {FormatDuration(track.DurationMs)}",
            FontSize = 11.5,
            Foreground = TextMuted,
            HorizontalAlignment = HorizontalAlignment.Left,
            TextAlignment = TextAlignment.Left
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
            HorizontalAlignment = HorizontalAlignment.Left,
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

    /// <summary>漫游页下方展示已听过的曲目与预取到的待播推荐，点卡片可直接切换。</summary>
    void BuildRoamQueueSection()
    {
        // 播放结束时可能自动触发漫游切歌，此时若已离开漫游页，不要再往当前页面追加队列
        if (_mode != Mode.Roam) return;

        if (_roamQueueSection is not null && _roamQueueSection.Parent is Panel oldParent)
        {
            oldParent.Children.Remove(_roamQueueSection);
        }
        _roamQueueSection = null;
        _roamQueueScroll = null;

        if (_roamTracks.Count == 0 && _roamUpcoming.Count == 0) return;

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
            Text = $"已听 {_roamTracks.Count} 首 · 待播 {_roamUpcoming.Count} 首",
            FontSize = 11,
            Foreground = TextMuted,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 2)
        });
        section.Children.Add(header);

        var strip = new WrapPanel { Margin = new Thickness(0, 2, 0, 0) };
        foreach (var track in _roamTracks)
        {
            strip.Children.Add(BuildRoamQueueCard(track, upcoming: false));
        }
        foreach (var item in _roamUpcoming)
        {
            strip.Children.Add(BuildRoamQueueCard(item.Track, upcoming: true));
        }

        // 队列独立滚动：多行自动换行，高度按当前视口剩余空间计算，避免和页面滚动条叠加
        var queueScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = strip
        };
        section.Children.Add(queueScroll);
        _roamQueueScroll = queueScroll;

        ContentPanel.Children.Add(section);
        _roamQueueSection = section;
        Dispatcher.UIThread.Post(UpdateRoamQueueHeight, DispatcherPriority.Background);
    }

    /// <summary>让漫游队列占据页面剩余高度，避免页面与队列同时出现滚动条。</summary>
    void UpdateRoamQueueHeight()
    {
        var scroll = _roamQueueScroll;
        if (scroll?.Parent?.Parent is null) return;

        var top = scroll.TranslatePoint(new Point(0, 0), ContentScroll);
        if (top is null) return;

        // TranslatePoint 返回的是视口坐标，加上滚动偏移换回内容坐标
        var contentTop = top.Value.Y + ContentScroll.Offset.Y;
        var available = ContentScroll.Bounds.Height - contentTop - 12;
        // 队列至少保留 160 高；页面内容本身确实放不下时仍由外层滚动
        scroll.MaxHeight = Math.Max(160, available);
    }

    Control BuildRoamQueueCard(Track track, bool upcoming)
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

        var texts = new StackPanel { Spacing = 1, Margin = new Thickness(0, 7, 0, 0), Opacity = upcoming ? 0.86 : 1 };
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
            Text = isCurrent ? "正在播放" : upcoming ? "即将推荐" : track.Artist,
            FontSize = 10.5,
            Foreground = isCurrent ? AccentSoft : upcoming ? AccentSoft : TextMuted,
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
            Margin = new Thickness(0, 0, 12, 10),
            CornerRadius = new CornerRadius(12),
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = stack,
            Opacity = upcoming ? 0.92 : 1
        };
        card.PointerEntered += (_, _) => card.Background = RowHover;
        card.PointerExited += (_, _) => card.Background = Brushes.Transparent;
        card.PointerPressed += (_, _) =>
        {
            if (upcoming)
            {
                var index = _roamUpcoming.FindIndex(item => item.Track.Guid == track.Guid);
                var chainId = index >= 0 ? _roamUpcoming[index].RoamId : null;
                if (index >= 0) _roamUpcoming.RemoveRange(0, index + 1);
                if (chainId is not null)
                {
                    _roamId = chainId;
                    _roamPrefetchCursor = chainId;
                }
            }

            if (!_roamTracks.Any(item => item.Guid == track.Guid)) _roamTracks.Add(track);
            _queue = new List<Track> { track };
            _queueIndex = 0;
            SyncRoamHero(track);
            BuildRoamQueueSection();
            PlayTrack(track);
            _ = PrefetchRoamAsync();
        };

        _ = AssignRoamQueueCoverAsync(image, track.CoverId);
        return card;
    }

    /// <summary>沿漫游链路向前预取若干首待播曲目，用于队列展示与「下一首」直接消费。</summary>
    async Task PrefetchRoamAsync()
    {
        if (_roamPrefetching || _roamId is null) return;

        _roamPrefetching = true;
        try
        {
            var cursor = _roamPrefetchCursor ?? _roamId;
            var changed = false;

            while (_roamUpcoming.Count < 6)
            {
                using var doc = await Api().RoamNextAsync(cursor);
                var result = Library.ParseRoam(doc);
                if (result is null) break;

                var track = result.Value.Track;
                cursor = result.Value.RoamId;
                _roamPrefetchCursor = cursor;
                changed = true;

                var seen = _roamTracks.Any(item => item.Guid == track.Guid)
                    || _roamUpcoming.Any(item => item.Track.Guid == track.Guid);
                if (!seen) _roamUpcoming.Add((track, result.Value.RoamId));
            }

            if (changed && _mode == Mode.Roam) BuildRoamQueueSection();
        }
        catch
        {
            // 预取失败不影响正常漫游，后续「下一首」会直接请求接口
        }
        finally
        {
            _roamPrefetching = false;
        }
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

        // 下一首优先使用预取队列；上一首会改变漫游链路，需要重新预取
        if (dir > 0 && _roamUpcoming.Count > 0)
        {
            var next = _roamUpcoming[0];
            _roamUpcoming.RemoveAt(0);
            _roamId = next.RoamId;

            _queue = new List<Track> { next.Track };
            _queueIndex = 0;
            if (!_roamTracks.Any(track => track.Guid == next.Track.Guid)) _roamTracks.Add(next.Track);

            SyncRoamHero(next.Track);
            BuildRoamQueueSection();
            PlayTrack(next.Track);
            _ = PrefetchRoamAsync();
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

            if (dir < 0)
            {
                // 回退后原预取链路失效，清空并从新的位置重新预取
                _roamUpcoming.Clear();
                _roamPrefetchCursor = result.Value.RoamId;
            }

            if (!_roamTracks.Any(track => track.Guid == result.Value.Track.Guid))
            {
                _roamTracks.Add(result.Value.Track);
            }

            SyncRoamHero(result.Value.Track);
            BuildRoamQueueSection();
            PlayTrack(result.Value.Track);
            _ = PrefetchRoamAsync();
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

        // 等待期间用户可能已切到别的页面（例如登录失效跳到设置），此时不要再往当前页追加首页内容
        if (_mode != Mode.Home) return;

        var recent = recentTask.Result;
        var favorites = favoritesTask.Result;
        var latest = latestTask.Result;
        var albums = albumsTask.Result;

        // 顶部四个入口卡片：封面堆叠 + 标题，高度固定、宽度随窗口自适应
        var shortcutCovers = new List<(string?, Image)>();
        var cards = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*,*"), ColumnSpacing = 12 };

        var roamCard = BuildHomeShortcutCard("漫游", "随机推荐", recent.Select(track => track.CoverId), LoadRoamAsync, shortcutCovers);
        Grid.SetColumn(roamCard, 0);
        cards.Children.Add(roamCard);

        var favoritesCard = BuildHomeShortcutCard(
            "收藏",
            "我喜欢的歌曲",
            favorites.Select(track => track.CoverId)
                .Concat(recent.Select(track => track.CoverId))
                .Distinct(),
            LoadFavoritesAsync,
            shortcutCovers);
        Grid.SetColumn(favoritesCard, 1);
        cards.Children.Add(favoritesCard);

        var recentCard = BuildHomeShortcutCard("最近播放", "继续收听", recent.Select(track => track.CoverId), LoadHistoryAsync, shortcutCovers);
        Grid.SetColumn(recentCard, 2);
        cards.Children.Add(recentCard);

        var latestCovers = latest.Select(track => track.CoverId).Concat(albums.Select(album => album.CoverId));
        var latestCard = BuildHomeShortcutCard("最近添加", "最新入库", latestCovers, LoadAllTracksAsync, shortcutCovers);
        Grid.SetColumn(latestCard, 3);
        cards.Children.Add(latestCard);

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
            ContentPanel.Children.Add(cards);
            AddEmptyState("欢迎使用 25DogMusic", "去「音乐库」挑一首歌开始播放吧", Symbol.MusicNote2);
            StatusText.Text = "首页";
            return;
        }

        var tabRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 18, 0, 4)
        };

        var host = new StackPanel();

        // 顶部卡片与 Tab 固定，只有列表区域滚动
        var headerArea = new StackPanel();
        headerArea.Children.Add(cards);
        headerArea.Children.Add(tabRow);

        var listScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Margin = new Thickness(0, 4, 0, 0),
            Content = host
        };

        var page = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        page.Children.Add(headerArea);
        Grid.SetRow(listScroll, 1);
        page.Children.Add(listScroll);

        ContentPanel.Children.Add(page);
        // 首页固定为视口高度并由列表区滚动，避免整页滚动
        _homePage = page;
        ContentScroll.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
        UpdateHomePageHeight();

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

        StatusText.Text = "首页";
    }

    /// <summary>首页高度取视口高度，使顶部固定、仅列表区域滚动。</summary>
    void UpdateHomePageHeight()
    {
        if (_homePage is null) return;
        _homePage.Height = ContentScroll.Bounds.Height;
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

    /// <summary>首页入口卡片：高度固定、宽度随窗口拉伸，封面铺满卡片，文字叠在左下角。</summary>
    Control BuildHomeShortcutCard(string title, string subtitle, IEnumerable<string?> coverIds, Func<Task> onClick, List<(string?, Image)> pending)
    {
        var covers = coverIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().Take(3).ToList();

        // 底层大封面铺满整张卡片
        var baseImage = new Image { Stretch = Stretch.UniformToFill };
        if (covers.Count > 0) pending.Add((covers[0], baseImage));

        var baseLayer = new Grid();
        baseLayer.Children.Add(new Border { Background = CoverPlaceholder });
        baseLayer.Children.Add(baseImage);

        // 右上角两张小封面错位旋转，作为层叠装饰
        var deco = new Canvas
        {
            Width = 116,
            Height = 96,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, -6, -8, 0)
        };
        var layout = new (double Left, double Top, double Size, double Angle, double Opacity)[]
        {
            (0, 14, 56, -14, 0.9),
            (44, 4, 60, 10, 1.0)
        };

        for (var i = 0; i < layout.Length; i++)
        {
            var (left, top, size, angle, opacity) = layout[i];
            var image = new Image { Stretch = Stretch.UniformToFill };
            if (i + 1 < covers.Count) pending.Add((covers[i + 1], image));

            var cover = new Border
            {
                Width = size,
                Height = size,
                CornerRadius = new CornerRadius(8),
                ClipToBounds = true,
                Background = CoverPlaceholder,
                Child = image,
                Opacity = opacity,
                RenderTransformOrigin = RelativePoint.Center,
                RenderTransform = new RotateTransform(angle),
                BoxShadow = BoxShadows.Parse("0 6 16 0 #66000000")
            };
            Canvas.SetLeft(cover, left);
            Canvas.SetTop(cover, top);
            deco.Children.Add(cover);
        }

        // 底部渐隐，保证文字在任何封面上都可读
        var scrim = new Border
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Height = 78,
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0.5, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(0.5, 1, RelativeUnit.Relative),
                GradientStops =
                {
                    new GradientStop(Colors.Transparent, 0),
                    new GradientStop(Color.Parse("#D9000000"), 1)
                }
            }
        };

        var texts = new StackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Bottom,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(14, 0, 14, 11)
        };
        texts.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 15,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brushes.White,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        texts.Children.Add(new TextBlock
        {
            Text = subtitle,
            FontSize = 11.5,
            Foreground = new SolidColorBrush(Colors.White, 0.78),
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        var content = new Grid();
        content.Children.Add(baseLayer);
        content.Children.Add(deco);
        content.Children.Add(scrim);
        content.Children.Add(texts);

        var card = new Border
        {
            Height = 176,
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
            card.RenderTransform = new TranslateTransform(0, -4);
            card.BoxShadow = BoxShadows.Parse("0 12 26 0 #4D000000");
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
            // 首页卡片封面要铺满整张卡片，按较大尺寸解码避免拉伸发糊
            var bitmap = await GetCoverAsync(coverId, 320);
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

}
