// 设置页：账号与连接、外观、音质与播放、关于。

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

    // ---- 登录 / 客户端 ----

    int _settingsTab;

    bool _sidebarCollapsed;

    // 窗口宽度低于该值时自动折叠侧栏，变宽后恢复用户手动设置
    const double AutoCollapseSidebarWidth = 900;
    bool _autoSidebarCollapsed;

    /// <summary>折叠侧栏：只保留图标，歌单区与搜索框隐藏，状态写入配置。</summary>
    void SetSidebarCollapsed(bool collapsed, bool save = true)
    {
        _sidebarCollapsed = collapsed;

        SidebarRoot.ColumnDefinitions = new ColumnDefinitions(collapsed ? "72,*" : "248,*");
        SearchPane.IsVisible = !collapsed;
        SidebarPane.Classes.Set("collapsed", collapsed);
        SidebarToggleIcon.Symbol = collapsed ? Symbol.ChevronRight : Symbol.ChevronLeft;
        ToolTip.SetTip(SidebarToggle, collapsed ? "展开侧栏" : "折叠侧栏");

        if (!save) return;
        _config.SidebarCollapsed = collapsed;
        _config.Save();
    }

    /// <summary>窄窗口自动折叠侧栏；不写入配置，窗口变宽后回到用户偏好。</summary>
    void UpdateSidebarForWidth(double width)
    {
        var collapse = width < AutoCollapseSidebarWidth;
        if (collapse == _autoSidebarCollapsed) return;

        _autoSidebarCollapsed = collapse;
        SetSidebarCollapsed(collapse || _config.SidebarCollapsed, save: false);
    }

    void ShowSettingsPage(string? message = null)
    {
        _mode = Mode.Settings;
        SetActiveNav(NavSettings);
        BeginPage("设置", "外观、音质与播放");
        EnsureDeviceId();

        var tabs = new TabControl
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 8, 0, 0)
        };

        // 使用 Fluent 主题默认的 TabItem 外观，仅提供标题文本；
        // 每个 Tab 的内容各自滚动，页面外层不滚动，宽度也不再固定
        tabs.Items.Add(new TabItem { Header = "账号与连接", Content = BuildSettingsTabScroll(BuildAccountCard(message)) });
        tabs.Items.Add(new TabItem { Header = "外观", Content = BuildSettingsTabScroll(BuildAppearanceCard()) });
        tabs.Items.Add(new TabItem { Header = "音质与播放", Content = BuildSettingsTabScroll(BuildPlaybackCard()) });
        tabs.Items.Add(new TabItem { Header = "关于", Content = BuildSettingsTabScroll(BuildAboutCard()) });

        tabs.SelectedIndex = Math.Clamp(_settingsTab, 0, tabs.ItemCount - 1);
        tabs.SelectionChanged += (_, _) =>
        {
            if (tabs.SelectedIndex >= 0) _settingsTab = tabs.SelectedIndex;
            AnimateSettingsTab(tabs.SelectedItem as TabItem);
        };

        ContentPanel.Children.Add(tabs);
        _settingsTabs = tabs;
        ContentScroll.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
        UpdateSettingsTabHeight();

        // 首次进入也播放一次淡入过渡
        AnimateSettingsTab(tabs.SelectedItem as TabItem);
    }

    /// <summary>设置页 Tab 内容外层：只在内容区域滚动，内容始终跟随可用宽度。</summary>
    Control BuildSettingsTabScroll(Control content) => new ScrollViewer
    {
        VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
        HorizontalContentAlignment = HorizontalAlignment.Stretch,
        Margin = new Thickness(0, 8, 0, 0),
        Content = content
    };

    /// <summary>TabControl 取视口高度，使标题栏固定、只有内容区滚动。</summary>
    void UpdateSettingsTabHeight()
    {
        if (_settingsTabs is null) return;
        _settingsTabs.Height = Math.Max(240, ContentScroll.Bounds.Height - 8);
    }

    /// <summary>设置页切换 Tab 时内容淡入并轻微上移。</summary>
    void AnimateSettingsTab(TabItem? item)
    {
        if (item?.Content is not Control content) return;

        content.Transitions = new Transitions
        {
            new DoubleTransition
            {
                Property = OpacityProperty,
                Duration = TimeSpan.FromMilliseconds(220),
                Easing = new CubicEaseOut()
            },
            new TransformOperationsTransition
            {
                Property = RenderTransformProperty,
                Duration = TimeSpan.FromMilliseconds(220),
                Easing = new CubicEaseOut()
            }
        };

        content.Opacity = 0;
        content.RenderTransform = new TranslateTransform(0, 10);
        Dispatcher.UIThread.Post(() =>
        {
            content.Opacity = 1;
            content.RenderTransform = new TranslateTransform(0, 0);
        }, DispatcherPriority.Background);
    }


    // ---- 关于 ----

    /// <summary>当前程序版本（取自程序集信息，发布时由流水线注入）。</summary>
    static string AppVersionText
    {
        get
        {
            var info = typeof(MainWindow).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var text = info?.InformationalVersion ?? "0.0.0";
            var plus = text.IndexOf('+');
            return plus >= 0 ? text[..plus] : text;
        }
    }

    Control BuildAboutCard()
    {
        var body = new StackPanel { Spacing = 18 };

        // 应用图标 + 名称 / 版本
        var logo = new Image { Width = 64, Height = 64, Stretch = Stretch.Uniform };
        try
        {
            logo.Source = new Bitmap(AssetLoader.Open(new Uri("avares://25DogMusic/Assets/appicon.ico")));
        }
        catch
        {
            logo.IsVisible = false;
        }

        var title = new TextBlock
        {
            Text = "25DogMusic",
            FontSize = 20,
            FontWeight = FontWeight.SemiBold,
            Foreground = TextPrimary
        };
        var subtitle = new TextBlock
        {
            Text = $"飞牛 NAS 音乐客户端 · 版本 {AppVersionText}",
            FontSize = 12.5,
            Foreground = TextSecondary,
            Margin = new Thickness(0, 2, 0, 0)
        };
        var titleStack = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        titleStack.Children.Add(title);
        titleStack.Children.Add(subtitle);

        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 14,
            Children = { logo, titleStack }
        };
        body.Children.Add(header);

        body.Children.Add(new TextBlock
        {
            Text = "25DogMusic 是基于 Avalonia 的桌面音乐客户端，连接飞牛 NAS 音乐服务，支持曲库浏览、搜索、播放、歌词与频谱。\n本项目仅用于访问你自己的飞牛音乐服务，不会上传任何数据。",
            FontSize = 13,
            Foreground = TextSecondary,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 22
        });

        var infoRow = new StackPanel { Spacing = 4 };
        infoRow.Children.Add(new TextBlock
        {
            Text = $"当前版本：{AppVersionText}",
            FontSize = 13,
            Foreground = TextPrimary
        });

        var status = new TextBlock
        {
            Text = string.Empty,
            FontSize = 12,
            Foreground = TextSecondary,
            TextWrapping = TextWrapping.Wrap,
            IsVisible = false
        };

        // 「检查更新」作为内联链接放在说明文字里，不再使用独立按钮
        var sourceLine = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        sourceLine.Children.Add(new TextBlock
        {
            Text = "更新来源：github.com/refout/25DogMusic/releases",
            FontSize = 12,
            Foreground = TextSecondary,
            VerticalAlignment = VerticalAlignment.Center
        });
        sourceLine.Children.Add(new TextBlock
        {
            Text = "·",
            FontSize = 12,
            Foreground = TextSecondary,
            VerticalAlignment = VerticalAlignment.Center
        });
        sourceLine.Children.Add(MakeLinkText("检查更新", () => _ = CheckForUpdatesAsync(status)));
        infoRow.Children.Add(sourceLine);
        body.Children.Add(infoRow);
        body.Children.Add(status);

        var links = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        links.Children.Add(MakeLinkText("项目主页", "https://refout.github.io/25DogMusic/"));
        links.Children.Add(MakeLinkSeparator());
        links.Children.Add(MakeLinkText("下载页面", "https://github.com/refout/25DogMusic/releases"));
        links.Children.Add(MakeLinkSeparator());
        links.Children.Add(MakeLinkText("问题反馈", "https://github.com/refout/25DogMusic/issues"));
        body.Children.Add(links);

        var ossSection = new StackPanel { Spacing = 6 };
        ossSection.Children.Add(new TextBlock
        {
            Text = "开源组件",
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = TextPrimary
        });
        ossSection.Children.Add(new TextBlock
        {
            Text = "本程序基于以下开源项目构建，感谢作者与社区：\n" +
                   "· Avalonia（MIT）— 跨平台 UI 框架\n" +
                   "· LibVLCSharp / VideoLAN LibVLC（LGPL-2.1+）— 音频播放内核\n" +
                   "· NAudio（MIT）— WASAPI 环回采集与频谱\n" +
                   "· FluentIcons.Avalonia（MIT）— 图标\n" +
                   "· Terminal.Gui（MIT）— 实验性终端界面",
            FontSize = 12.5,
            Foreground = TextSecondary,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 21
        });
        var noticeLine = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        noticeLine.Children.Add(new TextBlock
        {
            Text = "完整清单与许可证文本见仓库",
            FontSize = 12.5,
            Foreground = TextSecondary,
            VerticalAlignment = VerticalAlignment.Center
        });
        noticeLine.Children.Add(MakeLinkText("THIRD-PARTY-NOTICES.md",
            "https://github.com/refout/25DogMusic/blob/main/THIRD-PARTY-NOTICES.md"));
        ossSection.Children.Add(noticeLine);
        body.Children.Add(ossSection);

        body.Children.Add(AboutSection(
            "免责声明",
            "1. 本程序为第三方开源客户端，与飞牛（fnOS）官方无隶属或合作关系。\n" +
            "2. 程序仅通过你填写的服务器地址访问你自己的音乐服务，不提供、不分发任何音源内容。\n" +
            "3. 请确保你所播放的内容来源合法，因使用本程序产生的版权等责任由使用者自行承担。\n" +
            "4. 软件按「现状」提供，不附带任何明示或暗示的担保；使用风险由使用者自行承担。\n" +
            "5. 播放内核（VLC）遵循其自身许可证，本程序以动态链接方式使用。"));

        var license = new StackPanel { Spacing = 6 };
        license.Children.Add(new TextBlock
        {
            Text = "开源协议",
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = TextPrimary
        });
        var licenseLine = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        licenseLine.Children.Add(new TextBlock
        {
            Text = "本项目以",
            FontSize = 12.5,
            Foreground = TextSecondary,
            VerticalAlignment = VerticalAlignment.Center
        });
        licenseLine.Children.Add(MakeLinkText("MIT 协议", "https://github.com/refout/25DogMusic/blob/main/LICENSE"));
        licenseLine.Children.Add(new TextBlock
        {
            Text = "开源，可自由使用、修改与分发，请保留版权与许可声明。",
            FontSize = 12.5,
            Foreground = TextSecondary,
            VerticalAlignment = VerticalAlignment.Center
        });
        license.Children.Add(licenseLine);
        body.Children.Add(license);

        return MakeSettingsCard(body);
    }

    /// <summary>关于页的分节：小标题 + 正文。</summary>
    Control AboutSection(string heading, string text)
    {
        var stack = new StackPanel { Spacing = 6 };
        stack.Children.Add(new TextBlock
        {
            Text = heading,
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            Foreground = TextPrimary
        });
        stack.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 12.5,
            Foreground = TextSecondary,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 21
        });
        return stack;
    }

    /// <summary>文本样式的超链接：带下划线，悬停变淡，点击打开系统浏览器。</summary>
    Button MakeLinkText(string text, string url) => MakeLinkText(text, () => OpenExternal(url));

    /// <summary>文本样式的可点击行内链接（用于「检查更新」这类动作），无按钮外观。</summary>
    Button MakeLinkText(string text, Action action)
    {
        var link = new Button
        {
            Content = new TextBlock
            {
                Text = text,
                FontSize = 12.5,
                Foreground = AccentSoft,
                TextDecorations = TextDecorations.Underline,
                VerticalAlignment = VerticalAlignment.Center
            },
            Classes = { "link" },
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(0)
        };
        link.PointerEntered += (_, _) => link.Opacity = 0.7;
        link.PointerExited += (_, _) => link.Opacity = 1;
        link.Click += (_, _) => action();
        return link;
    }

    TextBlock MakeLinkSeparator() => new()
    {
        Text = "·",
        FontSize = 12.5,
        Foreground = TextSecondary,
        VerticalAlignment = VerticalAlignment.Center
    };

    static void OpenExternal(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // 打不开系统浏览器时忽略
        }
    }

    bool _checkingUpdates;

    async Task CheckForUpdatesAsync(TextBlock status)
    {
        if (_checkingUpdates) return;

        _checkingUpdates = true;
        status.IsVisible = true;
        status.Text = "正在检查更新…";

        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("25DogMusic");

            using var doc = JsonDocument.Parse(
                await http.GetStringAsync("https://api.github.com/repos/refout/25DogMusic/releases/latest"));

            var tag = doc.RootElement.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
            var url = doc.RootElement.TryGetProperty("html_url", out var u) ? u.GetString() ?? "" : "";
            var latest = tag.TrimStart('v', 'V');

            if (CompareVersionText(latest, AppVersionText) > 0)
            {
                status.Text = $"发现新版本 {tag}，已打开下载页面";
                if (url.Length > 0) OpenExternal(url);
            }
            else
            {
                status.Text = $"当前已是最新版本（{AppVersionText}）";
            }
        }
        catch (Exception ex)
        {
            status.Text = $"检查更新失败：{ex.Message}";
        }
        finally
        {
            _checkingUpdates = false;
        }
    }

    /// <summary>按段比较 x.y.z 版本号，忽略前缀 v 与预发布后缀。</summary>
    static int CompareVersionText(string a, string b)
    {
        var pa = a.Split('.');
        var pb = b.Split('.');
        var count = Math.Max(pa.Length, pb.Length);

        for (var i = 0; i < count; i++)
        {
            var x = i < pa.Length && int.TryParse(pa[i], out var vx) ? vx : 0;
            var y = i < pb.Length && int.TryParse(pb[i], out var vy) ? vy : 0;
            if (x != y) return x < y ? -1 : 1;
        }
        return 0;
    }

}
