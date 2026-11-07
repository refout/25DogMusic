using System.Text.Json;
using System.Text.Json.Serialization;

namespace FnMusic.Config;

/// <summary>本地配置：服务器地址、deviceId、登录令牌（不保存明文密码）。</summary>
public sealed class AppConfig
{
    public string BaseUrl { get; set; } = "https://10.0.0.10";
    public string DeviceId { get; set; } = string.Empty;
    public string? Token { get; set; }
    public string? LastUsername { get; set; }
    public int Volume { get; set; } = 70;
    public string BackgroundMode { get; set; } = "cover";
    public string Material { get; set; } = "off";
    public double UiOpacity { get; set; } = 0.75;
    public string Theme { get; set; } = "dark";
    public bool ShowAudioQuality { get; set; } = true;
    public bool BackgroundPlayback { get; set; } = true;
    public bool StartShuffle { get; set; }
    public int StartRepeat { get; set; }

    static string ConfigDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FnMusic");

    static string ConfigFile => Path.Combine(ConfigDir, "config.json");

    public static AppConfig Load()
    {
        try
        {
            if (File.Exists(ConfigFile))
            {
                var json = File.ReadAllText(ConfigFile);
                return JsonSerializer.Deserialize(json, ConfigJsonContext.Default.AppConfig) ?? new AppConfig();
            }
        }
        catch
        {
            // 配置损坏时回落到默认值
        }

        return new AppConfig();
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(ConfigFile, JsonSerializer.Serialize(this, ConfigJsonContext.Default.AppConfig));
    }

    public static string FilePath => ConfigFile;
}

/// <summary>AOT 兼容的配置序列化元数据。</summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AppConfig))]
internal partial class ConfigJsonContext : JsonSerializerContext
{
}
