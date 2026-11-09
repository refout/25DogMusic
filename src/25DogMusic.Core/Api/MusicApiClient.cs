using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using _25DogMusic.Core.Auth;

namespace _25DogMusic.Core.Api;

/// <summary>接口返回非 0 code 或响应不是 JSON 时抛出。</summary>
public sealed class MusicApiException(int code, string message) : Exception($"接口返回 code={code}: {message}")
{
    public int Code { get; } = code;
}

/// <summary>直连飞牛音乐服务器（/music/api/v1）。</summary>
public sealed class MusicApiClient : IDisposable
{
    public const string ApiPrefix = "/music/api/v1";

    readonly HttpClient _http;
    readonly string _baseUrl;
    readonly string _host;

    public string? Token { get; private set; }
    public string DeviceId { get; }
    public string BaseUrl => _baseUrl;

    public MusicApiClient(string baseUrl, string? token = null, string? deviceId = null)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _host = new Uri(_baseUrl).Host;
        DeviceId = string.IsNullOrWhiteSpace(deviceId) ? NewDeviceId() : deviceId;
        Token = token;

        var handler = new HttpClientHandler
        {
            // 仅放行目标 NAS 的自签证书
            ServerCertificateCustomValidationCallback = (msg, _, _, _) =>
                msg.RequestUri is not null && msg.RequestUri.Host.Equals(_host, StringComparison.OrdinalIgnoreCase)
        };
        _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        // 优先 HTTP/2，写接口可能只在 h2 下放行
        _http.DefaultRequestVersion = HttpVersion.Version20;
        _http.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        var assemblyVersion = typeof(MusicApiClient).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        _http.DefaultRequestHeaders.UserAgent.ParseAdd($"25DogMusic/{assemblyVersion}");
    }

    public static string NewDeviceId()
    {
        Span<byte> buf = stackalloc byte[16];
        Random.Shared.NextBytes(buf);
        return Convert.ToHexStringLower(buf);
    }

    public async Task<JsonDocument> GetAsync(string path, IDictionary<string, string?>? query = null, CancellationToken ct = default)
    {
        var fullPath = ApiPrefix + path;
        var q = query is null ? string.Empty : AuthSigner.BuildQuery(query);
        var url = _baseUrl + fullPath + (q.Length > 0 ? "?" + q : string.Empty);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("authx", AuthSigner.CreateAuthX(fullPath, q, isGet: true));
        AddToken(request);
        return await SendAsync(request, ct);
    }

    public async Task<JsonDocument> PostAsync(string path, JsonNode body, CancellationToken ct = default)
    {
        var fullPath = ApiPrefix + path;
        var json = body.ToJsonString();

        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, _baseUrl + fullPath)
        {
            Content = content
        };
        request.Headers.TryAddWithoutValidation("authx", AuthSigner.CreateAuthX(fullPath, json, isGet: false));
        AddToken(request);
        return await SendAsync(request, ct);
    }

    void AddToken(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(Token))
        {
            request.Headers.TryAddWithoutValidation("Cookie", $"music-token={Token}");
        }
    }

    async Task<JsonDocument> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        // Set-Cookie 里也可能下发 token
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookie in cookies)
            {
                var match = System.Text.RegularExpressions.Regex.Match(cookie, @"music-token=([^;]+)");
                if (match.Success)
                {
                    Token = Uri.UnescapeDataString(match.Groups[1].Value);
                }
            }
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            throw new MusicApiException(-1, $"非 JSON 响应（HTTP {(int)response.StatusCode}）: {Truncate(text)}");
        }

        var root = doc.RootElement;
        if (root.TryGetProperty("code", out var codeEl) && codeEl.ValueKind == JsonValueKind.Number && codeEl.GetInt32() != 0)
        {
            var msg = root.TryGetProperty("msg", out var m) ? m.GetString() ?? string.Empty : string.Empty;
            var c = codeEl.GetInt32();
            doc.Dispose();
            throw new MusicApiException(c, msg);
        }

        return doc;
    }

    static string Truncate(string s) => s.Length <= 200 ? s : s[..200];

    // ---- 登录：账号密码 ----

    public async Task<string> LoginWithPasswordAsync(string username, string password, CancellationToken ct = default)
    {
        var hashed = AuthSigner.Sha256Hex(password);
        var body = new JsonObject
        {
            ["username"] = username,
            ["password"] = hashed,
            ["deviceId"] = DeviceId
        };

        using var doc = await PostAsync("/user/password-login", body, ct);
        return CaptureToken(doc);
    }

    // ---- 登录：NAS OAuth 授权码 ----

    /// <summary>
    /// 生成 NAS 授权页地址。传入 redirectUri 时使用回环地址，否则回跳到音乐网页的 /oauth/result。
    /// </summary>
    public async Task<string> BuildOAuthUrlAsync(string? redirectUri = null, CancellationToken ct = default)
    {
        using var doc = await GetAsync("/sys/config", null, ct);
        var data = doc.RootElement.GetProperty("data");

        var clientId = data.TryGetProperty("nasOAuth", out var oauth) && oauth.TryGetProperty("clientId", out var cid)
            ? cid.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException("服务端未返回 nasOAuth.clientId，无法发起授权登录");
        }

        var origin = _baseUrl;
        if (data.TryGetProperty("nasOAuth", out var o) && o.TryGetProperty("url", out var u) && !string.IsNullOrWhiteSpace(u.GetString()))
        {
            origin = u.GetString()!.TrimEnd('/');
        }

        var redirect = Uri.EscapeDataString(redirectUri ?? origin + "/music/oauth/result");
        var appName = Uri.EscapeDataString("飞牛音乐");
        return $"{origin}/signin?client_id={Uri.EscapeDataString(clientId)}&redirect_uri={redirect}&app_name={appName}";
    }

    public async Task<string> LoginWithOAuthCodeAsync(string code, CancellationToken ct = default)
    {
        var body = new JsonObject
        {
            ["code"] = code,
            ["deviceId"] = DeviceId
        };

        using var doc = await PostAsync("/user/auth-login", body, ct);
        return CaptureToken(doc);
    }

    /// <summary>从登录响应里取 userToken（兼容 data.userToken / data.token / Set-Cookie）。</summary>
    string CaptureToken(JsonDocument doc)
    {
        if (doc.RootElement.TryGetProperty("data", out var data))
        {
            foreach (var key in new[] { "userToken", "token", "accessToken" })
            {
                if (data.TryGetProperty(key, out var el) && el.ValueKind == JsonValueKind.String)
                {
                    Token = el.GetString();
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(Token))
        {
            throw new InvalidOperationException("登录成功但响应里没有 userToken");
        }

        return Token;
    }

    // ---- 常用查询 ----

    // ---- 收藏 / 歌单 / 分类搜索 ----

    public async Task<JsonDocument> GetFavoritesAsync(int page = 1, int pageSize = 200, CancellationToken ct = default)
        => await GetAsync("/favorite-track/list", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        }, ct);

    public Task<JsonDocument> AddFavoriteAsync(string trackGuid, CancellationToken ct = default)
        => PostAsync("/favorite-track/create", new JsonObject { ["trackGUID"] = trackGuid }, ct);

    public Task<JsonDocument> RemoveFavoriteAsync(string trackGuid, CancellationToken ct = default)
        => PostAsync("/favorite-track/delete", new JsonObject { ["trackGUID"] = trackGuid }, ct);

    public async Task<JsonDocument> GetPlaylistsAsync(int page = 1, int pageSize = 200, CancellationToken ct = default)
        => await GetAsync("/playlist/list", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        }, ct);

    public async Task<JsonDocument> GetPlaylistTracksAsync(string playlistGuid, int page = 1, int size = 200, CancellationToken ct = default)
        => await GetAsync("/track/playlist-detail/list", new Dictionary<string, string?>
        {
            ["playlistGUID"] = playlistGuid,
            ["page"] = page.ToString(),
            ["size"] = size.ToString()
        }, ct);

    public async Task<JsonDocument> GetPlaylistDetailAsync(string playlistGuid, CancellationToken ct = default)
        => await GetAsync("/playlist/detail", new Dictionary<string, string?>
        {
            ["guid"] = playlistGuid
        }, ct);

    public Task<JsonDocument> EditPlaylistAsync(string playlistGuid, string name, CancellationToken ct = default)
        => PostAsync("/playlist/edit", new JsonObject
        {
            ["guid"] = playlistGuid,
            ["name"] = name
        }, ct);

    public Task<JsonDocument> DeletePlaylistAsync(string playlistGuid, CancellationToken ct = default)
        => PostAsync("/playlist/delete", new JsonObject { ["guid"] = playlistGuid }, ct);

    /// <summary>共享库（音乐根目录）列表。</summary>
    public async Task<JsonDocument> GetSharedLibrariesAsync(int page = 1, int pageSize = 200, CancellationToken ct = default)
        => await GetAsync("/shared-library/list", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["size"] = pageSize.ToString()
        }, ct);

    /// <summary>文件夹视图：返回 parentPath 下的子目录与歌曲。</summary>
    public async Task<JsonDocument> GetFolderViewAsync(string parentPath, string sort = "title,asc", CancellationToken ct = default)
        => await GetAsync("/folder-view/list", new Dictionary<string, string?>
        {
            ["parentPath"] = parentPath,
            ["sort"] = sort
        }, ct);

    public Task<JsonDocument> CreatePlaylistAsync(string name, CancellationToken ct = default)
        => PostAsync("/playlist/create", new JsonObject { ["name"] = name }, ct);

    public Task<JsonDocument> AddTrackToPlaylistAsync(string playlistGuid, string trackGuid, CancellationToken ct = default)
        => PostAsync("/playlist/add-track", new JsonObject
        {
            ["guid"] = playlistGuid,
            ["trackGUIDs"] = new JsonArray(JsonValue.Create(trackGuid))
        }, ct);

    public Task<JsonDocument> RemoveTrackFromPlaylistAsync(string playlistGuid, string trackGuid, CancellationToken ct = default)
        => PostAsync("/playlist/remove-track", new JsonObject
        {
            ["guid"] = playlistGuid,
            ["trackGUIDs"] = new JsonArray(JsonValue.Create(trackGuid))
        }, ct);

    public async Task<JsonDocument> SearchAlbumsAsync(string keyword, int page = 1, int pageSize = 30, CancellationToken ct = default)
        => await GetAsync("/search/album", new Dictionary<string, string?>
        {
            ["q"] = keyword,
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        }, ct);

    public async Task<JsonDocument> SearchArtistsAsync(string keyword, int page = 1, int pageSize = 30, CancellationToken ct = default)
        => await GetAsync("/search/artist", new Dictionary<string, string?>
        {
            ["q"] = keyword,
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        }, ct);

    public async Task<JsonDocument> SearchPlaylistsAsync(string keyword, int page = 1, int pageSize = 30, CancellationToken ct = default)
        => await GetAsync("/search/playlist", new Dictionary<string, string?>
        {
            ["q"] = keyword,
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        }, ct);

    public async Task<JsonDocument> GetCurrentUserAsync(CancellationToken ct = default)
        => await GetAsync("/user/me", null, ct);

    public async Task<JsonDocument> GetAlbumsAsync(int page = 1, int pageSize = 50, CancellationToken ct = default)
        => await GetAsync("/album/list", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        }, ct);

    public async Task<JsonDocument> GetArtistsAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
        => await GetAsync("/artist/list", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        }, ct);

    public async Task<JsonDocument> GetTracksByAlbumAsync(string albumGuid, int page = 1, int size = 200, CancellationToken ct = default)
        => await GetAsync("/track/album-detail/list", new Dictionary<string, string?>
        {
            ["albumGUID"] = albumGuid,
            ["page"] = page.ToString(),
            ["size"] = size.ToString()
        }, ct);

    public async Task<JsonDocument> GetTracksByArtistAsync(string artistGuid, int page = 1, int size = 200, CancellationToken ct = default)
        => await GetAsync("/track/artist-detail/list", new Dictionary<string, string?>
        {
            ["artistGUID"] = artistGuid,
            ["page"] = page.ToString(),
            ["size"] = size.ToString()
        }, ct);

    /// <summary>某位歌手的专辑列表。</summary>
    public async Task<JsonDocument> GetAlbumsByArtistAsync(string artistGuid, int page = 1, int size = 100, CancellationToken ct = default)
        => await GetAsync("/album/artist-detail/list", new Dictionary<string, string?>
        {
            ["artistGUID"] = artistGuid,
            ["page"] = page.ToString(),
            ["size"] = size.ToString()
        }, ct);

    public async Task<JsonDocument> GetTracksAsync(int page = 1, int pageSize = 200, string? sort = null, CancellationToken ct = default)
        => await GetAsync("/track/list", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            // 服务端对 track/list 的分页参数是 size（pageSize 会被限制为 50）
            ["size"] = pageSize.ToString(),
            ["pageSize"] = pageSize.ToString(),
            ["sort"] = sort
        }, ct);

    public async Task<JsonDocument> GetGenresAsync(int page = 1, int pageSize = 100, CancellationToken ct = default)
        => await GetAsync("/genre/list", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        }, ct);

    public async Task<JsonDocument> GetTracksByGenreAsync(string genreGuid, int page = 1, int size = 200, CancellationToken ct = default)
        => await GetAsync("/track/genre-detail/list", new Dictionary<string, string?>
        {
            ["genreGUID"] = genreGuid,
            ["page"] = page.ToString(),
            ["size"] = size.ToString()
        }, ct);

    public async Task<JsonDocument> GetPlayHistoryAsync(int page = 1, int pageSize = 200, CancellationToken ct = default)
        => await GetAsync("/play-history/list", new Dictionary<string, string?>
        {
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        }, ct);

    public async Task<JsonDocument> RoamStartAsync(CancellationToken ct = default)
        => await GetAsync("/track/roam-start", new Dictionary<string, string?>
        {
            ["deviceId"] = DeviceId
        }, ct);

    public async Task<JsonDocument> RoamNextAsync(string roamId, CancellationToken ct = default)
        => await GetAsync("/track/roam-next", new Dictionary<string, string?>
        {
            ["deviceId"] = DeviceId,
            ["relativeRoamId"] = roamId
        }, ct);

    public async Task<JsonDocument> RoamPreviousAsync(string roamId, CancellationToken ct = default)
        => await GetAsync("/track/roam-previous", new Dictionary<string, string?>
        {
            ["deviceId"] = DeviceId,
            ["relativeRoamId"] = roamId
        }, ct);

    public async Task<JsonDocument> SearchTracksAsync(string keyword, int page = 1, int pageSize = 50, CancellationToken ct = default)
        => await GetAsync("/search/track", new Dictionary<string, string?>
        {
            ["q"] = keyword,
            ["page"] = page.ToString(),
            ["pageSize"] = pageSize.ToString()
        }, ct);

    /// <summary>构造带鉴权的音频流请求（供本地代理转发）。</summary>
    public HttpRequestMessage CreateStreamRequest(string guid)
    {
        var fullPath = ApiPrefix + "/track/stream";
        var q = AuthSigner.BuildQuery(new Dictionary<string, string?> { ["guid"] = guid });

        var request = new HttpRequestMessage(HttpMethod.Get, _baseUrl + fullPath + "?" + q);
        request.Headers.TryAddWithoutValidation("authx", AuthSigner.CreateAuthX(fullPath, q, isGet: true));
        AddToken(request);
        return request;
    }

    public Task<HttpResponseMessage> SendRawAsync(HttpRequestMessage request, HttpCompletionOption option, CancellationToken ct = default)
        => _http.SendAsync(request, option, ct);

    /// <summary>打开音频流（支持 Range，可交给播放器直接播）。</summary>
    public Task<HttpResponseMessage> OpenStreamAsync(string guid, CancellationToken ct = default)
        => SendRawAsync(CreateStreamRequest(guid), HttpCompletionOption.ResponseHeadersRead, ct);

    public string BuildCoverUrl(string coverId, int? size = null)
    {
        var q = AuthSigner.BuildQuery(new Dictionary<string, string?>
        {
            ["coverId"] = coverId,
            ["size"] = size?.ToString()
        });
        return _baseUrl + ApiPrefix + "/static/cover?" + q;
    }

    public async Task<byte[]> GetCoverAsync(string coverId, int size = 160, CancellationToken ct = default)
    {
        var fullPath = ApiPrefix + "/static/cover";
        var q = AuthSigner.BuildQuery(new Dictionary<string, string?>
        {
            ["coverId"] = coverId,
            ["size"] = size.ToString()
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, _baseUrl + fullPath + "?" + q);
        request.Headers.TryAddWithoutValidation("authx", AuthSigner.CreateAuthX(fullPath, q, isGet: true));
        AddToken(request);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new MusicApiException((int)response.StatusCode, "封面获取失败");
        }

        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<List<LyricLine>> GetLyricsAsync(string trackGuid, CancellationToken ct = default)
    {
        using var doc = await GetAsync("/lyric/list", new Dictionary<string, string?>
        {
            ["trackGUID"] = trackGuid
        }, ct);

        var result = new List<LyricLine>();
        if (!doc.RootElement.TryGetProperty("data", out var data) || !data.TryGetProperty("list", out var list)
            || list.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        var preferred = data.TryGetProperty("preferred", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

        string? chosen = null;
        foreach (var item in list.EnumerateArray())
        {
            var content = Library.GetString(item, "content");
            var guid = Library.GetString(item, "guid");
            if (string.IsNullOrWhiteSpace(content)) continue;

            if (preferred is not null && guid == preferred)
            {
                chosen = content;
                break;
            }

            chosen ??= content;
        }

        return chosen is null ? result : LrcParser.Parse(chosen);
    }

    /// <summary>上报一次播放（官方前端事件：track_play）。</summary>
    public Task<JsonDocument> ReportTrackPlayAsync(string trackGuid, CancellationToken ct = default)
    {
        var body = new JsonObject
        {
            ["events"] = new JsonArray(new JsonObject
            {
                ["eventType"] = "track_play",
                ["occurredAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["payload"] = new JsonObject { ["trackGUID"] = trackGuid }
            })
        };

        return PostAsync("/event/report", body, ct);
    }

    public void ClearToken() => Token = null;

    public void Dispose() => _http.Dispose();
}
