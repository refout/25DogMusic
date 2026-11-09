using System.Text.Json;

namespace FnMusic.Api;

/// <summary>专辑条目。</summary>
public sealed record Album(string Guid, string Name, string Artist, int TrackCount, string CoverId);

/// <summary>歌手条目。</summary>
public sealed record Artist(string Guid, string Name, int TrackCount, int AlbumCount, string CoverId);

/// <summary>风格条目。</summary>
public sealed record Genre(string Guid, string Name, int TrackCount);

/// <summary>歌单条目。</summary>
public sealed record Playlist(string Guid, string Name, int TrackCount, string CoverId);

/// <summary>共享库（音乐根目录）。</summary>
public sealed record SharedLibrary(string Guid, string Name, string Path);

/// <summary>文件夹视图里的子文件夹。</summary>
public sealed record FolderEntry(string Name, string Path, int ItemCount);

/// <summary>文件夹视图内容：当前文件夹的子目录与歌曲。</summary>
public sealed record FolderContent(string Name, string Path, int ItemCount, List<FolderEntry> Folders, List<Track> Tracks);

/// <summary>歌曲条目，音质相关字段在服务端未提供时保持默认值。</summary>
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

    public static List<SharedLibrary> ParseSharedLibraries(JsonDocument doc)
    {
        var result = new List<SharedLibrary>();
        if (!TryGetList(doc, out var list)) return result;

        foreach (var item in list.EnumerateArray())
        {
            var path = GetString(item, "path");
            if (string.IsNullOrEmpty(path)) continue;
            var name = GetString(item, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                // 共享库名称为空时用目录名代替，避免标题直接显示整段路径
                var trimmed = path.TrimEnd('/');
                var index = trimmed.LastIndexOf('/');
                name = index >= 0 ? trimmed[(index + 1)..] : trimmed;
            }
            result.Add(new SharedLibrary(GetString(item, "guid"), name, path));
        }

        return result;
    }

    /// <summary>解析 folder-view/list 的 data.folder。</summary>
    public static FolderContent? ParseFolderView(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("folder", out var folder) ||
            folder.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var folders = new List<FolderEntry>();
        if (folder.TryGetProperty("folders", out var folderArray) && folderArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in folderArray.EnumerateArray())
            {
                var path = GetString(item, "path");
                if (string.IsNullOrEmpty(path)) continue;
                folders.Add(new FolderEntry(GetString(item, "name"), path, GetInt(item, "itemCount")));
            }
        }

        var tracks = new List<Track>();
        if (folder.TryGetProperty("tracks", out var trackArray) && trackArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in trackArray.EnumerateArray())
            {
                var track = FromElement(item);
                if (track is not null) tracks.Add(track);
            }
        }

        return new FolderContent(
            GetString(folder, "name"),
            GetString(folder, "path"),
            GetInt(folder, "itemCount"),
            folders,
            tracks);
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
        => item.ValueKind == JsonValueKind.Object
           && item.TryGetProperty(name, out var el)
           && el.ValueKind == JsonValueKind.String
            ? el.GetString() ?? string.Empty
            : string.Empty;

    public static int GetInt(JsonElement item, string name)
        => item.ValueKind == JsonValueKind.Object
           && item.TryGetProperty(name, out var el)
           && el.ValueKind == JsonValueKind.Number
            ? el.GetInt32()
            : 0;

    public static long GetLong(JsonElement item, string name)
        => item.ValueKind == JsonValueKind.Object
           && item.TryGetProperty(name, out var el)
           && el.ValueKind == JsonValueKind.Number
            ? el.GetInt64()
            : 0;
}
