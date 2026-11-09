// 右侧抽屉：播放队列与歌单「添加歌曲」。

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
using FnMusic.Api;
using FnMusic.Config;
using FnMusic.Playback;
using FluentIcons.Avalonia;
using FluentIcons.Common;

namespace _25DogMusic.Desktop;

public partial class MainWindow : Window
{

    // ---- 播放列表抽屉 ----

    readonly List<(string Guid, Border Row, TextBlock Number, TextBlock Title)> _drawerRows = new();

    void ToggleQueueDrawer()
    {
        if (QueuePanel.IsVisible) HideQueueDrawer();
        else ShowQueueDrawer();
    }

    void ShowQueueDrawer()
    {
        HidePickerDrawer();
        RefreshQueueDrawer();
        QueuePanel.IsVisible = true;
        Dispatcher.UIThread.Post(() => QueuePanelScroll.Offset = new Vector(0, 0), DispatcherPriority.Background);
    }

    void HideQueueDrawer() => QueuePanel.IsVisible = false;

    void RefreshQueueDrawer()
    {
        QueuePanelRows.Children.Clear();
        _drawerRows.Clear();

        QueuePanelCount.Text = _queue.Count == 0 ? string.Empty : $"共 {_queue.Count} 首";
        QueuePanelClear.IsVisible = _queue.Count > 0;

        if (_queue.Count == 0)
        {
            QueuePanelRows.Children.Add(new TextBlock
            {
                Text = "还没有歌曲，先播放一首吧",
                FontSize = 12.5,
                Foreground = TextMuted,
                Margin = new Thickness(4, 20, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        for (var i = 0; i < _queue.Count; i++)
        {
            QueuePanelRows.Children.Add(BuildDrawerRow(_queue[i], i));
        }

        UpdateQueueDrawerHighlight();
    }

    Control BuildDrawerRow(Track track, int index)
    {
        var number = new TextBlock
        {
            Text = (index + 1).ToString(),
            FontSize = 11.5,
            Foreground = TextMuted,
            Width = 22,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Right
        };

        var title = new TextBlock
        {
            Text = track.Title,
            FontSize = 13,
            Foreground = TextPrimary,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var sub = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(track.Artist) ? string.Empty : track.Artist,
            FontSize = 10.5,
            Foreground = TextSecondary,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var texts = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        texts.Children.Add(title);
        texts.Children.Add(sub);

        var duration = new TextBlock
        {
            Text = FormatDuration(track.DurationMs),
            FontSize = 10.5,
            Foreground = TextMuted,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("22,*,Auto"), VerticalAlignment = VerticalAlignment.Center };
        grid.Children.Add(number);
        Grid.SetColumn(texts, 1);
        grid.Children.Add(texts);
        Grid.SetColumn(duration, 2);
        grid.Children.Add(duration);

        var row = new Border
        {
            Padding = new Thickness(8, 7),
            CornerRadius = new CornerRadius(10),
            Background = Brushes.Transparent,
            Child = grid,
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        row.PointerEntered += (_, _) =>
        {
            if (!IsPlayingTrack(track)) row.Background = RowHover;
        };
        row.PointerExited += (_, _) =>
        {
            row.Background = IsPlayingTrack(track) ? RowActive : Brushes.Transparent;
        };
        row.PointerPressed += (_, _) => PlayFromQueue(_queue, index);

        _drawerRows.Add((track.Guid, row, number, title));
        return row;
    }

    void UpdateQueueDrawerHighlight()
    {
        if (!QueuePanel.IsVisible) return;

        foreach (var item in _drawerRows)
        {
            var active = item.Guid == _currentTrack?.Guid;
            item.Row.Background = active ? RowActive : Brushes.Transparent;
            item.Title.Foreground = active ? AccentSoft : TextPrimary;
            item.Number.Foreground = active ? AccentSoft : TextMuted;
        }
    }


    // ---- 歌单「添加歌曲」侧边抽屉 ----

    async Task ShowPickerDrawerAsync(Playlist playlist)
    {
        HideQueueDrawer();
        _pickerPlaylist = playlist;
        PickerPanelName.Text = $"「{playlist.Name}」";
        PickerSearchBox.Text = string.Empty;
        _pickerKeyword = string.Empty;
        _pickerSelected.Clear();
        UpdatePickerSummary();
        PickerPanel.IsVisible = true;
        await ReloadPickerAsync();
    }

    void HidePickerDrawer()
    {
        PickerPanel.IsVisible = false;
        _pickerPlaylist = null;
    }

    async Task ReloadPickerAsync()
    {
        _pickerTracks.Clear();
        _pickerPage = 0;
        _pickerTotal = -1;
        _pickerFinished = false;
        _pickerLoading = false;
        PickerPanelRows.Children.Clear();
        PickerPanelScroll.Offset = new Vector(0, 0);
        await LoadPickerPageAsync();
    }

    void OnPickerScrolled()
    {
        if (_pickerLoading || _pickerFinished) return;
        if (PickerPanelScroll.Extent.Height <= 0) return;
        if (PickerPanelScroll.Offset.Y + PickerPanelScroll.Viewport.Height >= PickerPanelScroll.Extent.Height - 160)
        {
            _ = LoadPickerPageAsync();
        }
    }

    async Task LoadPickerPageAsync()
    {
        if (_pickerLoading || _pickerFinished || _pickerPlaylist is null) return;
        _pickerLoading = true;

        try
        {
            _pickerPage++;
            using var doc = _pickerKeyword.Length > 0
                ? await Api().SearchTracksAsync(_pickerKeyword, _pickerPage, 100)
                : await Api().GetTracksAsync(_pickerPage, 100);

            if (_pickerTotal < 0) _pickerTotal = PageTotal(doc);
            var batch = Library.ParseTracks(doc);

            if (batch.Count == 0)
            {
                _pickerFinished = true;
                if (_pickerTracks.Count == 0)
                {
                    PickerPanelRows.Children.Add(new TextBlock
                    {
                        Text = "没有找到歌曲",
                        FontSize = 12.5,
                        Foreground = TextMuted,
                        Margin = new Thickness(4, 16, 0, 0)
                    });
                }
                return;
            }

            foreach (var track in batch)
            {
                _pickerTracks.Add(track);
                PickerPanelRows.Children.Add(BuildPickerRow(track));
            }

            if (_pickerTotal >= 0 && _pickerTracks.Count >= _pickerTotal) _pickerFinished = true;
        }
        catch (Exception ex)
        {
            _pickerFinished = true;
            HandleError(ex, "加载歌曲失败");
        }
        finally
        {
            _pickerLoading = false;
        }
    }

    Control BuildPickerRow(Track track)
    {
        var box = new CheckBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            IsChecked = _pickerSelected.Contains(track.Guid)
        };
        box.IsCheckedChanged += (_, _) =>
        {
            if (box.IsChecked == true) _pickerSelected.Add(track.Guid);
            else _pickerSelected.Remove(track.Guid);
            UpdatePickerSummary();
        };

        var title = new TextBlock
        {
            Text = track.Title,
            FontSize = 13,
            Foreground = TextPrimary,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var sub = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(track.Artist) ? track.AlbumName : $"{track.Artist} · {track.AlbumName}",
            FontSize = 10.5,
            Foreground = TextSecondary,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var texts = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        texts.Children.Add(title);
        texts.Children.Add(sub);

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        grid.Children.Add(box);
        Grid.SetColumn(texts, 1);
        grid.Children.Add(texts);

        var row = new Border
        {
            Padding = new Thickness(8, 7),
            CornerRadius = new CornerRadius(10),
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = grid
        };
        row.PointerEntered += (_, _) => row.Background = RowHover;
        row.PointerExited += (_, _) => row.Background = Brushes.Transparent;
        row.PointerPressed += (_, _) => box.IsChecked = !(box.IsChecked == true);
        return row;
    }

    void UpdatePickerSummary()
    {
        PickerPanelSummary.Text = $"已选 {_pickerSelected.Count} 首";
        PickerPanelAdd.IsEnabled = _pickerSelected.Count > 0;
    }

    async Task AddSelectedFromPickerAsync()
    {
        var playlist = _pickerPlaylist;
        if (playlist is null || _pickerSelected.Count == 0) return;

        PickerPanelAdd.IsEnabled = false;
        PickerPanelAdd.Content = "正在添加…";

        var added = 0;
        foreach (var guid in _pickerSelected.ToList())
        {
            try
            {
                await Api().AddTrackToPlaylistAsync(playlist.Guid, guid);
                added++;
            }
            catch
            {
                // 单首失败不影响其余歌曲
            }
        }

        PickerPanelAdd.Content = "添加";
        UpdatePickerSummary();

        if (added > 0)
        {
            StatusText.Text = $"已添加 {added} 首到歌单「{playlist.Name}」";
            HidePickerDrawer();
            await OpenPlaylistAsync(playlist);
        }
        else
        {
            StatusText.Text = "添加失败，请稍后再试";
        }
    }

    Border MakeSettingsCard(StackPanel content) => new()
    {
        Background = Surface,
        CornerRadius = new CornerRadius(12),
        Padding = new Thickness(18),
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Child = content
    };

    Control BuildField(string label, Control field)
    {
        var stack = new StackPanel { Spacing = 5 };
        stack.Children.Add(new TextBlock { Text = label, FontSize = 11.5, Foreground = TextSecondary });
        stack.Children.Add(field);
        return stack;
    }

    Control BuildSettingRow(string label, string? desc, Control control)
    {
        var texts = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        texts.Children.Add(new TextBlock { Text = label, FontSize = 13, Foreground = TextPrimary });
        if (!string.IsNullOrWhiteSpace(desc))
        {
            texts.Children.Add(new TextBlock
            {
                Text = desc,
                FontSize = 11.5,
                Foreground = TextMuted,
                TextWrapping = TextWrapping.Wrap
            });
        }

        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), VerticalAlignment = VerticalAlignment.Center };
        grid.Children.Add(texts);
        Grid.SetColumn(control, 1);
        grid.Children.Add(control);
        return grid;
    }

    Control BuildAccountCard(string? message)
    {
        var loggedIn = !string.IsNullOrWhiteSpace(_config.Token);

        var statusText = new TextBlock
        {
            Text = message ?? (loggedIn
                ? string.IsNullOrWhiteSpace(_config.LastUsername) ? "已连接飞牛音乐" : $"已登录为 {_config.LastUsername}"
                : "未登录，请填写账号或使用浏览器授权"),
            FontSize = 12.5,
            Foreground = loggedIn ? TextPrimary : TextSecondary,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        var dot = new Border
        {
            Width = 7,
            Height = 7,
            CornerRadius = new CornerRadius(4),
            Background = new SolidColorBrush(Color.Parse(loggedIn ? "#34C759" : "#FF9F0A")),
            VerticalAlignment = VerticalAlignment.Center
        };

        var statusRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        statusRow.Children.Add(dot);
        statusRow.Children.Add(statusText);

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), VerticalAlignment = VerticalAlignment.Center };
        header.Children.Add(statusRow);

        if (loggedIn)
        {
            var logout = new Button
            {
                Content = "退出登录",
                FontSize = 12,
                Padding = new Thickness(14, 6),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            logout.Classes.Add("ghost");
            logout.Click += (_, _) => Logout();
            Grid.SetColumn(logout, 1);
            header.Children.Add(logout);
        }

        // 已登录：只展示账号信息，不显示登录表单
        if (loggedIn)
        {
            TextBlock InfoValue(string text) => new()
            {
                Text = text,
                FontSize = 12.5,
                Foreground = TextSecondary,
                VerticalAlignment = VerticalAlignment.Center
            };

            var infoBody = new StackPanel { Spacing = 14 };
            infoBody.Children.Add(header);
            infoBody.Children.Add(BuildSettingRow("服务器地址", null, InfoValue(_config.BaseUrl)));
            infoBody.Children.Add(BuildSettingRow("当前账号", null,
                InfoValue(string.IsNullOrWhiteSpace(_config.LastUsername) ? "飞牛音乐用户" : _config.LastUsername)));
            infoBody.Children.Add(new TextBlock
            {
                Text = "需要切换账号？先点击右上角「退出登录」，登录表单会重新出现。",
                FontSize = 11.5,
                Foreground = TextMuted,
                TextWrapping = TextWrapping.Wrap
            });
            return MakeSettingsCard(infoBody);
        }

        var serverBox = MakeFormBox(_config.BaseUrl, "https://nas.example.com");
        var userBox = MakeFormBox(_config.LastUsername ?? string.Empty, "用户名");
        var passBox = MakeFormBox(string.Empty, "密码");
        passBox.PasswordChar = '*';
        passBox.KeyDown += async (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            e.Handled = true;
            await PasswordLoginAsync(serverBox.Text, userBox.Text, passBox.Text, statusText);
        };

        var loginButton = MakeFormButton("登录");
        loginButton.Click += async (_, _) =>
            await PasswordLoginAsync(serverBox.Text, userBox.Text, passBox.Text, statusText);

        var oauthButton = MakeFormButton("在浏览器中授权");
        oauthButton.Click += async (_, _) => await StartOAuthAsync(serverBox.Text, statusText);

        var codeBox = MakeFormBox(string.Empty, "code 或完整回跳地址");
        var submitButton = MakeFormButton("提交授权码");
        submitButton.Width = 120;
        submitButton.HorizontalAlignment = HorizontalAlignment.Right;
        submitButton.Click += async (_, _) => await SubmitOAuthAsync(serverBox.Text, codeBox.Text, statusText);

        var credentials = new Grid { ColumnDefinitions = new ColumnDefinitions("*,12,*") };
        credentials.Children.Add(BuildField("用户名", userBox));
        var passField = BuildField("密码", passBox);
        Grid.SetColumn(passField, 2);
        credentials.Children.Add(passField);

        var codeRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,110") };
        codeRow.Children.Add(codeBox);
        Grid.SetColumn(submitButton, 2);
        codeRow.Children.Add(submitButton);

        TextBlock SectionHeading(string text) => new()
        {
            Text = text,
            FontSize = 12.5,
            FontWeight = FontWeight.SemiBold,
            Foreground = CardLabel,
            Margin = new Thickness(0, 0, 0, 2)
        };

        var passwordPane = new StackPanel { Spacing = 10 };
        passwordPane.Children.Add(SectionHeading("账号密码登录"));
        passwordPane.Children.Add(BuildField("服务器地址", serverBox));
        passwordPane.Children.Add(credentials);
        passwordPane.Children.Add(loginButton);

        var oauthPane = new StackPanel { Spacing = 10 };
        oauthPane.Children.Add(SectionHeading("浏览器授权"));
        oauthPane.Children.Add(new TextBlock
        {
            Text = "打开飞牛 NAS 授权页，登录后把回跳地址或 code 粘贴到下方。",
            FontSize = 11.5,
            Foreground = TextMuted,
            TextWrapping = TextWrapping.Wrap
        });
        oauthPane.Children.Add(oauthButton);
        oauthPane.Children.Add(codeRow);

        var divider = new Border
        {
            Width = 1,
            Background = Divider,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(0, 2)
        };

        var columns = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,1,*"),
            ColumnSpacing = 22
        };
        columns.Children.Add(passwordPane);
        Grid.SetColumn(divider, 1);
        columns.Children.Add(divider);
        Grid.SetColumn(oauthPane, 2);
        columns.Children.Add(oauthPane);

        var body = new StackPanel { Spacing = 16 };
        body.Children.Add(header);
        body.Children.Add(columns);

        return MakeSettingsCard(body);
    }

    Control BuildAppearanceCard()
    {
        var swatches = new Dictionary<string, Border>();

        var options = IsLight
            ? new (string Mode, string Label, IBrush Brush)[]
            {
                ("cover", "跟随封面", new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop(Color.Parse("#D9D9E4"), 0),
                        new GradientStop(Color.Parse("#F8F8FC"), 1)
                    }
                }),
                ("black", "纯白", new SolidColorBrush(Color.Parse("#FFFFFF"))),
                ("gray", "浅灰", new SolidColorBrush(Color.Parse("#F0F0F5"))),
                ("navy", "浅蓝", new SolidColorBrush(Color.Parse("#E8EFFA"))),
                ("wine", "暖白", new SolidColorBrush(Color.Parse("#FAF5EE")))
            }
            : new (string Mode, string Label, IBrush Brush)[]
            {
                ("cover", "跟随封面", new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop(Color.Parse("#4B3B5A"), 0),
                        new GradientStop(Color.Parse("#121218"), 1)
                    }
                }),
                ("black", "纯黑", new SolidColorBrush(Color.Parse("#0B0B0E"))),
                ("gray", "深灰", new SolidColorBrush(Color.Parse("#17171C"))),
                ("navy", "午夜蓝", new SolidColorBrush(Color.Parse("#101A2E"))),
                ("wine", "深棕", new SolidColorBrush(Color.Parse("#1A1112")))
            };

