using System.Collections.ObjectModel;
using System.Text.Json;
using Terminal.Gui;
using FnMusic.Api;
using FnMusic.Config;
using FnMusic.Playback;

namespace FnMusic.Tui;

/// <summary>终端版主窗口：浏览、搜索、播放与歌词集中在同一个终端界面。</summary>
public sealed class PlayerWindow : Window
{
    readonly AppConfig _cfg;
    readonly MusicApiClient _api;
    readonly VlcPlayer _player;
    readonly StreamProxy _proxy;
    readonly AudioLevelMonitor _levelMonitor;

    readonly FrameView _loginFrame;
    readonly TextField _userField;
    readonly TextField _passField;
    readonly Label _loginHint;

    readonly MusicListView _nav;
    readonly MusicListView _list;
    readonly TextField _searchField;
    readonly Label _status;
    readonly HeaderView _header;
    readonly CoverView _cover;
    readonly SpectrumView _spectrum;
    readonly LyricsView _lyrics;
    readonly TransparentView _content;

    /// <summary>四边统一内边距（字符列/行）。</summary>
    const int EdgePadding = 1;

    readonly List<string> _navItems = new() { "全部歌曲", "专辑", "歌手", "流派", "历史" };

    enum BrowseMode { Tracks, Albums, Artists, Genres, History }

    BrowseMode _mode = BrowseMode.Tracks;
    BrowseMode _parentMode = BrowseMode.Tracks;
    bool _inDetail;
    bool _advance;
    bool _focusReady;
    bool _shuffle;
    int _repeat;              // 0 关闭 / 1 列表循环 / 2 单曲循环
    int _page = 1;
    string _sort = "title,asc";
    bool _roamMode;
    string? _roamId;
    ColorTheme _theme = ColorTheme.Default;
    List<Track> _tracks = new();
    List<Album> _albums = new();
    List<Artist> _artists = new();
    List<Genre> _genres = new();
    int _volume = 100;

    public PlayerWindow(AppConfig cfg, MusicApiClient api)
    {
        _cfg = cfg;
        _api = api;
        _player = new VlcPlayer();
        _proxy = new StreamProxy(api);
        _proxy.Start();
        _levelMonitor = new AudioLevelMonitor();

        // 顶部标题栏留空，避免边框上出现“飞牛音乐”
        Title = string.Empty;
        Border!.LineStyle = LineStyle.None;

        _loginFrame = new FrameView
        {
            Title = "登录飞牛音乐",
            X = 1,
            Y = 0,
            Width = Dim.Percent(70),
            Height = 7
        };

        _userField = new TextField
        {
            X = 2,
            Y = 0,
            Width = 36,
            Text = cfg.LastUsername ?? string.Empty
        };

        _passField = new TextField
        {
            X = 2,
            Y = 1,
            Width = 36,
            Secret = true
        };

        _loginHint = new Label
        {
            X = 2,
            Y = 3,
            Width = Dim.Fill(),
            Text = "Enter 账号登录   F2 浏览器授权登录"
        };

        _loginFrame.Add(
            new Label { X = 0, Y = 0, Text = "用户名" },
            _userField,
            new Label { X = 0, Y = 1, Text = "密码  " },
            _passField,
            _loginHint);

        _header = new HeaderView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 6
        };
        _header.SetUser(cfg.LastUsername ?? string.Empty, new Uri(cfg.BaseUrl).Host);

        _nav = new MusicListView
        {
            X = 0,
            Y = 7,
            Width = 14,
            Height = 5
        };
        _nav.SetItems(_navItems);
        _nav.OpenSelected += EnterNav;

        _list = new MusicListView
        {
            X = 15,
            Y = 7,
            Width = Dim.Percent(52),
            Height = Dim.Fill(3)
        };
        _list.OpenSelected += ActivateListItem;

        // 封面不再上屏，仅作为主题色与壁纸的来源
        _cover = new CoverView();

        _spectrum = new SpectrumView
        {
            X = Pos.Right(_list) + 1,
            Y = 7,
            Width = Dim.Fill(),
            Height = 4
        };

        _lyrics = new LyricsView
        {
            X = Pos.Right(_list) + 1,
            Y = Pos.Bottom(_spectrum),
            Width = Dim.Fill(),
            Height = Dim.Fill(3)
        };

