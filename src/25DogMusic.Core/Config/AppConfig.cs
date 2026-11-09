using System.Text.Json;
using System.Text.Json.Serialization;

namespace _25DogMusic.Core.Config;

/// <summary>本地配置：服务器地址、deviceId、登录令牌（不保存明文密码）。</summary>
public sealed class AppConfig
{
    public string BaseUrl { get; set; } = "";
    public string DeviceId { get; set; } = string.Empty;
    public string? Token { get; set; }
    public string? LastUsername { get; set; }
    public int Volume { get; set; } = 70;
    public string BackgroundMode { get; set; } = "cover";

    /// <summary>自定义背景色（BackgroundMode = custom 时生效）。</summary>
    public string BackgroundCustomColor { get; set; } = "#2B1B33";
    public string Material { get; set; } = "off";
    public double UiOpacity { get; set; } = 0.75;
    public string Theme { get; set; } = "dark";
    public bool ShowAudioQuality { get; set; } = true;
    public bool BackgroundPlayback { get; set; } = true;
    public bool StartShuffle { get; set; }
    public int StartRepeat { get; set; }

    /// <summary>均衡器预设索引，-1 表示关闭。</summary>
    public int EqPresetIndex { get; set; } = -1;
    public bool DesktopLyrics { get; set; }
    public bool SpectrumEnabled { get; set; } = true;

    /// <summary>侧边栏是否折叠为仅图标模式。</summary>
    public bool SidebarCollapsed { get; set; }

    static string ConfigDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "25DogMusic");

    /// <summary>旧版本使用的配置目录，首次运行时迁移一次。</summary>
    static string LegacyConfigFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FnMusic", "config.json");

    static string ConfigFile => Path.Combine(ConfigDir, "config.json");

    public static AppConfig Load()
    {
        try
        {
            MigrateLegacyConfig();
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

    /// <summary>把旧版 FnMusic 目录下的配置复制到新目录，失败时保留旧文件不动。</summary>
    static void MigrateLegacyConfig()
    {
        if (File.Exists(ConfigFile) || !File.Exists(LegacyConfigFile)) return;

        try
        {
            Directory.CreateDirectory(ConfigDir);
            File.Copy(LegacyConfigFile, ConfigFile);
        }
        catch
        {
            // 迁移失败则按未登录状态处理
        }
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