        var wrap = new WrapPanel { Margin = new Thickness(0, 2, 0, 0) };

        foreach (var option in options)
        {
            var coverImage = new Image { Stretch = Stretch.UniformToFill };

            var fillContent = new Grid();
            fillContent.Children.Add(new Border { Background = option.Brush });
            if (option.Mode == "cover") fillContent.Children.Add(coverImage);

            var fill = new Border
            {
                Width = 58,
                Height = 58,
                CornerRadius = new CornerRadius(12),
                ClipToBounds = true,
                Background = Brushes.Transparent,
                BorderBrush = SwatchBorder,
                BorderThickness = new Thickness(1),
                IsHitTestVisible = false,
                Child = fillContent
            };

            var swatch = new Button
            {
                Content = fill,
                Width = 58,
                Height = 58,
                Padding = new Thickness(0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            swatch.Classes.Add("swatch");
            swatch.Click += (_, _) =>
            {
                _config.BackgroundMode = option.Mode;
                _config.Save();
                ApplyBackground();
                UpdateSelection();
                StatusText.Text = $"背景：{option.Label}";
            };

            var item = new StackPanel { Spacing = 0, Margin = new Thickness(0, 0, 16, 8) };
            item.Children.Add(swatch);
            item.Children.Add(new TextBlock
            {
                Text = option.Label,
                FontSize = 11.5,
                Foreground = TextSecondary,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 6, 0, 0)
            });
            wrap.Children.Add(item);
            swatches[option.Mode] = fill;

            if (option.Mode == "cover") _ = LoadCoverSwatchAsync(coverImage);
        }

        // 末尾的颜色选择方框：点击打开调色板
        var customColor = Color.TryParse(_config.BackgroundCustomColor, out var parsed)
            ? parsed
            : Color.Parse("#2B1B33");
        var customFill = new Border
        {
            Width = 58,
            Height = 58,
            CornerRadius = new CornerRadius(12),
            ClipToBounds = true,
            Background = new SolidColorBrush(customColor),
            BorderBrush = SwatchBorder,
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false
        };

        var customButton = new Button
        {
            Content = customFill,
            Width = 58,
            Height = 58,
            Padding = new Thickness(0),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        customButton.Classes.Add("swatch");

        var palette = new WrapPanel { Width = 210, Margin = new Thickness(2) };
        foreach (var hex in new[]
                 {
                     "#FF375F", "#FF7A45", "#F5A524", "#34C759", "#0FA3A3", "#2E7CF6",
                     "#7A5CFF", "#C04AB8", "#8A5A44", "#6B7280", "#2B1B33", "#0B0B0E"
                 })
        {
            var color = Color.Parse(hex);
            var chip = new Border
            {
                Width = 30,
                Height = 30,
                Margin = new Thickness(3),
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(color),
                BorderBrush = SwatchBorder,
                BorderThickness = new Thickness(1),
                Cursor = new Cursor(StandardCursorType.Hand)
            };
            chip.PointerPressed += (_, _) =>
            {
                _config.BackgroundCustomColor = hex;
                _config.BackgroundMode = "custom";
                _config.Save();
                customFill.Background = new SolidColorBrush(color);
                ApplyBackground();
                UpdateSelection();
                StatusText.Text = $"背景：自定义 {hex}";
                customButton.Flyout?.Hide();
            };
            palette.Children.Add(chip);
        }

        customButton.Flyout = new Flyout { Content = palette };

        var customItem = new StackPanel { Spacing = 0, Margin = new Thickness(0, 0, 16, 8) };
        customItem.Children.Add(customButton);
        customItem.Children.Add(new TextBlock
        {
            Text = "自定义",
            FontSize = 11.5,
            Foreground = TextSecondary,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0)
        });
        wrap.Children.Add(customItem);
        swatches["custom"] = customFill;

        void UpdateSelection()
        {
            foreach (var pair in swatches)
            {
                var selected = string.Equals(_config.BackgroundMode, pair.Key, StringComparison.OrdinalIgnoreCase);
                pair.Value.BorderBrush = selected ? (IsLight ? TextPrimary : Brushes.White) : SwatchBorder;
                pair.Value.BorderThickness = new Thickness(selected ? 2 : 1);
            }
        }
        UpdateSelection();

        var body = new StackPanel { Spacing = 12 };

        var themeBox = new ComboBox
        {
            ItemsSource = new[] { "深色", "浅色" },
            SelectedIndex = IsLight ? 1 : 0,
            Width = 120,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        themeBox.SelectionChanged += (_, _) =>
        {
            var index = themeBox.SelectedIndex;
            if (index < 0) return;
            var target = index == 1 ? "light" : "dark";
            if (target != _config.Theme) Dispatcher.UIThread.Post(() => ApplyTheme(target));
        };
        body.Children.Add(BuildSettingRow("主题", "深色或浅色外观", themeBox));

        var materialKeys = new[] { "off", "mica", "acrylic", "blur", "liquid" };
        var materialIndex = Array.IndexOf(materialKeys, string.IsNullOrWhiteSpace(_config.Material) ? "off" : _config.Material);
        if (materialIndex < 0) materialIndex = 0;

        var materialBox = new ComboBox
        {
            ItemsSource = new[] { "无特效", "云母 Mica", "亚克力 Acrylic", "模糊 Blur", "液态玻璃" },
            SelectedIndex = materialIndex,
            Width = 190,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        body.Children.Add(BuildSettingRow("窗口材质",
            IsWindows11 ? "Windows 11 的云母 / 亚克力 / 液态玻璃效果" : "Windows 10 不支持窗口材质，将使用纯色背景",
            materialBox));

        var opacityValue = new TextBlock
        {
            Text = $"{(int)Math.Round(_config.UiOpacity * 100)}%",
            FontSize = 12,
            Foreground = TextSecondary,
            Width = 44,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        var opacitySlider = new Slider
        {
            Minimum = 30,
            Maximum = 100,
            Value = Math.Clamp(_config.UiOpacity * 100, 30, 100),
            Width = 190,
            VerticalAlignment = VerticalAlignment.Center
        };
        var opacityBox = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        opacityBox.Children.Add(opacitySlider);
        opacityBox.Children.Add(opacityValue);
        body.Children.Add(BuildSettingRow("窗口透明度", "整个窗口（内容区、侧边栏、播放栏）的透出程度", opacityBox));

        var description = new TextBlock
        {
            FontSize = 11.5,
            Foreground = TextMuted,
            TextWrapping = TextWrapping.Wrap
        };
        body.Children.Add(description);
        body.Children.Add(wrap);

        void UpdateMaterialState()
        {
            var active = materialBox.IsEnabled && materialBox.SelectedIndex > 0;
            opacitySlider.IsEnabled = active;
            opacityValue.Opacity = active ? 1.0 : 0.45;
            wrap.IsEnabled = true;
            wrap.Opacity = 1.0;
            description.Text = active
                ? "材质与背景作用于整个窗口：跟随封面时透出当前歌曲封面，纯色时按所选颜色着色；「窗口透明度」统一控制整个窗口的透出程度。"
                : "选择窗口背景。跟随封面会用当前歌曲封面做柔和底图，纯色背景更安静。";
        }

        materialBox.SelectionChanged += (_, _) =>
        {
            var index = materialBox.SelectedIndex;
            if (index < 0) return;
            _config.Material = materialKeys[index];
            _config.Save();
            ApplyBackground(announceMaterial: true);
            UpdateMaterialState();
        };

        if (!IsWindows11) materialBox.IsEnabled = false;

        opacitySlider.ValueChanged += (_, e) =>
        {
            var percent = (int)Math.Round(e.NewValue);
            _config.UiOpacity = percent / 100.0;
            opacityValue.Text = $"{percent}%";
            ApplyBackground();
        };
        opacitySlider.PointerReleased += (_, _) => _config.Save();

        UpdateMaterialState();
        return MakeSettingsCard(body);
    }

    Control BuildPlaybackCard()
    {
        var body = new StackPanel { Spacing = 14 };

        var qualitySwitch = new ToggleSwitch
        {
            IsChecked = _config.ShowAudioQuality,
            OnContent = string.Empty,
            OffContent = string.Empty,
            VerticalAlignment = VerticalAlignment.Center
        };
        qualitySwitch.IsCheckedChanged += (_, _) =>
        {
            _config.ShowAudioQuality = qualitySwitch.IsChecked == true;
            _config.Save();
            RefreshNowPlayingText();
        };
        body.Children.Add(BuildSettingRow(
            "显示音质信息",
            "在播放栏与全屏页显示当前曲目的格式、位深与采样率",
            qualitySwitch));

        var backgroundSwitch = new ToggleSwitch
        {
            IsChecked = _config.BackgroundPlayback,
            OnContent = string.Empty,
            OffContent = string.Empty,
            VerticalAlignment = VerticalAlignment.Center
        };
        backgroundSwitch.IsCheckedChanged += (_, _) =>
        {
            _config.BackgroundPlayback = backgroundSwitch.IsChecked == true;
            _config.Save();
        };
        body.Children.Add(BuildSettingRow(
            "关闭窗口后继续播放",
            "开启时关闭窗口会隐藏到系统托盘，音乐不中断；退出请使用托盘菜单",
            backgroundSwitch));

        var volumeValue = new TextBlock
        {
            Text = _config.Volume.ToString(),
            FontSize = 12,
            Foreground = TextSecondary,
            Width = 34,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        var volumeSlider = new Slider
        {
            Minimum = 0,
            Maximum = 100,
            Value = _config.Volume,
            Width = 190,
            VerticalAlignment = VerticalAlignment.Center
        };
        volumeSlider.ValueChanged += (_, e) =>
        {
            var value = (int)e.NewValue;
            _config.Volume = value;
            volumeValue.Text = value.ToString();
            VolumeSlider.Value = value;
        };
        volumeSlider.PointerReleased += (_, _) => _config.Save();

        var volumeBox = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        volumeBox.Children.Add(volumeSlider);
        volumeBox.Children.Add(volumeValue);
        body.Children.Add(BuildSettingRow("默认音量", "启动应用时使用的音量", volumeBox));

        var shuffleSwitch = new ToggleSwitch
        {
            IsChecked = _config.StartShuffle,
            OnContent = string.Empty,
            OffContent = string.Empty,
            VerticalAlignment = VerticalAlignment.Center
        };
        shuffleSwitch.IsCheckedChanged += (_, _) =>
        {
            _config.StartShuffle = shuffleSwitch.IsChecked == true;
            _shuffle = _config.StartShuffle;
            UpdatePlaybackModeUi();
            _config.Save();
        };
        body.Children.Add(BuildSettingRow("启动时随机播放", "开启后启动默认按随机顺序播放", shuffleSwitch));

        var repeatBox = new ComboBox
        {
            ItemsSource = new[] { "关闭", "列表循环", "单曲循环" },
            SelectedIndex = Math.Clamp(_config.StartRepeat, 0, 2),
            Width = 150,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        repeatBox.SelectionChanged += (_, _) =>
        {
            _config.StartRepeat = repeatBox.SelectedIndex;
            _repeat = Math.Clamp(_config.StartRepeat, 0, 2);
            UpdatePlaybackModeUi();
            _config.Save();
        };
        body.Children.Add(BuildSettingRow("默认循环模式", "启动时应用的循环方式", repeatBox));

        var spectrumSwitch = new ToggleSwitch
        {
            IsChecked = _config.SpectrumEnabled,
            OnContent = string.Empty,
            OffContent = string.Empty,
            VerticalAlignment = VerticalAlignment.Center
        };
        spectrumSwitch.IsCheckedChanged += (_, _) =>
        {
            _config.SpectrumEnabled = spectrumSwitch.IsChecked == true;
            _config.Save();
            SyncSpectrum(_player?.IsPlaying == true && !_paused);
        };
        body.Children.Add(BuildSettingRow("频谱可视化", "全屏播放页显示跟随音频的实时频谱", spectrumSwitch));

        var lyricsSwitch = new ToggleSwitch
        {
            IsChecked = _config.DesktopLyrics,
            OnContent = string.Empty,
            OffContent = string.Empty,
            VerticalAlignment = VerticalAlignment.Center
        };
        lyricsSwitch.IsCheckedChanged += (_, _) =>
        {
            _config.DesktopLyrics = lyricsSwitch.IsChecked == true;
            _config.Save();
            ApplyDesktopLyrics();
        };
        body.Children.Add(BuildSettingRow("桌面歌词", "在屏幕底部显示可拖动的浮动歌词", lyricsSwitch));

        var presetNames = new List<string> { "关闭" };
        presetNames.AddRange(_player?.EqualizerPresets ?? Array.Empty<string>());
        var equalizerBox = new ComboBox
        {
            ItemsSource = presetNames,
            SelectedIndex = Math.Clamp(_config.EqPresetIndex + 1, 0, presetNames.Count - 1),
            Width = 170,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        equalizerBox.SelectionChanged += (_, _) =>
        {
            _config.EqPresetIndex = equalizerBox.SelectedIndex - 1;
            _player?.ApplyEqualizer(_config.EqPresetIndex);
            _config.Save();
        };
        body.Children.Add(BuildSettingRow("均衡器", "使用播放器内置的音效预设", equalizerBox));

        return MakeSettingsCard(body);
    }

    void ApplyBackground(bool announceMaterial = false)
    {
        var mode = string.IsNullOrWhiteSpace(_config.BackgroundMode) ? "cover" : _config.BackgroundMode;
        var material = string.IsNullOrWhiteSpace(_config.Material) ? "off" : _config.Material;
        // Windows 10 不支持 Mica / Acrylic 背景，强制退回纯色，避免透出黑块
        var useMaterial = material != "off" && IsWindows11;
        var opacity = Math.Clamp(_config.UiOpacity <= 0 ? 0.75 : _config.UiOpacity, 0.3, 1.0);

        var customColor = Color.TryParse(_config.BackgroundCustomColor, out var parsedCustom)
            ? parsedCustom
            : Color.Parse(IsLight ? "#F5F5F9" : "#0B0B0E");

        var page = mode == "custom"
            ? customColor
            : Color.Parse(IsLight
                ? mode switch
                {
                    "black" => "#FFFFFF",
                    "gray" => "#F0F0F5",
                    "navy" => "#E8EFFA",
                    "wine" => "#FAF5EE",
                    _ => "#F5F5F9"
                }
                : mode switch
                {
                    "black" => "#0B0B0E",
                    "gray" => "#17171C",
                    "navy" => "#101A2E",
                    "wine" => "#1A1112",
                    _ => "#0B0B0E"
                });

        var themePage = Color.Parse(IsLight ? "#F5F5F9" : "#0B0B0E");
        // 材质与背景可叠加：跟随封面时底色取自封面平均色，纯色时用所选颜色
        var useCover = mode == "cover";
        var coverBase = useCover ? CoverTint(_coverTint ?? themePage) : page;
        Backdrop.IsVisible = useCover;
        BackdropShade.IsVisible = useCover;
        // 封面压暗/提亮后再透出，既能看见封面又保证文字对比度；材质模式下随透明度一起淡出
        var backdropStrength = IsLight ? 0.50 : 0.62;
        Backdrop.Opacity = useMaterial
            ? backdropStrength * (0.35 + 0.65 * opacity)
            : backdropStrength;
        BackdropShade.Background = new SolidColorBrush(
            IsLight ? Colors.White : Colors.Black,
            IsLight ? 0.72 : 0.64);

        var liquid = material == "liquid";
        GlassSheen.IsVisible = liquid;
        GlassGlow.IsVisible = liquid;
        GlassEdge.IsVisible = liquid;

        // 纯色背景时，侧边栏与播放条都从同一个背景色派生，保证整窗一致
        var solidTint = !useCover && !useMaterial;
        Color sideColor;
        Color playerColor;
        double sideAlpha;
        double playerAlpha;

        if (solidTint)
        {
            sideColor = BlendColor(page, Colors.Black, IsLight ? 0.035 : 0.22);
            playerColor = BlendColor(page, IsLight ? Colors.White : Colors.Black, IsLight ? 0.62 : 0.1);
            sideAlpha = 1.0;
            playerAlpha = 1.0;
        }
        else if (useMaterial)
        {
            sideColor = BlendColor(coverBase, Colors.Black, IsLight ? 0.04 : 0.2);
            playerColor = BlendColor(coverBase, IsLight ? Colors.White : Colors.Black, IsLight ? 0.55 : 0.08);

            // 透明度作用于整个窗口：侧边栏、内容区、播放栏使用同一个透明度
            sideAlpha = 0.45 + 0.55 * opacity;
            playerAlpha = sideAlpha;
        }
        else if (useCover)
        {
            sideColor = BlendColor(coverBase, Colors.Black, IsLight ? 0.05 : 0.18);
            playerColor = BlendColor(coverBase, IsLight ? Colors.White : Colors.Black, IsLight ? 0.55 : 0.08);
            sideAlpha = IsLight ? 0.98 : 0.78;
            playerAlpha = IsLight ? 1.0 : 0.91;
        }
        else
        {
            sideColor = Color.Parse(IsLight ? "#F0F0F5" : "#0A0A0D");
            playerColor = Color.Parse(IsLight ? "#FBFBFD" : "#0C0C11");
            sideAlpha = IsLight ? 0.98 : 0.78;
            playerAlpha = IsLight ? 1.0 : 0.91;
        }

        SidebarPane.Background = new SolidColorBrush(sideColor, sideAlpha);
        PlayerBar.Background = new SolidColorBrush(playerColor, playerAlpha);
        // 播放列表与加歌抽屉：和播放栏旁的胶囊按钮保持同一种半透明白玻璃质感，
        // 不再使用由侧边栏派生的灰蓝色面板
        var panelBrush = IsLight
            ? new SolidColorBrush(Color.Parse("#F2F2F7"), 0.97)
            : new SolidColorBrush(Colors.White, 0.10);
        QueuePanel.Background = panelBrush;
        PickerPanel.Background = panelBrush;

        if (useMaterial)
        {
            TransparencyLevelHint = material switch
            {
                "mica" => new[]
                {
                    WindowTransparencyLevel.Mica,
                    WindowTransparencyLevel.AcrylicBlur,
                    WindowTransparencyLevel.Blur,
                    WindowTransparencyLevel.None
                },
                "acrylic" or "liquid" => new[]
                {
                    WindowTransparencyLevel.AcrylicBlur,
                    WindowTransparencyLevel.Blur,
                    WindowTransparencyLevel.None
                },
                _ => new[] { WindowTransparencyLevel.Blur, WindowTransparencyLevel.None }
            };

            Background = Brushes.Transparent;
            RootGrid.Background = new SolidColorBrush(coverBase, 0.45 + 0.55 * opacity);

            if (announceMaterial)
            {
                Dispatcher.UIThread.Post(
                    () => StatusText.Text = $"窗口材质：{ActualTransparencyLevel}",
                    DispatcherPriority.Background);
            }
        }
        else
        {
            TransparencyLevelHint = new[] { WindowTransparencyLevel.None };
            Background = new SolidColorBrush(coverBase);
            RootGrid.Background = new SolidColorBrush(coverBase);
        }

        // 系统标题栏与边框取同一底色，避免顶部出现割裂感
        _windowBaseColor = coverBase;
        WindowChrome.ApplyTitleBar(this, !IsLight, coverBase);
    }

    static Color BlendColor(Color baseColor, Color overlay, double amount)
    {
        var r = (byte)Math.Round(baseColor.R + (overlay.R - baseColor.R) * amount);
        var g = (byte)Math.Round(baseColor.G + (overlay.G - baseColor.G) * amount);
        var b = (byte)Math.Round(baseColor.B + (overlay.B - baseColor.B) * amount);
        return Color.FromArgb(255, r, g, b);
    }

    void ApplyTheme(string theme)
    {
        _config.Theme = theme;
        _config.Save();

        var variant = theme == "light" ? ThemeVariant.Light : ThemeVariant.Dark;
        if (Application.Current is not null) Application.Current.RequestedThemeVariant = variant;

        ApplyBackground();
        UpdateThemeChrome();
        UpdatePlaybackModeUi();
        RefreshNowPlayingText();

        if (NowPlayingOverlay.IsVisible && _currentTrack is not null)
        {
            _ = RefreshOverlayAsync(_currentTrack);
        }

        ShowSettingsPage();
        StatusText.Text = theme == "light" ? "已切换到浅色模式" : "已切换到深色模式";
    }

    void UpdateThemeChrome()
    {
        WindowChrome.ApplyTitleBar(this, !IsLight, _windowBaseColor);
        OverlayBackdrop.Opacity = IsLight ? 0.16 : 0.3;
    }

    static string QualityLabel(Track track)
    {
        if (string.IsNullOrWhiteSpace(track.Format)) return string.Empty;

        var format = track.Format.ToUpperInvariant();
        if (track.BitDepth > 0 && track.SampleRate > 0)
        {
            return $"{format} {track.BitDepth}bit/{track.SampleRate / 1000.0:0.#}kHz";
        }
        if (track.SampleRate > 0)
        {
            return $"{format} {track.SampleRate / 1000.0:0.#}kHz";
        }
        return format;
    }

    string BuildNowSub(Track track)
    {
        var text = string.IsNullOrWhiteSpace(track.AlbumName) ? track.Artist : $"{track.Artist} · {track.AlbumName}";
        if (!_config.ShowAudioQuality) return text;

        var quality = QualityLabel(track);
        return string.IsNullOrEmpty(quality) ? text : $"{text} · {quality}";
    }

    void RefreshNowPlayingText()
    {
        if (_currentTrack is null) return;

        var sub = BuildNowSub(_currentTrack);
        NowSub.Text = sub;
        if (NowPlayingOverlay.IsVisible) OverlaySub.Text = sub;
    }

    TextBox MakeFormBox(string text, string placeholder)
    {
        var box = new TextBox
        {
            Text = text,
            PlaceholderText = placeholder,
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        box.Classes.Add("form");
        return box;
    }

    Button MakeFormButton(string label, bool primary = false)
    {
        var button = new Button
        {
            Content = label,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        button.Classes.Add(primary ? "primary" : "ghost");
        return button;
    }

    string EnsureDeviceId()
    {
        if (string.IsNullOrWhiteSpace(_config.DeviceId))
        {
            _config.DeviceId = MusicApiClient.NewDeviceId();
            _config.Save();
        }
        return _config.DeviceId;
    }

    async Task PasswordLoginAsync(string? server, string? username, string? password, TextBlock status)
    {
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            status.Text = "请填写服务器地址、用户名和密码";
            return;
        }

        try
        {
            status.Text = "登录中…";
            var device = EnsureDeviceId();
            using var api = new MusicApiClient(server.Trim(), null, device);
            var token = await api.LoginWithPasswordAsync(username.Trim(), password);
            ApplySession(server.Trim(), token, username.Trim(), device, "登录成功");
        }
        catch (Exception ex)
        {
            status.Text = $"登录失败：{ex.Message}";
        }
    }

    async Task StartOAuthAsync(string? server, TextBlock status)
    {
        try
        {
            status.Text = "正在打开授权页…";
            var device = EnsureDeviceId();
            using var api = new MusicApiClient(server?.Trim() ?? _config.BaseUrl, null, device);
            var url = await api.BuildOAuthUrlAsync();
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            status.Text = "已在浏览器打开授权页";
        }
        catch (Exception ex)
        {
            status.Text = $"发起授权失败：{ex.Message}";
        }
    }

    async Task SubmitOAuthAsync(string? server, string? input, TextBlock status)
    {
        var code = ExtractCode(input ?? string.Empty);
        if (string.IsNullOrWhiteSpace(code))
        {
            status.Text = "请粘贴授权码或回跳地址";
            return;
        }

        try
        {
            status.Text = "授权登录中…";
            var device = EnsureDeviceId();
            using var api = new MusicApiClient(server?.Trim() ?? _config.BaseUrl, null, device);
            var token = await api.LoginWithOAuthCodeAsync(code);
            ApplySession(server?.Trim() ?? _config.BaseUrl, token, _config.LastUsername, device, "授权登录成功");
        }
        catch (Exception ex)
        {
            status.Text = $"授权失败：{ex.Message}";
        }
    }

    void ApplySession(string server, string token, string? username, string device, string message)
    {
        _config.BaseUrl = server;
        _config.Token = token;
        _config.LastUsername = username;
        _config.DeviceId = device;
        _config.Save();

        _api = null;
        ClearCoverCaches();
        _favorites.Clear();

        ShowSettingsPage(message);
        _ = RefreshFavoriteGuidsAsync();
        _ = RefreshSidebarPlaylistsAsync();
        StatusText.Text = message;
    }

    void Logout()
    {
        _config.Token = null;
        _config.Save();

        _api = null;
        ClearCoverCaches();
        _favorites.Clear();
        SidebarPlaylists.Children.Clear();
        PlaylistCreateBar.IsVisible = false;

        ShowSettingsPage("已退出登录");
        StatusText.Text = "已退出登录";
    }

    static string ExtractCode(string input)
    {
        var index = input.IndexOf("code=", StringComparison.Ordinal);
        if (index < 0) return input.Trim();

        var code = input[(index + 5)..];
        var end = code.IndexOf('&');
        if (end >= 0) code = code[..end];
        return Uri.UnescapeDataString(code);
    }

    MusicApiClient Api() => _api ??= CreateClient();

    MusicApiClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_config.DeviceId))
        {
            _config.DeviceId = MusicApiClient.NewDeviceId();
            _config.Save();
        }

        // 播放代理与业务请求共用同一个客户端，复用连接池，避免重复的 HttpClient 与套接字
        var client = new MusicApiClient(_config.BaseUrl, _config.Token, _config.DeviceId);

        _proxy?.Dispose();
        _proxy = new StreamProxy(client);
        _proxy.Start();

        return client;
    }

    bool HandleError(Exception ex, string what)
    {
        if (ex is MusicApiException apiEx && apiEx.Code == 99999)
        {
            ShowSettingsPage("登录已失效，请重新登录");
            StatusText.Text = "登录已失效，请重新登录";
            return true;
        }

        StatusText.Text = $"{what}: {ex.Message}";
        return false;
    }

}
