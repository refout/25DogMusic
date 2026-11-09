# 第三方组件说明

25DogMusic 使用以下开源项目。各项目的版权与许可证归其作者所有，本文件仅作汇总说明；
完整许可证文本请查阅对应项目的仓库或 NuGet 包内的 LICENSE 文件。

## 运行时直接依赖（桌面客户端）

| 组件 | 版本 | 许可证 | 用途 |
| --- | --- | --- | --- |
| [Avalonia](https://github.com/AvaloniaUI/Avalonia) | 12.1.5 | MIT | 跨平台 UI 框架 |
| [Avalonia.Themes.Fluent](https://github.com/AvaloniaUI/Avalonia) | 12.1.5 | MIT | Fluent 主题 |
| [Avalonia.Fonts.Inter](https://github.com/AvaloniaUI/Avalonia) | 12.1.5 | MIT / OFL-1.1 | Inter 字体资源 |
| [LibVLCSharp](https://github.com/videolan/libvlcsharp) | 3.10.2 | LGPL-2.1-or-later | VLC 的 .NET 绑定 |
| [VideoLAN LibVLC](https://www.videolan.org/vlc/libvlc.html) | 3.0.24 | LGPL-2.1-or-later | 音频解码与播放内核（动态链接） |
| [NAudio](https://github.com/naudio/NAudio) | 2.2.1 | MIT | WASAPI 环回采集、频谱分析 |
| [FluentIcons.Avalonia](https://github.com/davidxuang/FluentIcons) | 2.1.344 | MIT | 界面图标 |
| [Fluent System Icons](https://github.com/microsoft/fluentui-system-icons) | — | MIT | 图标资源 |

## 终端版（实验性，`25DogMusic.Tui`）

| 组件 | 版本 | 许可证 | 用途 |
| --- | --- | --- | --- |
| [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui) | 2.0.0 | MIT | 终端界面框架 |
| [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp) | 3.1.11 | Six Labors Split License（开源用途为 Apache-2.0） | 终端封面渲染 |

## 构建与发布

| 组件 | 许可证 | 用途 |
| --- | --- | --- |
| [.NET 10 / NativeAOT](https://github.com/dotnet/runtime) | MIT | 运行时与原生 AOT 编译 |
| [Inno Setup](https://jrsoftware.org/isinfo.php) | Inno Setup License | Windows 安装包 |

## 关于 LGPL 组件

LibVLCSharp 与 LibVLC 以 LGPL-2.1-or-later 授权。本项目以**动态链接**方式使用 LibVLC：
发布包内的 `libvlc.dll` / `libvlc.dylib` / `libvlc.so` 及插件目录保持独立文件，
用户可自行替换为其他兼容版本的 LibVLC。相关源码可从
<https://code.videolan.org/videolan/vlc> 获取。
