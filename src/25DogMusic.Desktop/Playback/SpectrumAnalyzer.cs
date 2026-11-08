using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace FnMusic.Playback;

/// <summary>
/// 频谱分析：采集系统回环音频（WASAPI loopback），做 FFT 后输出对数的频段能量。
/// 只读采集，不影响 VLC 的播放输出。
/// </summary>
public sealed class SpectrumAnalyzer : IDisposable
{
    const int FftSize = 1024;
    const int BandCount = 28;

    readonly object _lock = new();

    WasapiLoopbackCapture? _capture;
    WaveFormat? _format;
    readonly float[] _ring = new float[FftSize * 2];
    int _write;
    int _filled;

    readonly float[] _levels = new float[BandCount];
    bool _running;

    public bool IsRunning
    {
        get { lock (_lock) return _running; }
    }

    public void Start()
    {
        // WASAPI 系统回环采集仅 Windows 支持，其它平台保持空闲频谱
        if (!OperatingSystem.IsWindows()) return;
        if (_capture is not null) return;

        try
        {
            var capture = new WasapiLoopbackCapture();
            _format = capture.WaveFormat;
            Array.Clear(_ring);
            _write = 0;
            _filled = 0;

            capture.DataAvailable += OnData;
            capture.StartRecording();
            _capture = capture;
            lock (_lock) _running = true;
        }
        catch
        {
            // 没有可用的音频设备/权限时静默失败，界面显示空闲频谱
            _capture = null;
            _format = null;
            lock (_lock) _running = false;
        }
    }

    public void Stop()
    {
        var capture = _capture;
        _capture = null;
        _format = null;
        if (capture is not null)
        {
            try { capture.StopRecording(); } catch { }
            try { capture.Dispose(); } catch { }
        }
        lock (_lock)
        {
            _running = false;
            Array.Clear(_levels);
            Array.Clear(_ring);
            _write = 0;
            _filled = 0;
        }
    }

    void OnData(object? sender, WaveInEventArgs e)
    {
        var format = _format;
        if (format is null || e.Buffer.Length == 0) return;

        var channels = Math.Max(1, format.Channels);
        var bytesAvailable = Math.Min(e.Buffer.Length, e.BytesRecorded);
        var count = 0;
        float[]? mono = null;

        if (format.Encoding == WaveFormatEncoding.IeeeFloat && format.BitsPerSample == 32)
        {
            count = bytesAvailable / 4 / channels;
            if (count <= 0) return;
            mono = new float[count];
            var bytes = e.Buffer;
            for (var i = 0; i < count; i++)
            {
                var sum = 0f;
                for (var c = 0; c < channels; c++)
                {
                    var offset = (i * channels + c) * 4;
                    sum += BitConverter.ToSingle(bytes, offset);
                }
                mono[i] = sum / channels;
            }
        }
        else if (format.Encoding == WaveFormatEncoding.Pcm && format.BitsPerSample == 16)
        {
            count = bytesAvailable / 2 / channels;
            if (count <= 0) return;
            mono = new float[count];
            var bytes = e.Buffer;
            for (var i = 0; i < count; i++)
            {
                var sum = 0f;
                for (var c = 0; c < channels; c++)
                {
                    var offset = (i * channels + c) * 2;
                    sum += BitConverter.ToInt16(bytes, offset) / 32768f;
                }
                mono[i] = sum / channels;
            }
        }

        if (mono is null) return;

        lock (_lock)
        {
            foreach (var sample in mono)
            {
                _ring[_write] = sample;
                _write = (_write + 1) % _ring.Length;
                if (_filled < _ring.Length) _filled++;
            }
        }
    }

    /// <summary>取当前 28 个频段的能量（0~1），带平滑衰减。</summary>
    public float[] GetBands()
    {
        float[] samples;
        WaveFormat? format;
        lock (_lock)
        {
            format = _format;
            if (format is null || _filled < FftSize)
            {
                for (var i = 0; i < BandCount; i++) _levels[i] *= 0.86f;
                return _levels.ToArray();
            }

            samples = new float[FftSize];
            var start = (_write - FftSize + _ring.Length) % _ring.Length;
            for (var i = 0; i < FftSize; i++) samples[i] = _ring[(start + i) % _ring.Length];
        }

        var spectrum = Transform(samples);
        var rate = format.SampleRate;
        var binHz = (double)rate / FftSize;

        var low = 40.0;
        var high = Math.Min(16000.0, rate / 2.0 - 20);
        var ratio = high / low;

        lock (_lock)
        {
            for (var band = 0; band < BandCount; band++)
            {
                var f0 = low * Math.Pow(ratio, (double)band / BandCount);
                var f1 = low * Math.Pow(ratio, (double)(band + 1) / BandCount);
                var bin0 = Math.Max(1, (int)(f0 / binHz));
                var bin1 = Math.Max(bin0 + 1, (int)(f1 / binHz));

                var sum = 0.0;
                var bins = 0;
                for (var bin = bin0; bin < bin1 && bin < spectrum.Length; bin++)
                {
                    sum += spectrum[bin];
                    bins++;
                }

                // 频段内取平均幅度，再按 dB 映射到 0~1：-70dB 以下为静音，-25dB 以上为满格
                var mean = bins > 0 ? (float)(sum / bins) : 0f;
                var db = mean <= 1e-8f ? -100f : 20f * Math.Log10(mean);
                var level = (float)Math.Clamp((db + 70f) / 45f, 0f, 1f);

                var previous = _levels[band];
                _levels[band] = level > previous ? level : previous * 0.86f + level * 0.14f;
            }

            return _levels.ToArray();
        }
    }

    static float[] Transform(float[] input)
    {
        var n = input.Length;
        var re = new double[n];
        var im = new double[n];

        for (var i = 0; i < n; i++)
        {
            // Hann 窗，减少频谱泄漏
            var window = 0.5 - 0.5 * Math.Cos(2 * Math.PI * i / (n - 1));
            re[i] = input[i] * window;
        }

        // 位反转置换
        for (int i = 1, j = 0; i < n; i++)
        {
            var bit = n >> 1;
            for (; j >= bit; bit >>= 1) j -= bit;
            j += bit;
            if (i < j)
            {
                (re[i], re[j]) = (re[j], re[i]);
            }
        }

        for (var length = 2; length <= n; length <<= 1)
        {
            var angle = -2 * Math.PI / length;
            var wRe = Math.Cos(angle);
            var wIm = Math.Sin(angle);

            for (var start = 0; start < n; start += length)
            {
                var curRe = 1.0;
                var curIm = 0.0;
                for (var k = 0; k < length / 2; k++)
                {
                    var even = start + k;
                    var odd = even + length / 2;

                    var tRe = re[odd] * curRe - im[odd] * curIm;
                    var tIm = re[odd] * curIm + im[odd] * curRe;

                    re[odd] = re[even] - tRe;
                    im[odd] = im[even] - tIm;
                    re[even] += tRe;
                    im[even] += tIm;

                    var nextRe = curRe * wRe - curIm * wIm;
                    curIm = curRe * wIm + curIm * wRe;
                    curRe = nextRe;
                }
            }
        }

        var magnitude = new float[n / 2];
        for (var i = 0; i < n / 2; i++)
        {
            magnitude[i] = (float)Math.Sqrt(re[i] * re[i] + im[i] * im[i]) / (n / 2);
        }

        return magnitude;
    }

    public void Dispose() => Stop();
}
