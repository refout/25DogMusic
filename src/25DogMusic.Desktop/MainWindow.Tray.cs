// 后台播放与托盘、窗口恢复。

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

        // 后台播放期间自动切歌会跳过界面刷新，恢复时补齐全屏页，避免仍停在上一首的信息
        if (NowPlayingOverlay.IsVisible)
        {
            OverlayTitle.Text = _currentTrack.Title;
            OverlaySub.Text = BuildNowSub(_currentTrack);
            _ = RefreshOverlayAsync(_currentTrack);
            if (_lyricsGuid != _currentTrack.Guid) _ = LoadLyricsAsync(_currentTrack.Guid);
        }

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
