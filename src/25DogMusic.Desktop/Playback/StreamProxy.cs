using System.Net;
using System.Net.Sockets;
using System.Text;
using _25DogMusic.Core.Api;

namespace _25DogMusic.Desktop.Playback;

/// <summary>
/// 本地回环代理：VLC 不支持自定义 Cookie/签名，这里把请求补上再转发给飞牛音乐，
/// 同时透传 Range，保证拖动进度条可用。
/// </summary>
public sealed class StreamProxy : IDisposable
{
    readonly HttpListener _listener = new();
    readonly MusicApiClient _api;
    CancellationTokenSource? _cts;

    public int Port { get; }
    public string BaseUrl => $"http://127.0.0.1:{Port}/";

    public StreamProxy(MusicApiClient api)
    {
        _api = api;
        Port = FindFreePort();
        _listener.Prefixes.Add(BaseUrl);
    }

    public void Start()
    {
        _listener.Start();
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => LoopAsync(_cts.Token));
    }

    async Task LoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch
            {
                return;
            }

            _ = Task.Run(() => HandleAsync(context));
        }
    }

    async Task HandleAsync(HttpListenerContext context)
    {
        var response = context.Response;
        try
        {
            var guid = context.Request.Url?.AbsolutePath.Trim('/');
            if (string.IsNullOrEmpty(guid))
            {
                response.StatusCode = 400;
                response.Close();
                return;
            }

            using var upstream = _api.CreateStreamRequest(guid);
            var range = context.Request.Headers["Range"];
            if (!string.IsNullOrEmpty(range))
            {
                upstream.Headers.TryAddWithoutValidation("Range", range);
            }

            using var upstreamResponse = await _api.SendRawAsync(upstream, HttpCompletionOption.ResponseHeadersRead);
            response.StatusCode = (int)upstreamResponse.StatusCode;

            if (upstreamResponse.Content.Headers.ContentType is { } contentType)
            {
                response.ContentType = contentType.ToString();
            }

            response.ContentLength64 = upstreamResponse.Content.Headers.ContentLength ?? -1;

            foreach (var header in new[] { "Accept-Ranges", "Content-Range" })
            {
                if (upstreamResponse.Content.Headers.TryGetValues(header, out var values))
                {
                    response.AddHeader(header, string.Join(",", values));
                }
                else if (upstreamResponse.Headers.TryGetValues(header, out var hv))
                {
                    response.AddHeader(header, string.Join(",", hv));
                }
            }

            response.AddHeader("Cache-Control", "no-store");
            response.KeepAlive = false;

            await using var body = await upstreamResponse.Content.ReadAsStreamAsync();
            await body.CopyToAsync(response.OutputStream);
        }
        catch (Exception ex)
        {
            try
            {
                response.StatusCode = 502;
                var bytes = Encoding.UTF8.GetBytes(ex.Message);
                await response.OutputStream.WriteAsync(bytes);
            }
            catch
            {
                // 客户端已断开
            }
        }
        finally
        {
            try { response.Close(); } catch { }
        }
    }

    static int FindFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        try { _listener.Stop(); } catch { }
        _listener.Close();
        _cts?.Dispose();
    }
}
