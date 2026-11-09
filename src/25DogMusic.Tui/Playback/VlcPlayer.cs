using LibVLCSharp.Shared;

namespace _25DogMusic.Tui.Playback;

/// <summary>LibVLC 无窗口播放：直接播本机回环代理地址。</summary>
public sealed class VlcPlayer : IDisposable
{
    readonly LibVLC _lib;
    readonly MediaPlayer _player;
    Media? _currentMedia;

    public VlcPlayer()
    {
        LibVLCSharp.Shared.Core.Initialize();
        _lib = new LibVLC("--no-video", "--network-caching=3000", "--quiet");
        _player = new MediaPlayer(_lib);
        _player.EndReached += (_, _) => EndReached?.Invoke(this, EventArgs.Empty);
    }

    public bool IsPlaying => _player.IsPlaying;

    /// <summary>一曲播完（在 VLC 线程触发，界面侧只置标志位）。</summary>
    public event EventHandler? EndReached;
    public long Position => _player.Time;
    public long Duration => _player.Length;
    public int Volume
    {
        get => _player.Volume;
        set => _player.Volume = Math.Clamp(value, 0, 150);
    }

    public void Play(string url, bool autoplay = true)
    {
        _currentMedia?.Dispose();
        _currentMedia = new Media(_lib, url, FromType.FromLocation);
        if (autoplay) _player.Play(_currentMedia);
    }

    public void TogglePause()
    {
        if (_player.IsPlaying) _player.Pause();
        else _player.Play();
    }

    public void SeekRelative(long deltaMs)
    {
        var target = _player.Time + deltaMs;
        if (target < 0) target = 0;
        if (_player.Length > 0 && target > _player.Length) target = _player.Length - 1000;
        _player.Time = target;
    }

    public void Dispose()
    {
        try { _player.Stop(); } catch { }
        _currentMedia?.Dispose();
        _player.Dispose();
        _lib.Dispose();
    }
}
