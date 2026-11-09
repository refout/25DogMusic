using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using _25DogMusic.Desktop.Playback;

namespace _25DogMusic.Desktop;

/// <summary>全屏播放页的频谱条：由 SpectrumAnalyzer 提供实时频段能量。</summary>
public sealed class SpectrumView : Control
{
    readonly DispatcherTimer _timer;
    float[] _levels = new float[28];
    SpectrumAnalyzer? _analyzer;

    public SpectrumView()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        _timer.Tick += (_, _) =>
        {
            _levels = _analyzer?.GetBands() ?? new float[28];
            InvalidateVisual();
        };
    }

    public SpectrumAnalyzer? Analyzer
    {
        get => _analyzer;
        set => _analyzer = value;
    }

    public IBrush BarBrush { get; set; } = new SolidColorBrush(Color.Parse("#FF375F"));

    public void SetActive(bool active)
    {
        if (active)
        {
            if (!_timer.IsEnabled) _timer.Start();
        }
        else
        {
            _timer.Stop();
            _levels = new float[_levels.Length];
            InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
        => new Size(double.IsInfinity(availableSize.Width) ? 320 : availableSize.Width, 44);

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0) return;

        var count = _levels.Length;
        var slot = width / count;
        var gap = Math.Max(2.0, slot * 0.28);
        var barWidth = Math.Max(2.0, slot - gap);

        for (var i = 0; i < count; i++)
        {
            var level = Math.Clamp(_levels[i], 0, 1);
            var barHeight = Math.Max(2.0, level * (height - 2));
            var x = i * slot + (slot - barWidth) / 2;
            var rect = new Rect(x, height - barHeight, barWidth, barHeight);

            var alpha = 0.28 + 0.72 * level;
            var brush = BarBrush is SolidColorBrush solid
                ? new SolidColorBrush(solid.Color, alpha)
                : BarBrush;
            context.DrawRectangle(brush, null, new RoundedRect(rect, 2));
        }
    }
}
