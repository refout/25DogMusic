using System.Net;
using System.Net.Sockets;
using System.Text;

namespace _25DogMusic.Desktop;

/// <summary>
/// 授权回跳监听：在 127.0.0.1 的随机端口等待浏览器带回 code。
/// 飞牛音乐网页版的 /oauth/result 会立刻消费授权码，走本地回环可以避免授权码被网页先用掉。
/// </summary>
public sealed class OAuthLoopbackListener : IDisposable
{
    readonly HttpListener _listener = new();
    readonly int _port;

    /// <summary>传给 NAS 的 redirect_uri，授权完成后浏览器会跳到这里。</summary>
    public string RedirectUri => $"http://127.0.0.1:{_port}/oauth/callback";

    public OAuthLoopbackListener()
    {
        _port = FindFreePort();
        _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
    }

    /// <summary>等待回调，返回授权码或 NAS 带回的错误；超时则两者均为 null。</summary>
    public async Task<(string? Code, string? Error)> WaitForCodeAsync(TimeSpan timeout)
    {
        _listener.Start();
        var deadline = DateTime.UtcNow + timeout;
        try
        {
            while (DateTime.UtcNow < deadline)
            {
                var remaining = deadline - DateTime.UtcNow;
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync().WaitAsync(remaining);
                }
                catch (TimeoutException)
                {
                    return (null, null);
                }

                var query = context.Request.QueryString;
                var code = query["code"];
                var error = query["error"];
                Respond(context, string.IsNullOrEmpty(error));

                if (!string.IsNullOrEmpty(code) || !string.IsNullOrEmpty(error))
                {
                    return (code, error);
                }
            }
        }
        catch (HttpListenerException)
        {
            // 监听器被关闭（用户取消或应用退出）
        }
        finally
        {
            try { _listener.Stop(); } catch { }
        }

        return (null, null);
    }

    static void Respond(HttpListenerContext context, bool success)
    {
        var title = success ? "授权完成" : "授权未完成";
        var hint = success
            ? "25DogMusic 已自动切回前台并完成登录，可以关闭此页面。"
            : "请回到 25DogMusic 重试。";
        // 授权页是被导航打开的标签页，浏览器通常会拦截 window.close，这里只做尽力尝试
        var script = success ? "<script>setTimeout(function(){try{window.close()}catch(e){}},800)</script>" : string.Empty;
        var html = "<!doctype html><html><head><meta charset=\"utf-8\"><title>" + title + "</title></head>" +
                   "<body style=\"margin:0;min-height:100vh;display:flex;align-items:center;justify-content:center;" +
                   "background:#0f1116;color:#e8eaf0;font-family:system-ui,-apple-system,'Segoe UI',sans-serif\">" +
                   "<div style=\"text-align:center;padding:32px\">" +
                   "<h2 style=\"margin:0 0 12px;font-size:20px\">" + title + "</h2>" +
                   "<p style=\"margin:0;color:#9aa0ad;font-size:13px\">" + hint + "</p>" +
                   "</div>" + script + "</body></html>";
        var body = Encoding.UTF8.GetBytes(html);

        context.Response.StatusCode = success ? 200 : 400;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = body.Length;
        try
        {
            context.Response.OutputStream.Write(body, 0, body.Length);
        }
        finally
        {
            context.Response.Close();
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
        try { _listener.Stop(); } catch { }
        _listener.Close();
    }
}
