using System.Text.Json;

namespace FnMusic.Api;

public sealed record Album(string Guid, string Name, string Artist, int TrackCount, string CoverId);

public sealed record Artist(string Guid, string Name, int TrackCount, int AlbumCount, string CoverId);

public sealed record Genre(string Guid, string Name, int TrackCount);

public sealed record Playlist(string Guid, string Name, int TrackCount, string CoverId);

public sealed record Track(
    string Guid,
    string Title,
    string Artist,
    string AlbumName,
    string AlbumGuid,
    long DurationMs,
    string CoverId,
    string Format = "",
    int BitDepth = 0,
    int SampleRate = 0,
    long Bitrate = 0);

/// <summary>把飞牛音乐返回的 JSON 映射成界面模型。</summary>
public static class Library
{
    public static List<Album> ParseAlbums(JsonDocument doc)
    {
        var result = new List<Album>();
        if (!TryGetList(doc, out var list)) return result;

        foreach (var item in list.EnumerateArray())
        {
            var guid = GetString(item, "guid");
            if (string.IsNullOrEmpty(guid)) continue;

            result.Add(new Album(
                guid,
                GetString(item, "name"),
                FirstArtist(item),
                GetInt(item, "trackCount"),
                GetString(item, "coverId")));
        }

        return result;
    }

    public static List<Artist> ParseArtists(JsonDocument doc)
    {
        var result = new List<Artist>();
        if (!TryGetList(doc, out var list)) return result;

        foreach (var item in list.EnumerateArray())
        {
            var guid = GetString(item, "guid");
            if (string.IsNullOrEmpty(guid)) continue;

            result.Add(new Artist(
                guid,
                GetString(item, "name"),
                GetInt(item, "trackCount"),
                GetInt(item, "albumCount"),
                GetString(item, "coverId")));
        }

        return result;
    }

    public static List<Genre> ParseGenres(JsonDocument doc)
    {
        var result = new List<Genre>();
        if (!TryGetList(doc, out var list)) return result;

        foreach (var item in list.EnumerateArray())
        {
            var guid = GetString(item, "guid");
            if (string.IsNullOrEmpty(guid)) continue;

            result.Add(new Genre(guid, GetString(item, "name"), GetInt(item, "trackCount")));
        }

        return result;
    }

    public static List<Playlist> ParsePlaylists(JsonDocument doc)
    {
        var result = new List<Playlist>();
        if (!TryGetList(doc, out var list)) return result;

        foreach (var item in list.EnumerateArray())
        {
            var guid = GetString(item, "guid");
            if (string.IsNullOrEmpty(guid)) continue;

            result.Add(new Playlist(
                guid,
                GetString(item, "name"),
                GetInt(item, "trackCount"),
                GetString(item, "coverId")));
        }

        return result;
    }

    public static List<Track> ParseTracks(JsonDocument doc)
    {
        var result = new List<Track>();
        if (!TryGetList(doc, out var list)) return result;

        foreach (var item in list.EnumerateArray())
        {
            var track = FromElement(item);
            if (track is not null) result.Add(track);
        }

        return result;
    }

    public static Track? FromElement(JsonElement item)
    {
        var guid = GetString(item, "guid");
        if (string.IsNullOrEmpty(guid)) return null;

        var album = item.TryGetProperty("album", out var a) ? a : default;
        var spec = item.TryGetProperty("audioSpec", out var s) && s.ValueKind == JsonValueKind.Object ? s : default;
        return new Track(
            guid,
            GetString(item, "title"),
            FirstArtist(item),
            album.ValueKind == JsonValueKind.Object ? GetString(album, "name") : string.Empty,
            album.ValueKind == JsonValueKind.Object ? GetString(album, "guid") : string.Empty,
            GetLong(item, "duration"),
            GetString(item, "coverId"),
            GetString(spec, "format"),
            GetInt(spec, "bitDepth"),
            GetInt(spec, "sampleRate"),
            GetLong(spec, "bitrate"));
    }

    /// <summary>解析漫游接口的 current（roamId + track）。</summary>
    public static (string RoamId, Track Track)? ParseRoam(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("current", out var current) ||
            current.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!current.TryGetProperty("track", out var element)) return null;

        var track = FromElement(element);
        var roamId = GetString(current, "roamId");
        return track is null || string.IsNullOrEmpty(roamId) ? null : (roamId, track);
    }

    static bool TryGetList(JsonDocument doc, out JsonElement list)
    {
        list = default;
        return doc.RootElement.TryGetProperty("data", out var data)
               && data.TryGetProperty("list", out list)
               && list.ValueKind == JsonValueKind.Array;
    }

    static string FirstArtist(JsonElement item)
    {
        if (!item.TryGetProperty("artists", out var artists) || artists.ValueKind != JsonValueKind.Array) return string.Empty;
        foreach (var artist in artists.EnumerateArray())
        {
            var name = GetString(artist, "name");
            if (!string.IsNullOrEmpty(name)) return name;
        }
        return string.Empty;
    }

    public static string GetString(JsonElement item, string name)
        => item.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String ? el.GetString() ?? string.Empty : string.Empty;

    public static int GetInt(JsonElement item, string name)
        => item.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number ? el.GetInt32() : 0;

    public static long GetLong(JsonElement item, string name)
        => item.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.Number ? el.GetInt64() : 0;
}
