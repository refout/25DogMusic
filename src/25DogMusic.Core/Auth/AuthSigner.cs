using System.Security.Cryptography;
using System.Text;

namespace _25DogMusic.Core.Auth;

/// <summary>
/// 复刻飞牛音乐 Web 客户端的 authx 请求签名。
/// sign = MD5(salt_path_nonce_timestamp_dataHash_apiKey)
/// GET 的 dataHash 是「解码后的查询串」的 MD5；POST 是「原始 JSON 请求体」的 MD5。
/// </summary>
public static class AuthSigner
{
    const string Salt = "NDzZTVxnRKP8Z0jXg1VAMonaG8akvh";
    const string ApiKey = "6D5602D4-A342-4799-A0F0-BB795E7167D0";

    /// <summary>按 key 升序拼查询串，与官方前端一致（空值不发送）。</summary>
    public static string BuildQuery(IEnumerable<KeyValuePair<string, string?>> parameters)
    {
        var parts = new List<string>();
        foreach (var kv in parameters.Where(p => p.Value is not null).OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            parts.Add($"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}");
        }

        return string.Join("&", parts).Replace("+", "%20");
    }

    public static string CreateAuthX(string path, string? payload, bool isGet)
    {
        var dataHash = isGet ? Md5Hex(PercentDecode(payload ?? string.Empty)) : Md5Hex(payload ?? string.Empty);
        var nonce = RandomNumberGenerator.GetInt32(100000, 1000001);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var sign = Md5Hex($"{Salt}_{path}_{nonce}_{timestamp}_{dataHash}_{ApiKey}");
        return $"nonce={nonce}&timestamp={timestamp}&sign={sign}";
    }

    static string PercentDecode(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value);
        }
        catch
        {
            return value;
        }
    }

    public static string Md5Hex(string value)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(bytes);
    }

    public static string Sha256Hex(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexStringLower(bytes);
    }
}
