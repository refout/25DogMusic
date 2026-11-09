using System.Diagnostics;
using System.Text;
using System.Text.Json;
using FnMusic.Api;
using FnMusic.Auth;
using FnMusic.Config;

var cfg = AppConfig.Load();
if (string.IsNullOrWhiteSpace(cfg.DeviceId))
{
    cfg.DeviceId = MusicApiClient.NewDeviceId();
    cfg.Save();
}

if (args.Length == 0)
{
    RunTui(cfg);
    return;
}

using var api = new MusicApiClient(cfg.BaseUrl, cfg.Token, cfg.DeviceId);

var command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";

switch (command)
{
    case "probe":
        await ProbeAsync(api);
        break;

    case "login":
        await LoginPasswordAsync(api, cfg);
        break;

    case "oauth":
        await LoginOAuthAsync(api, cfg);
        break;

    case "me":
        await PrintAsync(api.GetCurrentUserAsync());
        break;

    case "albums":
    {
        var page = args.Length > 1 ? int.Parse(args[1]) : 1;
        await PrintAsync(api.GetAlbumsAsync(page));
        break;
    }

    case "search":
        if (args.Length < 2) { Console.WriteLine("用法: fnmusic search <关键词>"); return; }
        await PrintAsync(api.SearchTracksAsync(args[1]));
        break;

    case "tracks":
    {
        using var doc = await api.GetTracksAsync();
        var tracks = Library.ParseTracks(doc);
        for (var i = 0; i < tracks.Count; i++)
        {
            Console.WriteLine($"{i + 1,3}. {tracks[i].Title}  —  {tracks[i].Artist}  [{tracks[i].AlbumName}]");
        }
        Console.WriteLine($"共 {tracks.Count} 首");
        break;
    }

    case "api":
    {
        var type = typeof(Terminal.Gui.View);
        foreach (var m in type.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly)
                     .Where(m => m.Name.Contains("Needs"))
                     .OrderBy(m => m.Name))
        {
            Console.WriteLine($"METHOD {(m.IsPublic ? "public" : "nonpublic")} {m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name))})");
        }

        var ctx = typeof(Terminal.Gui.DrawContext);
        Console.WriteLine("--- DrawContext ---");
        foreach (var p in ctx.GetProperties().OrderBy(p => p.Name))
        {
            Console.WriteLine($"PROP {p.PropertyType.Name} {p.Name}");
        }
        foreach (var f in ctx.GetFields().OrderBy(f => f.Name))
        {
            Console.WriteLine($"FIELD {f.FieldType.Name} {f.Name}");
        }
        break;
    }

    case "theme":
    {
        if (args.Length < 2) { Console.WriteLine("用法: fnmusic theme <coverId>"); return; }
        var bytes = await api.GetCoverAsync(args[1], 300);
        var image = FnMusic.Tui.RgbaImage.TryDecode(bytes);
        if (image is null) { Console.WriteLine("封面解码失败"); return; }
        var theme = FnMusic.Tui.ColorTheme.FromImage(image);
        Console.WriteLine($"bg={theme.Background.R},{theme.Background.G},{theme.Background.B}");
        Console.WriteLine($"panel={theme.Panel.R},{theme.Panel.G},{theme.Panel.B}");
        Console.WriteLine($"text={theme.Text.R},{theme.Text.G},{theme.Text.B}");
        Console.WriteLine($"dim={theme.Dim.R},{theme.Dim.G},{theme.Dim.B}");
        Console.WriteLine($"accent={theme.Accent.R},{theme.Accent.G},{theme.Accent.B}");
        Console.WriteLine("palette=" + string.Join(" | ", theme.Palette.Select(c => $"{c.R},{c.G},{c.B}")));
        break;
    }

    case "render":
    {
        if (args.Length < 2) { Console.WriteLine("用法: fnmusic render <coverId>"); return; }
        var bytes = await api.GetCoverAsync(args[1], 32);
        var image = FnMusic.Tui.RgbaImage.TryDecode(bytes);
        if (image is null) { Console.WriteLine("封面解码失败"); return; }
        Console.WriteLine($"cover {image.Width}x{image.Height}, {bytes.Length} bytes");
        var small = image.Resize(24, 12);
        for (var y = 0; y < small.Height; y++)
        {
            var line = new System.Text.StringBuilder();
            for (var x = 0; x < small.Width; x++)
            {
                var p = small[x, y];
                var lum = (p.R * 3 + p.G * 6 + p.B) / 10;
                line.Append(lum switch { > 220 => '@', > 170 => '#', > 120 => '*', > 70 => '.', _ => ' ' });
            }
            Console.WriteLine(line.ToString());
        }
        break;
    }

    case "lyrics":
    {
        if (args.Length < 2) { Console.WriteLine("用法: fnmusic lyrics <trackGuid>"); return; }
        var lines = await api.GetLyricsAsync(args[1]);
        Console.WriteLine($"歌词 {lines.Count} 行");
        foreach (var line in lines.Take(8))
        {
            Console.WriteLine($"[{line.Time:mm\\:ss\\.ff}] {line.Text}");
        }
        break;
    }

    case "leveltest":
    {
        if (args.Length < 2) { Console.WriteLine("用法: fnmusic leveltest <trackGuid>"); return; }
        using var proxy = new FnMusic.Playback.StreamProxy(api);
        proxy.Start();
        using var player = new FnMusic.Playback.VlcPlayer();
        using var monitor = new FnMusic.Playback.AudioLevelMonitor();
        Console.WriteLine($"loopback available={monitor.Available}");
        player.Play(proxy.BaseUrl + args[1]);
        await Task.Delay(1200);
        for (var i = 0; i < 10; i++)
        {
            var bands = monitor.GetBands();
            Console.WriteLine($"level={monitor.Level:0.00} bands=[{string.Join(",", bands.Select(b => b.ToString("0.0")))}]");
            await Task.Delay(250);
        }
        break;
    }

    case "pwlogin":
    {
        if (args.Length < 3) { Console.WriteLine("用法: fnmusic pwlogin <用户名> <密码>"); return; }
        var token = await api.LoginWithPasswordAsync(args[1], args[2]);
        cfg.Token = token;
        cfg.LastUsername = args[1];
        cfg.Save();
        Console.WriteLine($"登录成功 token={token}");
        break;
    }

    case "report":
    {
        if (args.Length < 2) { Console.WriteLine("用法: fnmusic report <trackGuid>"); return; }
        using var doc = await api.ReportTrackPlayAsync(args[1]);
        Console.WriteLine(doc.RootElement.GetRawText());
        break;
    }

    case "favorites":
    {
        using var doc = await api.GetFavoritesAsync();
        Console.WriteLine($"收藏 {doc.RootElement.GetProperty("data").GetProperty("total").GetInt32()} 首");
        break;
    }

    case "fav":
    {
        if (args.Length < 2) { Console.WriteLine("用法: fnmusic fav <trackGuid>"); return; }
        using var doc = await api.AddFavoriteAsync(args[1]);
        Console.WriteLine(doc.RootElement.GetRawText());
        break;
    }

    case "unfav":
    {
        if (args.Length < 2) { Console.WriteLine("用法: fnmusic unfav <trackGuid>"); return; }
        using var doc = await api.RemoveFavoriteAsync(args[1]);
        Console.WriteLine(doc.RootElement.GetRawText());
        break;
    }

    case "playlists":
    {
        using var doc = await api.GetPlaylistsAsync();
        var s = doc.RootElement.GetRawText();
        Console.WriteLine(s.Length > 200 ? s[..200] : s);
        break;
    }

    case "plcreate":
    {
        if (args.Length < 2) { Console.WriteLine("用法: fnmusic plcreate <名称>"); return; }
        using var doc = await api.CreatePlaylistAsync(args[1]);
        Console.WriteLine(doc.RootElement.GetRawText());
        break;
    }

    case "pladd":
    {
        if (args.Length < 3) { Console.WriteLine("用法: fnmusic pladd <playlistGuid> <trackGuid>"); return; }
        using var doc = await api.AddTrackToPlaylistAsync(args[1], args[2]);
        Console.WriteLine(doc.RootElement.GetRawText());
        break;
    }

    case "salbums":
    {
        if (args.Length < 2) { Console.WriteLine("用法: fnmusic salbums <关键词>"); return; }
        using var doc = await api.SearchAlbumsAsync(args[1]);
        Console.WriteLine(doc.RootElement.GetRawText()[..150]);
        break;
    }

    case "proxytest":
    {
        if (args.Length < 2) { Console.WriteLine("用法: fnmusic proxytest <trackGuid>"); return; }
        using var proxy = new FnMusic.Playback.StreamProxy(api);
        proxy.Start();
        using var http = new HttpClient();
        using var req = new HttpRequestMessage(HttpMethod.Get, proxy.BaseUrl + args[1]);
        req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 1023);
        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        Console.WriteLine($"status={(int)resp.StatusCode} type={resp.Content.Headers.ContentType} len={resp.Content.Headers.ContentLength}");
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        Console.WriteLine($"read={bytes.Length} bytes, head={Convert.ToHexStringLower(bytes.AsSpan(0, 4))}");
        break;
    }

    case "playtest":
    {
        if (args.Length < 2) { Console.WriteLine("用法: fnmusic playtest <trackGuid>"); return; }
        using var proxy = new FnMusic.Playback.StreamProxy(api);
        proxy.Start();
        using var player = new FnMusic.Playback.VlcPlayer();
        player.Play(proxy.BaseUrl + args[1]);
        await Task.Delay(6000);
        Console.WriteLine($"isPlaying={player.IsPlaying} time={player.Position}ms length={player.Duration}ms");
        break;
    }

    case "sign":
        // 便于与浏览器抓到的 authx 对比：fnmusic sign <path> <body>
        if (args.Length < 3) { Console.WriteLine("用法: fnmusic sign <path> <body>"); return; }
        Console.WriteLine(AuthSigner.CreateAuthX(args[1], args[2], isGet: false));
        break;

    case "token":
        Console.WriteLine(api.Token ?? "(未登录)");
        break;

    default:
        PrintHelp();
        break;
}

