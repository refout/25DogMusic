using FnMusic.Api;
using Xunit;

namespace FnMusic.Tests;

/// <summary>LRC 歌词解析测试。</summary>
public class LrcParserTests
{
    [Fact]
    public void Parse_ReadsTimestampAndText()
    {
        var lines = LrcParser.Parse("[00:12.34]第一行\n[01:02.50]第二行");

        Assert.Equal(2, lines.Count);
        Assert.Equal(TimeSpan.FromMilliseconds(12340), lines[0].Time);
        Assert.Equal("第一行", lines[0].Text);
        Assert.Equal(TimeSpan.FromMilliseconds(62500), lines[1].Time);
    }

    [Fact]
    public void Parse_SupportsMultipleTagsOnOneLine()
    {
        var lines = LrcParser.Parse("[00:01.00][00:10.00]重复的副歌");

        Assert.Equal(2, lines.Count);
        Assert.All(lines, line => Assert.Equal("重复的副歌", line.Text));
        Assert.Equal(TimeSpan.FromSeconds(1), lines[0].Time);
        Assert.Equal(TimeSpan.FromSeconds(10), lines[1].Time);
    }

    [Fact]
    public void Parse_SortsByTime()
    {
        var lines = LrcParser.Parse("[00:30.00]后\n[00:05.00]前");

        Assert.Equal(new[] { "前", "后" }, lines.Select(line => line.Text).ToArray());
    }

    [Theory]
    [InlineData("[00:01.5]x", 1500)]
    [InlineData("[00:01.05]x", 1050)]
    [InlineData("[00:01.123]x", 1123)]
    [InlineData("[00:01.1234]x", 1123)]
    [InlineData("[00:01]x", 1000)]
    public void Parse_NormalizesFractionDigits(string lrc, int expectedMs)
    {
        var line = Assert.Single(LrcParser.Parse(lrc));

        Assert.Equal(TimeSpan.FromMilliseconds(expectedMs), line.Time);
    }

    [Fact]
    public void Parse_AcceptsSquareBrackets()
    {
        var line = Assert.Single(LrcParser.Parse("[00:02.00]方括号"));

        Assert.Equal(TimeSpan.FromSeconds(2), line.Time);
        Assert.Equal("方括号", line.Text);
    }

    [Fact]
    public void Parse_IgnoresLinesWithoutTimestamp()
    {
        Assert.Empty(LrcParser.Parse("没有时间戳的一行"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_ReturnsEmptyForBlankInput(string lrc)
    {
        Assert.Empty(LrcParser.Parse(lrc));
    }
}