        _searchField = new TextField
        {
            X = 8,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(10),
            Visible = false
        };
        _searchField.KeyDown += (_, k) =>
        {
            if (k.KeyCode != KeyCode.Enter) return;
            Search(_searchField.Text?.ToString() ?? string.Empty);
            _searchField.Visible = false;
            _list.SetFocus();
        };

        _status = new Label
        {
            X = 0,
            Y = Pos.AnchorEnd(),
            Width = Dim.Fill(),
            Text = "就绪"
        };

        _content = new TransparentView
        {
            X = EdgePadding,
            Y = EdgePadding,
            Width = Dim.Fill(EdgePadding * 2),
            Height = Dim.Fill(EdgePadding * 2)
        };
        _content.Add(_header, _loginFrame, _nav, _list, _spectrum, _lyrics, _searchField, _status);
        Add(_content);
        ApplyTheme(ColorTheme.Default);

        if (cfg.Token is null)
        {
            _userField.SetFocus();
        }
        else
        {
            _loginFrame.Visible = false;
            LoadAllTracks();
            _list.SetFocus();
        }

        Application.AddTimeout(TimeSpan.FromMilliseconds(500), Tick);
        Application.AddTimeout(TimeSpan.FromMilliseconds(80), LevelTick);
        _player.EndReached += (_, _) => _advance = true;
    }

    bool LevelTick()
    {
        if (_player.IsPlaying && _levelMonitor.Available)
        {
            _spectrum.SetBands(_levelMonitor.GetBands());
        }
        else
        {
            _spectrum.SetBands(new float[32]);
        }

        return true;
    }

    bool Tick()
    {
        // 首帧后确保焦点落在列表上（布局完成前 SetFocus 可能无效）
        if (!_focusReady)
        {
            _focusReady = true;
            if (!_loginFrame.Visible) _list.SetFocus();
        }

        if (_advance)
        {
            _advance = false;
            PlayNext();
        }

        if (_player.IsPlaying)
        {
            _lyrics.UpdatePosition(TimeSpan.FromMilliseconds(_player.Position));
        }

        UpdateStatus();
        return true;
    }

    protected override bool OnKeyDown(Key key)
    {
        if (_loginFrame.Visible)
        {
            switch (key.KeyCode)
            {
                case KeyCode.Enter:
                    PasswordLogin();
                    return true;
                case KeyCode.F2:
                    OAuthLogin();
                    return true;
                case KeyCode.Esc:
                    Application.RequestStop();
                    return true;
            }

            return base.OnKeyDown(key);
        }

        switch (key.KeyCode)
        {
            case KeyCode.Space:
                _player.TogglePause();
                UpdateStatus();
                return true;
            case KeyCode.CursorLeft:
                _player.SeekRelative(-5000);
                return true;
            case KeyCode.CursorRight:
                _player.SeekRelative(5000);
                return true;
            case KeyCode.F5:
                LoadAllTracks();
                return true;
            case (KeyCode)'/':
                _searchField.Visible = true;
                _searchField.SetFocus();
                return true;
            case KeyCode.Tab:
                if (_list.HasFocus) _nav.SetFocus();
                else _list.SetFocus();
                return true;
            case (KeyCode)'n':
                LoadNextPage();
                return true;
            case (KeyCode)'o':
                CycleSort();
                return true;
            case (KeyCode)'s':
                _shuffle = !_shuffle;
                UpdateStatus();
                return true;
            case (KeyCode)'r':
                _repeat = (_repeat + 1) % 3;
                UpdateStatus();
                return true;
            case (KeyCode)'f':
                if (_roamMode)
                {
                    _roamMode = false;
                    _status.Text = "已退出漫游";
                }
                else
                {
                    StartRoam();
                }
                return true;
            case KeyCode.Esc:
                if (_inDetail)
                {
                    _inDetail = false;
                    _mode = _parentMode;
                    if (_parentMode == BrowseMode.Albums) LoadAlbums();
                    else if (_parentMode == BrowseMode.Artists) LoadArtists();
                    else if (_parentMode == BrowseMode.Genres) LoadGenres();
                    else LoadAllTracks();
                    _nav.SetFocus();
                }
                else
                {
                    Application.RequestStop();
                }
                return true;
            case (KeyCode)'-':
                _volume = Math.Max(0, _volume - 10);
                _player.Volume = _volume;
                UpdateStatus();
                return true;
            case (KeyCode)'+':
                _volume = Math.Min(150, _volume + 10);
                _player.Volume = _volume;
                UpdateStatus();
                return true;
        }

        return base.OnKeyDown(key);
    }

    void PasswordLogin()
    {
        var user = _userField.Text?.ToString()?.Trim() ?? string.Empty;
        var pass = _passField.Text?.ToString() ?? string.Empty;

        try
        {
            _loginHint.Text = "登录中…";

            var token = Task.Run(() => _api.LoginWithPasswordAsync(user, pass)).GetAwaiter().GetResult();
            _cfg.Token = token;
            _cfg.LastUsername = user;
            _cfg.Save();

            _loginFrame.Visible = false;
            LoadAllTracks();
            _list.SetFocus();
        }
        catch (Exception ex)
        {
            _loginHint.Text = $"登录失败: {ex.Message}";
        }
    }

    void OAuthLogin()
    {
        try
        {
            var url = Task.Run(() => _api.BuildOAuthUrlAsync()).GetAwaiter().GetResult();
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });

            _loginHint.Text = "浏览器登录后，把回跳地址粘贴到这里…";

            var dialog = new Dialog
            {
                Title = "粘贴授权地址",
                Width = 80,
                Height = 8
            };
            var field = new TextField { X = 1, Y = 1, Width = Dim.Fill(2) };
            dialog.Add(new Label { X = 1, Y = 0, Text = "code 或 .../oauth/result?code=xxxx" }, field);
            _searchField.Visible = false;

            // 使用输入框回车提交
            field.KeyDown += (_, k) =>
            {
                if (k.KeyCode != KeyCode.Enter) return;

                var code = ExtractCode(field.Text?.ToString() ?? string.Empty);
                try
                {
                    var token = Task.Run(() => _api.LoginWithOAuthCodeAsync(code)).GetAwaiter().GetResult();
                    _cfg.Token = token;
                    _cfg.Save();
                    _loginFrame.Visible = false;
                    dialog.RequestStop();
                    LoadAllTracks();
                    _list.SetFocus();
                }
                catch (Exception ex)
                {
                    _loginHint.Text = $"授权登录失败: {ex.Message}";
                }
            };

            Application.Run(dialog);
        }
        catch (Exception ex)
        {
            _loginHint.Text = $"发起授权失败: {ex.Message}";
        }
    }

    static string ExtractCode(string input)
    {
        if (!input.Contains('=')) return input.Trim();
        var index = input.IndexOf("code=", StringComparison.Ordinal);
        if (index < 0) return input.Trim();
        var code = input[(index + 5)..];
        var end = code.IndexOf('&');
        if (end >= 0) code = code[..end];
        return Uri.UnescapeDataString(code);
    }

    void EnterNav()
    {
        _inDetail = false;

        switch (_nav.Selected)
        {
            case 0:
                _mode = BrowseMode.Tracks;
                LoadAllTracks();
                break;
            case 1:
                _mode = BrowseMode.Albums;
                LoadAlbums();
                break;
            case 2:
                _mode = BrowseMode.Artists;
                LoadArtists();
                break;
            case 3:
                _mode = BrowseMode.Genres;
                LoadGenres();
                break;
            case 4:
                _mode = BrowseMode.History;
                LoadHistory();
                break;
        }

        _list.SetFocus();
    }

    void ActivateListItem()
    {
        var index = _list.Selected;

        if (_mode == BrowseMode.Albums)
        {
            if (index < 0 || index >= _albums.Count) return;
            _parentMode = BrowseMode.Albums;
            _inDetail = true;
            LoadTracksByAlbum(_albums[index]);
            return;
        }

        if (_mode == BrowseMode.Artists)
        {
            if (index < 0 || index >= _artists.Count) return;
            _parentMode = BrowseMode.Artists;
            _inDetail = true;
            LoadTracksByArtist(_artists[index]);
            return;
        }

        if (_mode == BrowseMode.Genres)
        {
            if (index < 0 || index >= _genres.Count) return;
            _parentMode = BrowseMode.Genres;
            _inDetail = true;
            LoadTracksByGenre(_genres[index]);
            return;
        }

        PlaySelected();
    }

    void LoadAlbums()
    {
        try
        {
            var doc = Task.Run(() => _api.GetAlbumsAsync(1, 200)).GetAwaiter().GetResult();
            using (doc)
            {
                _albums = Library.ParseAlbums(doc);
            }

            Refresh();
            _status.Text = $"专辑 {_albums.Count} 张，Enter 展开，Esc 返回";

            if (_albums.Count > 0) ShowCover(_albums[0].CoverId);
        }
        catch (Exception ex)
        {
            _status.Text = $"加载专辑失败: {ex.Message}";
        }
    }

    void LoadArtists()
    {
        try
        {
            var doc = Task.Run(() => _api.GetArtistsAsync(1, 200)).GetAwaiter().GetResult();
            using (doc)
            {
                _artists = Library.ParseArtists(doc);
            }

            Refresh();
            _status.Text = $"歌手 {_artists.Count} 位，Enter 展开，Esc 返回";

            if (_artists.Count > 0) ShowCover(_artists[0].CoverId);
        }
        catch (Exception ex)
        {
            _status.Text = $"加载歌手失败: {ex.Message}";
        }
    }

    void LoadTracksByAlbum(Album album)
    {
        try
        {
            var doc = Task.Run(() => _api.GetTracksByAlbumAsync(album.Guid)).GetAwaiter().GetResult();
            using (doc)
            {
                _tracks = Library.ParseTracks(doc);
            }

            _mode = BrowseMode.Tracks;
            Refresh();
            ShowCover(album.CoverId);
            _lyrics.SetLyrics(new List<LyricLine>());
            _status.Text = $"{album.Name} · {_tracks.Count} 首，Enter 播放，Esc 返回专辑";
        }
        catch (Exception ex)
        {
            _status.Text = $"加载专辑曲目失败: {ex.Message}";
        }
    }

    void LoadTracksByArtist(Artist artist)
    {
        try
        {
            var doc = Task.Run(() => _api.GetTracksByArtistAsync(artist.Guid)).GetAwaiter().GetResult();
            using (doc)
            {
                _tracks = Library.ParseTracks(doc);
            }

            _mode = BrowseMode.Tracks;
            Refresh();
            ShowCover(artist.CoverId);
            _lyrics.SetLyrics(new List<LyricLine>());
            _status.Text = $"{artist.Name} · {_tracks.Count} 首，Enter 播放，Esc 返回歌手";
        }
        catch (Exception ex)
        {
            _status.Text = $"加载歌手曲目失败: {ex.Message}";
        }
    }

    void LoadGenres()
    {
        try
        {
            using var doc = Task.Run(() => _api.GetGenresAsync(1, 200)).GetAwaiter().GetResult();
            _genres = Library.ParseGenres(doc);
            Refresh();
            _status.Text = $"流派 {_genres.Count} 个，Enter 展开，Esc 返回";
        }
        catch (Exception ex)
        {
            HandleError("加载流派失败", ex);
        }
    }

    void LoadHistory()
    {
        try
        {
            using var doc = Task.Run(() => _api.GetPlayHistoryAsync(1, 200)).GetAwaiter().GetResult();
            _tracks = Library.ParseTracks(doc);
            _page = 1;
            _mode = BrowseMode.History;
            Refresh();
            _status.Text = $"播放历史 {_tracks.Count} 首，Enter 重播，N 下一页";
        }
        catch (Exception ex)
        {
            HandleError("加载播放历史失败", ex);
        }
    }

    void LoadTracksByGenre(Genre genre)
    {
        try
        {
            using var doc = Task.Run(() => _api.GetTracksByGenreAsync(genre.Guid)).GetAwaiter().GetResult();
            _tracks = Library.ParseTracks(doc);
            _mode = BrowseMode.Tracks;
            _page = 1;
            Refresh();
            _lyrics.SetLyrics(new List<LyricLine>());
            _status.Text = $"{genre.Name} · {_tracks.Count} 首，Enter 播放，Esc 返回流派";
        }
        catch (Exception ex)
        {
            HandleError("加载流派曲目失败", ex);
        }
    }

    /// <summary>下一页（追加到当前列表）。</summary>
    void LoadNextPage()
    {
        var next = _page + 1;

        try
        {
            JsonDocument doc = _mode switch
            {
                BrowseMode.History => Task.Run(() => _api.GetPlayHistoryAsync(next, 200)).GetAwaiter().GetResult(),
                _ => Task.Run(() => _api.GetTracksAsync(next, 200, _sort)).GetAwaiter().GetResult()
            };

            using (doc)
            {
                var more = Library.ParseTracks(doc);
                if (more.Count == 0)
                {
                    _status.Text = "没有更多了";
                    return;
                }

                _tracks.AddRange(more);
                _page = next;
                var selected = _list.Selected;
                _list.SetItems(_tracks.Select(t => $"{t.Title}  —  {t.Artist}"), selected);
                _status.Text = $"已加载到第 {_page} 页，共 {_tracks.Count} 首";
            }
        }
        catch (Exception ex)
        {
            HandleError("翻页失败", ex);
        }
    }

    void CycleSort()
    {
        _sort = _sort switch
        {
            "title,asc" => "title,desc",
            "title,desc" => "createdAt,desc",
            _ => "title,asc"
        };

        LoadAllTracks();
        _status.Text = $"排序：{_sort}";
    }

    void StartRoam()
    {
        try
        {
            using var doc = Task.Run(() => _api.RoamStartAsync()).GetAwaiter().GetResult();
            var result = Library.ParseRoam(doc);
            if (result is null) { _status.Text = "漫游没有返回结果"; return; }

            _roamMode = true;
            _roamId = result.Value.RoamId;
            _tracks = new List<Track> { result.Value.Track };
            _mode = BrowseMode.Tracks;
            Refresh();
            PlaySelected();
            _status.Text = "随机漫游中 · F 退出漫游";
        }
        catch (Exception ex)
        {
            HandleError("启动漫游失败", ex);
        }
    }

    void RoamAdvance()
    {
        if (_roamId is null) return;

        try
        {
            using var doc = Task.Run(() => _api.RoamNextAsync(_roamId)).GetAwaiter().GetResult();
            var result = Library.ParseRoam(doc);
            if (result is null) return;

            _roamId = result.Value.RoamId;
            _tracks = new List<Track> { result.Value.Track };
            Refresh();
            PlaySelected();
        }
        catch (Exception ex)
        {
            _status.Text = $"漫游失败: {ex.Message}";
        }
    }

    void HandleError(string title, Exception ex)
    {
        if (ex is FnMusic.Api.MusicApiException { Code: 99999 })
        {
            RequireLogin();
            _status.Text = $"{title}: 登录已失效，请重新登录";
            return;
        }

        _status.Text = $"{title}: {ex.Message}";
    }

    void RequireLogin()
    {
        _roamMode = false;
        _cfg.Token = null;
        _cfg.Save();
        _api.ClearToken();
        _loginFrame.Visible = true;
        _userField.SetFocus();
    }

    void ShowCover(string coverId)
    {
        ColorTheme theme;
        try
        {
            var bytes = string.IsNullOrEmpty(coverId)
                ? null
                : Task.Run(() => _api.GetCoverAsync(coverId, 512)).GetAwaiter().GetResult();
            theme = _cover.SetJpeg(bytes);
        }
        catch
        {
            theme = _cover.SetJpeg(null);
        }

        ApplyTheme(theme);
    }

    void ApplyTheme(ColorTheme theme)
    {
        _theme = theme;
        ThemeProvider.Current = theme;

        // 壁纸按当前窗口尺寸铺满整屏
        Wallpaper.Shared.Update(_cover.Image, Viewport.Width, Viewport.Height, theme);

        var scheme = theme.BuildScheme();
        ColorScheme = scheme;
        _loginFrame.ColorScheme = scheme;
        _status.ColorScheme = scheme;
        SetNeedsDraw();
        _cover.SetNeedsDraw();
    }

    /// <summary>整窗背景：模糊压暗后的封面壁纸。</summary>
    protected override bool OnDrawingContent(DrawContext? context)
    {
        Wallpaper.Shared.Update(_cover.Image, Viewport.Width, Viewport.Height, _theme);

        var driver = Application.Driver!;
        for (var row = 0; row < Viewport.Height; row++)
        {
            var color = Wallpaper.Shared.Cell(Viewport.X, Viewport.Y + row);
            driver.SetAttribute(new Terminal.Gui.Attribute(color, color));
            driver.Move(Viewport.X, Viewport.Y + row);
            driver.AddStr(new string(' ', Viewport.Width));
        }

        return true;
    }

    void PlayNext()
    {
        if (_roamMode)
        {
            RoamAdvance();
            return;
        }

        if (_tracks.Count == 0) return;

        if (_repeat == 2)
        {
            PlaySelected();
            return;
        }

        var next = _shuffle ? Random.Shared.Next(_tracks.Count) : _list.Selected + 1;
        if (next >= _tracks.Count)
        {
            if (_repeat == 0) return;   // 顺序播放到底就停
            next = 0;
        }

        _list.Selected = next;
        PlaySelected();
    }

    void LoadAllTracks()
    {
        _mode = BrowseMode.Tracks;
        _page = 1;

        try
        {
            var doc = Task.Run(() => _api.GetTracksAsync(1, 200, _sort)).GetAwaiter().GetResult();
            using (doc)
            {
                _tracks = Library.ParseTracks(doc);
            }

            Refresh();
            UpdateStatus();

            if (_tracks.Count > 0)
            {
                ShowCover(_tracks[0].CoverId);
                _header.SetTrack(_tracks[0]);
            }
        }
        catch (Exception ex)
        {
            HandleError("加载失败", ex);
        }
    }

    void Search(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            LoadAllTracks();
            return;
        }

        try
        {
            var doc = Task.Run(() => _api.SearchTracksAsync(keyword)).GetAwaiter().GetResult();
            using (doc)
            {
                _tracks = Library.ParseTracks(doc);
            }

            Refresh();
            _status.Text = $"搜索“{keyword}”命中 {_tracks.Count} 首";

            if (_tracks.Count > 0)
            {
                ShowCover(_tracks[0].CoverId);
                _header.SetTrack(_tracks[0]);
            }
        }
        catch (Exception ex)
        {
            _status.Text = $"搜索失败: {ex.Message}";
        }
    }

    void Refresh()
    {
        var items = _mode switch
        {
            BrowseMode.Albums => _albums
                .Select(a => $"{a.Name}  —  {a.Artist}  ({a.TrackCount} 首)")
                .ToList(),
            BrowseMode.Artists => _artists
                .Select(a => $"{a.Name}  ({a.AlbumCount} 张专辑 / {a.TrackCount} 首)")
                .ToList(),
            BrowseMode.Genres => _genres
                .Select(g => $"{g.Name}  ({g.TrackCount} 首)")
                .ToList(),
            _ => _tracks
                .Select(t => $"{t.Title}  —  {t.Artist}")
                .ToList()
        };

        _list.SetItems(items);
    }

    void PlaySelected()
    {
        var index = _list.Selected;
        if (index < 0 || index >= _tracks.Count) return;

        var track = _tracks[index];
        _player.Play(_proxy.BaseUrl + track.Guid);
        _header.SetTrack(track);
        LoadDetails(track);
        UpdateStatus();

        // 播放历史上报：失败不影响播放
        _ = Task.Run(async () =>
        {
            try { await _api.ReportTrackPlayAsync(track.Guid); }
            catch { /* 服务端写接口未放行时静默 */ }
        });
    }

    void LoadDetails(Track track)
    {
        _lyrics.SetLyrics(new List<LyricLine>());
        ShowCover(track.CoverId);

        try
        {
            _lyrics.SetLyrics(Task.Run(() => _api.GetLyricsAsync(track.Guid)).GetAwaiter().GetResult());
            _lyrics.UpdatePosition(TimeSpan.FromMilliseconds(_player.Position));
        }
        catch
        {
            // 无歌词时留空
        }
    }

    void UpdateStatus()
    {
        var state = _player.IsPlaying ? "▶" : "⏸";
        var position = TimeSpan.FromMilliseconds(Math.Max(0, _player.Position));
        var duration = TimeSpan.FromMilliseconds(Math.Max(0, _player.Duration));
        var title = _tracks.Count > 0 && _list.Selected >= 0 && _list.Selected < _tracks.Count
            ? _tracks[_list.Selected].Title
            : "未选择";

        var repeatLabel = _repeat switch { 1 => "列表循环", 2 => "单曲循环", _ => "顺序" };
        var shuffleLabel = _shuffle ? "随机" : "不随机";

        _status.Text = $"{state} {title}  {position:mm\\:ss}/{duration:mm\\:ss}  音量 {_volume}  {shuffleLabel}/{repeatLabel}  Enter 播放  Space 暂停  S 随机  R 循环  N 翻页  O 排序  F 漫游  / 搜索";
        _header.SetProgress(position, duration, _player.IsPlaying);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _player.Dispose();
            _proxy.Dispose();
            _levelMonitor.Dispose();
            _cover.Dispose();
            _api.Dispose();
        }

        base.Dispose(disposing);
    }
}



