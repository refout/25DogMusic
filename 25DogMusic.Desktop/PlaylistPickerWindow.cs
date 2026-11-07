using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using FnMusic.Api;
using FluentIcons.Avalonia;
using FluentIcons.Common;

namespace _25DogMusic.Desktop;

/// <summary>「加入歌单」选择框：点击现有歌单直接加入，或新建后再加入。</summary>
public sealed class PlaylistPickerWindow : Window
{
    static bool IsLight => Application.Current?.RequestedThemeVariant == ThemeVariant.Light;
    static IBrush TextPrimary => new SolidColorBrush(Color.Parse(IsLight ? "#1C1C1E" : "#F2F2F5"));
    static IBrush TextSecondary => new SolidColorBrush(Color.Parse(IsLight ? "#54545C" : "#8E8E93"));
    static IBrush CardBg => new SolidColorBrush(Color.Parse(IsLight ? "#F0F0F5" : "#14FFFFFF"));

    readonly MusicApiClient _api;
    readonly string _trackGuid;
    readonly StackPanel _listPanel = new() { Spacing = 4 };
    readonly TextBox _nameBox = new() { PlaceholderText = "新歌单名称", FontSize = 13 };
    readonly TextBlock _status = new() { FontSize = 12, Foreground = TextSecondary, TextWrapping = TextWrapping.Wrap };

    public string? SelectedGuid { get; private set; }

    public PlaylistPickerWindow(MusicApiClient api, Track track)
    {
        _api = api;
        _trackGuid = track.Guid;

        Title = "加入歌单";
        Width = 420;
        Height = 520;
        CanResize = false;
        Background = new SolidColorBrush(Color.Parse(IsLight ? "#F5F5F9" : "#141419"));
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        Content = BuildLayout(track.Title);
        Opened += async (_, _) => await ReloadAsync();
    }

    Control BuildLayout(string trackTitle)
    {
        var root = new Grid
        {
            Margin = new Thickness(22),
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            RowSpacing = 14
        };

        var head = new StackPanel { Spacing = 4 };
        head.Children.Add(new TextBlock { Text = "加入歌单", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = TextPrimary });
        head.Children.Add(new TextBlock
        {
            Text = trackTitle,
            FontSize = 12,
            Foreground = TextSecondary,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        root.Children.Add(head);

        var scroll = new ScrollViewer { Content = _listPanel, VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto };
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        var footer = new StackPanel { Spacing = 8 };
        Grid.SetRow(footer, 2);

        var createRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        createRow.Children.Add(_nameBox);

        var createButton = new Button { Content = "新建", Width = 64, Margin = new Thickness(8, 0, 0, 0) };
        Grid.SetColumn(createButton, 1);
        createRow.Children.Add(createButton);
        footer.Children.Add(createRow);
        footer.Children.Add(_status);
        root.Children.Add(footer);

        _nameBox.KeyDown += async (_, e) =>
        {
            if (e.Key != Avalonia.Input.Key.Enter) return;
            e.Handled = true;
            await CreateAsync();
        };
        createButton.Click += async (_, _) => await CreateAsync();
        return root;
    }

    async Task ReloadAsync()
    {
        try
        {
            _listPanel.Children.Clear();
            using var doc = await _api.GetPlaylistsAsync();
            var playlists = Library.ParsePlaylists(doc);

            if (playlists.Count == 0)
            {
                _status.Text = "还没有歌单，先新建一个";
                return;
            }

            foreach (var playlist in playlists)
            {
                _listPanel.Children.Add(BuildPlaylistRow(playlist));
            }

            _status.Text = $"共 {playlists.Count} 个歌单";
        }
        catch (Exception ex)
        {
            _status.Text = $"加载歌单失败: {ex.Message}";
        }
    }

    Control BuildPlaylistRow(Playlist playlist)
    {
        var row = new Button
        {
            Background = CardBg,
            CornerRadius = new CornerRadius(9),
            Padding = new Thickness(14, 11),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 12,
                Children =
                {
                    new SymbolIcon
                    {
                        Symbol = Symbol.TextBulletList,
                        FontSize = 16,
                        Foreground = TextSecondary,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new TextBlock { Text = playlist.Name, FontSize = 14, Foreground = TextPrimary, VerticalAlignment = VerticalAlignment.Center },
                    new TextBlock
                    {
                        Text = $"{playlist.TrackCount} 首",
                        FontSize = 11,
                        Foreground = TextSecondary,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(6, 0, 0, 0)
                    }
                }
            }
        };

        row.Click += (_, _) =>
        {
            SelectedGuid = playlist.Guid;
            Close();
        };

        return row;
    }

    async Task CreateAsync()
    {
        var name = _nameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            _status.Text = "请先填写歌单名称";
            return;
        }

        try
        {
            _status.Text = "创建中…";
            using var doc = await _api.CreatePlaylistAsync(name);

            var guid = string.Empty;
            if (doc.RootElement.TryGetProperty("data", out var data) && data.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                guid = Library.GetString(data, "guid");
                if (string.IsNullOrEmpty(guid) && data.TryGetProperty("playlist", out var p))
                {
                    guid = Library.GetString(p, "guid");
                }
            }

            if (string.IsNullOrEmpty(guid))
            {
                using var listDoc = await _api.GetPlaylistsAsync();
                guid = Library.ParsePlaylists(listDoc)
                    .FirstOrDefault(p => p.Name == name)?.Guid ?? string.Empty;
            }

            if (string.IsNullOrEmpty(guid))
            {
                _status.Text = "歌单已创建，但未能取回标识，请重新打开后再试";
                await ReloadAsync();
                return;
            }

            SelectedGuid = guid;
            Close();
        }
        catch (Exception ex)
        {
            _status.Text = $"创建失败: {ex.Message}";
        }
    }
}
