using StbImageSharp;

namespace FnMusic.Tui;

/// <summary>
/// 解码后的 RGBA 位图。解码用纯托管的 StbImageSharp，
/// 这里只提供 TUI 需要的「取像素」与「缩放」两种能力。
/// </summary>
public sealed class RgbaImage
{
    public int Width { get; }
    public int Height { get; }

    /// <summary>行优先的 RGBA 字节，长度为 Width * Height * 4。</summary>
    public byte[] Data { get; }

    RgbaImage(int width, int height, byte[] data)
    {
        Width = width;
        Height = height;
        Data = data;
    }

    /// <summary>解码 JPEG/PNG 等封面数据；格式不支持或数据损坏时返回 null。</summary>
    public static RgbaImage? TryDecode(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0) return null;

        try
        {
            var result = ImageResult.FromMemory(bytes, ColorComponents.RedGreenBlueAlpha);
            return result is null ? null : new RgbaImage(result.Width, result.Height, result.Data);
        }
        catch
        {
            return null;
        }
    }

    public RgbaPixel this[int x, int y]
    {
        get
        {
            var index = (y * Width + x) * 4;
            return new RgbaPixel(Data[index], Data[index + 1], Data[index + 2], Data[index + 3]);
        }
    }

    /// <summary>双线性缩放到指定尺寸；终端按字符格显示，不需要更高质量的重采样。</summary>
    public RgbaImage Resize(int width, int height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);

        // 尺寸没变时直接复用当前数据，避免每帧重复采样
        if (width == Width && height == Height) return this;

        var buffer = new byte[width * height * 4];
        var scaleX = (double)Width / width;
        var scaleY = (double)Height / height;

        for (var y = 0; y < height; y++)
        {
            var sourceY = (y + 0.5) * scaleY - 0.5;
            var y0 = Math.Clamp((int)Math.Floor(sourceY), 0, Height - 1);
            var y1 = Math.Clamp(y0 + 1, 0, Height - 1);
            var ty = Math.Clamp(sourceY - y0, 0, 1);

            for (var x = 0; x < width; x++)
            {
                var sourceX = (x + 0.5) * scaleX - 0.5;
                var x0 = Math.Clamp((int)Math.Floor(sourceX), 0, Width - 1);
                var x1 = Math.Clamp(x0 + 1, 0, Width - 1);
                var tx = Math.Clamp(sourceX - x0, 0, 1);

                var topLeft = (y0 * Width + x0) * 4;
                var topRight = (y0 * Width + x1) * 4;
                var bottomLeft = (y1 * Width + x0) * 4;
                var bottomRight = (y1 * Width + x1) * 4;

                var target = (y * width + x) * 4;
                for (var channel = 0; channel < 4; channel++)
                {
                    var top = Data[topLeft + channel] * (1 - tx) + Data[topRight + channel] * tx;
                    var bottom = Data[bottomLeft + channel] * (1 - tx) + Data[bottomRight + channel] * tx;
                    buffer[target + channel] = (byte)Math.Clamp(Math.Round(top * (1 - ty) + bottom * ty), 0, 255);
                }
            }
        }

        return new RgbaImage(width, height, buffer);
    }
}

/// <summary>单个像素，字段顺序与 RGBA 字节一致。</summary>
public readonly record struct RgbaPixel(byte R, byte G, byte B, byte A);
