using NAudio.Dsp;
using NAudio.Wave;

namespace _25DogMusic.Tui.Playback;

/// <summary>
/// 用 WASAPI 回环采集系统输出，算 RMS 电平与 32 段频谱（FFT），供界面画频谱条。
/// 采集失败时 Available=false。
/// </summary>
public sealed class AudioLevelMonitor : IDisposable
{
    const int FftSize = 2048;
    const int BandCount = 32;

    WasapiRecorder? _capture;
    volatile float _level;

    readonly List<float> _pending = new(FftSize * 2);
    readonly Complex[] _fft = new Complex[FftSize];
    readonly float[] _window = new float[FftSize];
    readonly float[] _bands = new float[BandCount];
    readonly float[] _peaks = new float[BandCount];
    readonly int[] _bandStart = new int[BandCount];
    readonly int[] _bandEnd = new int[BandCount];
    readonly object _gate = new();
    int _configuredRate;

    public bool Available => _capture is not null;
    public float Level => _level;

    public AudioLevelMonitor()
    {
        for (var i = 0; i < FftSize; i++)
        {
            _window[i] = 0.5f - 0.5f * (float)Math.Cos(2 * Math.PI * i / (FftSize - 1));
        }

        try
        {
            // WASAPI 回环只在 Windows 可用，其他平台保持 Available=false
            if (!OperatingSystem.IsWindows()) return;

            _capture = new WasapiRecorderBuilder()
                .WithLoopbackCapture()
                .Build();
            _capture.DataAvailable += OnData;
            _capture.StartRecording();
        }
        catch
        {
            _capture = null;
        }
    }

    /// <summary>拷贝当前频谱（0~1）。</summary>
    public float[] GetBands()
    {
        lock (_gate)
        {
            return (float[])_bands.Clone();
        }
    }

    /// <summary>零拷贝回调：<paramref name="buffer"/> 只在本次回调内有效。</summary>
    void OnData(ReadOnlySpan<byte> buffer, NAudio.CoreAudioApi.AudioClientBufferFlags flags, long devicePosition, long qpcPosition)
    {
        if (!OperatingSystem.IsWindows()) return;

        var format = _capture?.WaveFormat;
        if (format is null || buffer.IsEmpty) return;

        var channels = Math.Max(1, format.Channels);
        var frames = format.BitsPerSample switch
        {
            16 => buffer.Length / (2 * channels),
            32 => buffer.Length / (4 * channels),
            _ => 0
        };

        if (frames <= 0) return;

        for (var frame = 0; frame < frames; frame++)
        {
            double sum = 0;
            for (var ch = 0; ch < channels; ch++)
            {
                var index = (frame * channels + ch) * (format.BitsPerSample / 8);
                sum += format.BitsPerSample == 16
                    ? BitConverter.ToInt16(buffer.Slice(index, 2)) / 32768.0
                    : BitConverter.ToSingle(buffer.Slice(index, 4));
            }

            _pending.Add((float)(sum / channels));
        }

        if (_pending.Count >= FftSize)
        {
            ComputeSpectrum(format.SampleRate);
            _pending.RemoveRange(0, FftSize);
        }
    }

    void ComputeSpectrum(int sampleRate)
    {
        if (_configuredRate != sampleRate)
        {
            ConfigureBands(sampleRate);
            _configuredRate = sampleRate;
        }

        var source = _pending;

        for (var i = 0; i < FftSize; i++)
        {
            _fft[i].X = source[i] * _window[i];
            _fft[i].Y = 0;
        }

        FastFourierTransform.FFT(true, (int)Math.Log2(FftSize), _fft);

        var maxMagnitude = 0f;
        var peak = 1e-6f;

        lock (_gate)
        {
            for (var band = 0; band < BandCount; band++)
            {
                var start = _bandStart[band];
                var end = Math.Max(start + 1, _bandEnd[band]);
                var magnitude = 0f;

                for (var i = start; i < end && i < FftSize / 2; i++)
                {
                    var x = _fft[i].X;
                    var y = _fft[i].Y;
                    var value = (float)Math.Sqrt(x * x + y * y);
                    if (value > magnitude) magnitude = value;
                }

                _peaks[band] = magnitude > _peaks[band] ? magnitude : _peaks[band] * 0.996f;
                var normalized = Math.Clamp(magnitude / _peaks[band], 0, 1);
                var shaped = (float)Math.Pow(normalized, 0.7);

                _bands[band] = _bands[band] * 0.55f + shaped * 0.45f;
                if (_bands[band] > maxMagnitude) maxMagnitude = _bands[band];
                if (_bands[band] > peak) peak = _bands[band];
            }

            _level = Math.Clamp(maxMagnitude * 0.8f + peak * 0.2f, 0, 1);
        }

        if (_level > 0.02f)
        {
            // 峰值缓慢回落，避免频谱条一直顶满
            for (var band = 0; band < BandCount; band++)
            {
                _peaks[band] *= 0.9995f;
            }
        }
    }

    void ConfigureBands(int sampleRate)
    {
        var binHz = sampleRate / (double)FftSize;
        var low = 40.0;
        var high = Math.Min(16000.0, sampleRate / 2.0 - 1);

        for (var band = 0; band < BandCount; band++)
        {
            var f0 = low * Math.Pow(high / low, band / (double)BandCount);
            var f1 = low * Math.Pow(high / low, (band + 1) / (double)BandCount);
            _bandStart[band] = Math.Max(1, (int)(f0 / binHz));
            _bandEnd[band] = Math.Max(_bandStart[band] + 1, (int)(f1 / binHz));
        }
    }

    public void Dispose()
    {
        if (!OperatingSystem.IsWindows() || _capture is null) return;

        try
        {
            _capture.DataAvailable -= OnData;
            _capture.StopRecording();
        }
        catch
        {
            // 设备切换时可能抛异常
        }

        _capture.Dispose();
        _capture = null;
    }
}
