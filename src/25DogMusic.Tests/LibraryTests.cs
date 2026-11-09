using System.Text.Json;
using _25DogMusic.Core.Api;
using Xunit;

namespace _25DogMusic.Tests;

/// <summary>飞牛音乐返回 JSON 到界面模型的映射测试。</summary>
public class LibraryTests
{
    static JsonDocument Json(string json) => JsonDocument.Parse(json);

    [Fact]
    public void ParseTracks_ReadsMetadataAndAudioSpec()
    {
        using var doc = Json(@"
        {
          ""data"": {
            ""list"": [
              {
                ""guid"": ""g1"",
                ""title"": ""一無所有"",
                ""artists"": [{ ""name"": ""王杰"" }, { ""name"": ""另一位"" }],
                ""album"": { ""name"": ""True Legend"", ""guid"": ""a1"" },
                ""duration"": 267019,
                ""coverId"": ""c1"",
                ""audioSpec"": { ""format"": ""WAV"", ""bitDepth"": 16, ""sampleRate"": 44100, ""bitrate"": 1411200 }
              }
            ]
          }
        }");

        var tracks = Library.ParseTracks(doc);

        var track = Assert.Single(tracks);
        Assert.Equal("g1", track.Guid);
        Assert.Equal("一無所有", track.Title);
        Assert.Equal("王杰", track.Artist);
        Assert.Equal("True Legend", track.AlbumName);
        Assert.Equal("a1", track.AlbumGuid);
        Assert.Equal(267019, track.DurationMs);
        Assert.Equal("c1", track.CoverId);
        Assert.Equal("WAV", track.Format);
        Assert.Equal(16, track.BitDepth);
        Assert.Equal(44100, track.SampleRate);
        Assert.Equal(1411200, track.Bitrate);
    }

    [Fact]
    public void ParseTracks_SkipsEntriesWithoutGuid()
    {
        using var doc = Json(@"{ ""data"": { ""list"": [
            { ""title"": ""没有 guid"" },
            { ""guid"": ""g2"", ""title"": ""正常"" }
        ] } }");

        var tracks = Library.ParseTracks(doc);

        var track = Assert.Single(tracks);
        Assert.Equal("g2", track.Guid);
    }

    [Fact]
    public void ParseTracks_ReturnsEmptyForUnexpectedPayload()
    {
        using var doc = Json(@"{ ""code"": 0, ""data"": { } }");

        Assert.Empty(Library.ParseTracks(doc));
    }

    [Fact]
    public void ParseRoam_RequiresRoamIdAndTrack()
    {
        using var ok = Json(@"{ ""data"": { ""current"": {
            ""roamId"": ""r1"",
            ""track"": { ""guid"": ""g1"", ""title"": ""歌"" }
        } } }");
        using var missingRoamId = Json(@"{ ""data"": { ""current"": {
            ""track"": { ""guid"": ""g1"" }
        } } }");
        using var missingTrack = Json(@"{ ""data"": { ""current"": { ""roamId"": ""r1"" } } }");

        var result = Library.ParseRoam(ok);

        Assert.NotNull(result);
        Assert.Equal("r1", result!.Value.RoamId);
        Assert.Equal("g1", result.Value.Track.Guid);
        Assert.Null(Library.ParseRoam(missingRoamId));
        Assert.Null(Library.ParseRoam(missingTrack));
    }

    [Fact]
    public void ParseFolderView_ParsesFoldersAndTracks()
    {
        using var doc = Json(@"{ ""data"": { ""folder"": {
            ""name"": ""华语"",
            ""path"": ""/music/华语"",
            ""itemCount"": 3,
            ""folders"": [ { ""name"": ""王杰"", ""path"": ""/music/华语/王杰"", ""itemCount"": 12 } ],
            ""tracks"": [ { ""guid"": ""g1"", ""title"": ""歌"" } ]
        } } }");

        var folder = Library.ParseFolderView(doc);

        Assert.NotNull(folder);
        Assert.Equal("华语", folder!.Name);
        Assert.Equal(3, folder.ItemCount);
        Assert.Single(folder.Folders);
        Assert.Equal("/music/华语/王杰", folder.Folders[0].Path);
        Assert.Single(folder.Tracks);
    }

    [Fact]
    public void ParseFolderView_ReturnsNullWhenFolderMissing()
    {
        using var doc = Json(@"{ ""data"": { } }");

        Assert.Null(Library.ParseFolderView(doc));
    }

    [Fact]
    public void ParseSharedLibraries_FallsBackToLastPathSegment()
    {
        using var doc = Json(@"{ ""data"": { ""list"": [
            { ""guid"": ""s1"", ""name"": ""我的音乐"", ""path"": ""/vol1/music"" },
            { ""guid"": ""s2"", ""name"": """", ""path"": ""/vol1/华语/"" }
        ] } }");

        var libraries = Library.ParseSharedLibraries(doc);

        Assert.Equal(2, libraries.Count);
        Assert.Equal("我的音乐", libraries[0].Name);
        Assert.Equal("华语", libraries[1].Name);
    }

    [Fact]
    public void ParseAlbumsArtistsGenresPlaylists_UseSameShape()
    {
        using var doc = Json(@"{ ""data"": { ""list"": [
            { ""guid"": ""x1"", ""name"": ""名称"", ""trackCount"": 5, ""albumCount"": 2, ""coverId"": ""c1"",
              ""artists"": [{ ""name"": ""歌手"" }] }
        ] } }");

        Assert.Equal("歌手", Assert.Single(Library.ParseAlbums(doc)).Artist);
        Assert.Equal(2, Assert.Single(Library.ParseArtists(doc)).AlbumCount);
        Assert.Equal(5, Assert.Single(Library.ParseGenres(doc)).TrackCount);
        Assert.Equal("c1", Assert.Single(Library.ParsePlaylists(doc)).CoverId);
    }
}
