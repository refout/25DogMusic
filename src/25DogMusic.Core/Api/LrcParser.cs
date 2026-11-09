using System.Text.RegularExpressions;

namespace FnMusic.Api;

/// <summary>一行歌词与其起始时间。</summary>
public sealed record LyricLine(TimeSpan Time, string Text);

/// <summary>解析 LRC 歌词（[mm:ss.xx] 行首时间戳）。</summary>
public static class LrcParser
{
    static readonly Regex TimeTag = new(@"[\[\[](?<m>\d+):(?<s>\d{1,2})(?:\.(?<f>\d+))?[\]\]]", RegexOptions.Compiled);

    public static List<LyricLine> Parse(string lrc)
    {
        var lines = new List<LyricLine>();
        if (string.IsNullOrWhiteSpace(lrc)) return lines;

        foreach (var raw in lrc.Split('\n'))
        {
            var text = raw.TrimEnd('\r');
            var matches = TimeTag.Matches(text);
            if (matches.Count == 0) continue;

            var content = TimeTag.Replace(text, string.Empty).Trim();
            foreach (Match match in matches)
            {
                var minutes = int.Parse(match.Groups["m"].Value);
                var seconds = int.Parse(match.Groups["s"].Value);
                var fraction = match.Groups["f"].Success
                    ? int.Parse(match.Groups["f"].Value.PadRight(3, '0')[..3])
                    : 0;

                var time = new TimeSpan(0, 0, minutes, seconds, fraction);
                lines.Add(new LyricLine(time, content));
            }
        }

        return lines.OrderBy(l => l.Time).ToList();
    }
}
