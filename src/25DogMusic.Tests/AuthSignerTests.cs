using FnMusic.Auth;
using Xunit;

namespace FnMusic.Tests;

/// <summary>authx 签名相关的确定性逻辑测试（不覆盖随机 nonce / timestamp）。</summary>
public class AuthSignerTests
{
    [Fact]
    public void BuildQuery_SortsByKeyAndSkipsNullValues()
    {
        var query = AuthSigner.BuildQuery(new Dictionary<string, string?>
        {
            ["b"] = "2",
            ["a"] = "1",
            ["c"] = null
        });

        Assert.Equal("a=1&b=2", query);
    }

    [Fact]
    public void BuildQuery_EscapesReservedCharacters()
    {
        var query = AuthSigner.BuildQuery(new Dictionary<string, string?>
        {
            ["kw"] = "周杰伦 叶惠美",
            ["p"] = "1/2"
        });

        Assert.Equal("kw=%E5%91%A8%E6%9D%B0%E4%BC%A6%20%E5%8F%B6%E6%83%A0%E7%BE%8E&p=1%2F2", query);
    }

    [Fact]
    public void BuildQuery_ReturnsEmptyForNoParameters()
    {
        Assert.Equal(string.Empty, AuthSigner.BuildQuery(Array.Empty<KeyValuePair<string, string?>>()));
    }

    [Fact]
    public void Md5Hex_MatchesKnownVector()
    {
        Assert.Equal("900150983cd24fb0d6963f7d28e17f72", AuthSigner.Md5Hex("abc"));
    }

    [Fact]
    public void Sha256Hex_MatchesKnownVector()
    {
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", AuthSigner.Sha256Hex("abc"));
    }

    [Fact]
    public void CreateAuthX_ReturnsNonceTimestampAndMd5Sign()
    {
        var authx = AuthSigner.CreateAuthX("/music/api/v1/album/list", "page=1", isGet: true);

        var parts = authx.Split('&');
        Assert.Equal(3, parts.Length);
        Assert.StartsWith("nonce=", parts[0]);
        Assert.StartsWith("timestamp=", parts[1]);
        Assert.StartsWith("sign=", parts[2]);

        var nonce = int.Parse(parts[0]["nonce=".Length..]);
        Assert.InRange(nonce, 100000, 1000000);

        var sign = parts[2]["sign=".Length..];
        Assert.Equal(32, sign.Length);
        Assert.Matches("^[0-9a-f]{32}$", sign);
    }
}
