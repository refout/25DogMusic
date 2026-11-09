// 播放控制与全屏播放页 / 歌词。

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
using _25DogMusic.Core.Api;
using _25DogMusic.Core.Config;
using _25DogMusic.Desktop.Playback;
using FluentIcons.Avalonia;
using FluentIcons.Common;

namespace _25DogMusic.Desktop;

public partial class MainWindow : Window
{

    // ---- 播放 ----

    bool IsPlayingTrack(Track track) => _currentTrack?.Guid == track.Guid;

    void PlayFromQueue(IReadOnlyList<Track> queue, int index)
    {
        if (index < 0 || index >= queue.Count) return;
        _roaming = false;
        _roamId = null;
        _roamUpcoming.Clear();
        _roamPrefetchCursor = null;
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
        // 优先用播放器解析出的真实时长：部分整轨 WAV 的实际长度远大于曲库元数据，
        // 用元数据会让进度条提前走满、时钟超出总时长，看起来像「播完了却不开下一首」
        var playerDurationMs = Math.Max(0, _player.Duration);
        var durationMs = playerDurationMs > 0
            ? playerDurationMs
            : _currentTrack?.DurationMs ?? 0;
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

}