static void RunTui(AppConfig cfg)
{
    Terminal.Gui.Application.Init();
    var api = new MusicApiClient(cfg.BaseUrl, cfg.Token, cfg.DeviceId);
    var window = new FnMusic.Tui.PlayerWindow(cfg, api);
    Terminal.Gui.Application.Run(window);
    Terminal.Gui.Application.Shutdown();
}

static void PrintHelp()
{
    Console.WriteLine("""
        飞牛音乐 TUI 客户端（M0 命令行探针）

        fnmusic probe              探测服务器配置与初始化状态
        fnmusic login              账号密码登录（密码输入不显示）
        fnmusic oauth              浏览器授权登录（粘贴 code）
        fnmusic me                 当前登录用户
        fnmusic albums [page]      专辑列表
        fnmusic search <关键词>     搜索歌曲
        fnmusic sign <path> <body> 输出本地计算的 authx（用于和浏览器对比）
        fnmusic token              打印已保存的 token

        配置文件: %LOCALAPPDATA%\25DogMusic\config.json
        """);
}

static async Task ProbeAsync(MusicApiClient api)
{
    using var sys = await api.GetAsync("/sys/config");
    using var state = await api.GetAsync("/initialization/state");
    Console.WriteLine(JsonSerializer.Serialize(sys.RootElement, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine(JsonSerializer.Serialize(state.RootElement, new JsonSerializerOptions { WriteIndented = true }));
}

static async Task LoginPasswordAsync(MusicApiClient api, AppConfig cfg)
{
    Console.Write("用户名: ");
    var user = Console.ReadLine()?.Trim() ?? string.Empty;
    Console.Write("密码: ");
    var pass = ReadSecret();

    var token = await api.LoginWithPasswordAsync(user, pass);
    cfg.Token = token;
    cfg.LastUsername = user;
    cfg.Save();
    Console.WriteLine($"登录成功，token={token}");
    Console.WriteLine($"配置文件: {AppConfig.FilePath}");
}

static async Task LoginOAuthAsync(MusicApiClient api, AppConfig cfg)
{
    var url = await api.BuildOAuthUrlAsync();
    Console.WriteLine("即将打开浏览器完成飞牛账号授权：");
    Console.WriteLine(url);
    Console.WriteLine();

    try
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"自动打开浏览器失败，请手动访问上面的地址（{ex.Message}）");
    }

    Console.WriteLine("登录成功后浏览器会跳到 .../music/oauth/result?code=xxxx，");
    Console.WriteLine("把整段地址或 code 粘贴到下面：");
    var input = Console.ReadLine()?.Trim() ?? string.Empty;
    if (input.Length == 0) { Console.WriteLine("已取消"); return; }

    var code = ExtractCode(input);
    var token = await api.LoginWithOAuthCodeAsync(code);
    cfg.Token = token;
    cfg.Save();
    Console.WriteLine($"授权登录成功，token={token}");
}

static string ExtractCode(string input)
{
    if (!input.Contains('=')) return input.Trim();

    try
    {
        var uri = new Uri(input);
        var query = uri.Query.TrimStart('?');
        foreach (var pair in query.Split('&'))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length == 2 && (kv[0] is "code" or "authCode"))
            {
                return Uri.UnescapeDataString(kv[1]);
            }
        }
    }
    catch
    {
        // 不是完整 URL，按裸 code 处理
    }

    return input.Trim();
}

static string ReadSecret()
{
    var sb = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter) { Console.WriteLine(); break; }
        if (key.Key == ConsoleKey.Backspace)
        {
            if (sb.Length > 0) sb.Length--;
            continue;
        }
        if (!char.IsControl(key.KeyChar)) sb.Append(key.KeyChar);
    }
    return sb.ToString();
}

static async Task PrintAsync(Task<JsonDocument> task)
{
    using var doc = await task;
    Console.WriteLine(JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    }));
}
