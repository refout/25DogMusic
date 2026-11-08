# 25DogMusic

基于 [Avalonia](https://avaloniaui.net/) 的 Windows 桌面音乐客户端，连接飞牛 NAS（fnOS）的音乐服务，支持在线浏览、搜索、播放与歌词显示。

![界面预览](docs/assets/home.png)

## 功能特性

- **音乐库浏览**：专辑、歌手、风格、文件夹、收藏、最近播放，滚动到底自动加载
- **搜索**：按歌曲 / 专辑 / 歌手 / 歌单分类 Tab 展示，列表内部独立滚动分页
- **播放**：全屏播放页、播放队列抽屉、歌单批量添加歌曲抽屉
- **歌词**：全屏歌词跟随高亮，桌面歌词浮窗（仅在当前曲目有歌词时显示，可拖动）
- **频谱 / 均衡器**：基于 WASAPI 环回的实时频谱可视化，内置均衡器预设
- **漫游**：随机漫游曲库，下方展示漫游队列卡片，点击即可切换
- **外观**：深色 / 浅色主题、封面取色背景、Windows 11 云母材质与圆角、Windows 10 标题栏适配
- **后台播放**：关闭窗口后可继续播放并驻留托盘

## 技术栈

| 组件 | 说明 |
| --- | --- |
| UI | Avalonia 12.1.5（Fluent 主题） |
| 运行时 | .NET 10 |
| 音频 | LibVLCSharp + VideoLAN LibVLC、NAudio |
| 图标 | FluentIcons.Avalonia |
| 终端版 | Terminal.Gui 2.0（`25DogMusic.Tui`，实验性） |

## 目录结构

```
.
├── .github/workflows/   # CI 与 Release（NativeAOT 打包）流水线
├── docs/                # GitHub Pages 站点
└── src/
    ├── 25DogMusic.slnx
    ├── 25DogMusic.Core/       # API 客户端、配置、歌词解析
    ├── 25DogMusic.Desktop/    # Avalonia 桌面客户端
    └── 25DogMusic.Tui/        # 终端界面（实验性）
```

## 本地开发

主要支持 Windows；Linux / macOS 亦可构建，需各自的原生工具链（Linux 需 `clang`、`zlib1g-dev` 与 `libvlc-dev`，macOS 需 Xcode）。环境要求：[.NET 10 SDK](https://dotnet.microsoft.com/download)。

```powershell
# 还原与构建
dotnet build src/25DogMusic.slnx

# 启动桌面客户端
dotnet run --project src/25DogMusic.Desktop
```

首次启动需在登录页填写飞牛 NAS 地址并登录。登录信息保存在系统本地应用数据目录下的 `FnMusic/config.json`（Windows：`%LOCALAPPDATA%\FnMusic\config.json`，Linux：`~/.local/share/FnMusic/config.json`，macOS：`~/Library/Application Support/FnMusic/config.json`）。

## 构建与发布

- **CI**：推送到 `main` 或提交 PR 时自动还原并构建 Release（`.github/workflows/ci.yml`）。
- **打包**：推送 `v*` 标签触发 NativeAOT 发布并创建 GitHub Release（`.github/workflows/release.yml`）：
  - Windows：`25DogMusic-<版本>-win-x64.zip` 与 `25DogMusic-<版本>-windows-x64-setup.exe` 安装包
  - Linux：`25DogMusic-<版本>-linux-x64.tar.gz`（内置 libvlc 与启动脚本 `25DogMusic.sh`）
  - macOS（Apple Silicon）：`25DogMusic-<版本>-macos-arm64.zip`（内置 libvlc 的 `.app`，未签名）

```powershell
# 本地 AOT 打包（需安装 VS「使用 C++ 的桌面开发」组件）
dotnet publish src/25DogMusic.Desktop/25DogMusic.Desktop.csproj `
  -c Release -r win-x64 --self-contained true -p:PublishAot=true `
  -o publish/25DogMusic-win-x64
```

## 相关链接

- 项目主页：<https://refout.github.io/25DogMusic/>
- 下载最新版本：<https://github.com/refout/25DogMusic/releases/latest>
- 问题反馈：<https://github.com/refout/25DogMusic/issues>
