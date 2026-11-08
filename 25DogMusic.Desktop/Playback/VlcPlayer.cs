using LibVLCSharp.Shared;

namespace FnMusic.Playback;

/// <summary>LibVLC 无窗口播放：直接播本机回环代理地址。</summary>
public sealed class VlcPlayer : IDisposable
{
    readonly LibVLC _lib;
    readonly MediaPlayer _player;
    Media? _currentMedia;

    public VlcPlayer()
    {
        Core.Initialize();
        _lib = new LibVLC("--no-video", "--network-caching=3000", "--quiet");
        _player = new MediaPlayer(_lib);
        _player.EndReached += (_, _) => EndReached?.Invoke(this, EventArgs.Empty);
    }

    public bool IsPlaying => _player.IsPlaying;

    /// <summary>VLС 自带的均衡器预设名称（用于设置页下拉）。</summary>
    public IReadOnlyList<string> EqualizerPresets
    {
        get
        {
            var result = new List<string>();
            using var eq = new Equalizer();
            var count = (int)eq.PresetCount;
            for (var i = 0; i < count; i++) result.Add(eq.PresetName((uint)i) ?? $"预设 {i}");
            return result;
        }
    }

    /// <summary>应用均衡器预设，index 为 -1 时关闭。</summary>
    public void ApplyEqualizer(int index)
    {
        try
        {
            if (index < 0)
            {
                _player.UnsetEqualizer();
                return;
            }
            var equalizer = new Equalizer((uint)index);
            _player.SetEqualizer(equalizer);
        }
        catch
        {
            // 均衡器不可用时忽略，不影响播放
        }
    }

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

    public void SeekTo(long ms)
    {
        if (_player.Length <= 0) return;
        _player.Time = Math.Clamp(ms, 0, _player.Length - 500);
    }

    public void Dispose()
    {
        try { _player.Stop(); } catch { }
        _currentMedia?.Dispose();
        _player.Dispose();
        _lib.Dispose();
    }
}
