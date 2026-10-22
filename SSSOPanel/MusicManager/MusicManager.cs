using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Process = System.Diagnostics.Process;

namespace SSSOPanel.MusicManager;

/// <summary>
/// Polls the configured music source in the background, so screen-music-get never waits on it.
/// Smooths out the short gaps sources report between tracks.
/// </summary>
public class MusicManager
{
    public const string SourceTuna = "tuna";
    public const string DefaultTunaUrl = "http://localhost:1608";

    private const int PollDelay = 1000;
    // Tuna reports a gap or a stale title for a moment between tracks, don't flash the footer for it
    private static readonly TimeSpan HideAfter = TimeSpan.FromSeconds(3);
    // Tuna writes cover.png a while after the title changes, keep checking it until it settles
    private static readonly TimeSpan CoverSettleTime = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan CoverRecheckDelay = TimeSpan.FromSeconds(5);
    // Same album tracks share the cover, so only hide an unchanged cover for this long
    private static readonly TimeSpan StaleCoverTime = TimeSpan.FromSeconds(5);

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(2) };

    private static MusicManager? _instance;
    private static readonly object Lock = new();

    public record Song(bool Active, string Artist, string Title, string Cover);

    private static readonly Song NoSong = new(false, "", "", "");

    private Song _current = NoSong;
    private bool _started;

    private DateTime _lastSeenAt = DateTime.MinValue;
    private string _pendingKey = "";

    private string _coverHash = "";
    private string _previousCoverHash = "";
    private DateTime _songChangedAt = DateTime.MinValue;
    private DateTime _coverCheckedAt = DateTime.MinValue;

    public static MusicManager GetInstance()
    {
        if (_instance == null)
        {
            lock (Lock)
            {
                if (_instance == null)
                {
                    _instance = new MusicManager();
                }
            }
        }
        return _instance;
    }

    public void Start()
    {
        lock (Lock)
        {
            if (_started) return;
            _started = true;
        }
        _ = Run();
    }

    public Song GetCurrent()
    {
        lock (Lock)
        {
            return _current;
        }
    }

    private async Task Run()
    {
        while (true)
        {
            try
            {
                await Poll();
            }
            catch (Exception)
            {
                // ignored
            }
            await Task.Delay(PollDelay);
        }
    }

    private async Task Poll()
    {
        var settingsManager = SettingsManager.SettingsManager.GetInstance();

        // Spotify needs no external setup, so it stays the default
        if (settingsManager.Get<string>("music.source") != SourceTuna)
        {
            Apply(GetSpotifySong(), "");
            return;
        }

        var url = settingsManager.Get<string>("tuna.url");
        if (string.IsNullOrWhiteSpace(url)) url = DefaultTunaUrl;
        url = url.TrimEnd('/');

        var (song, hasCover) = await GetTunaSong(url);
        var showCover = !settingsManager.Exists("tuna.showCover") || settingsManager.Get<bool>("tuna.showCover");
        Apply(song, showCover && hasCover ? url : "");

        if (_current.Active && showCover && hasCover) await UpdateCover(url);
    }

    private void Apply(Song song, string coverBaseUrl)
    {
        var now = DateTime.UtcNow;

        lock (Lock)
        {
            if (!song.Active)
            {
                // Hold on to the last song through short gaps
                if (_current.Active && now - _lastSeenAt < HideAfter) return;
                _current = NoSong;
                _pendingKey = "";
                return;
            }

            var key = song.Artist + "\n" + song.Title;
            var currentKey = _current.Active ? _current.Artist + "\n" + _current.Title : "";

            if (key != currentKey)
            {
                // Sources update title and artists separately, wait for a second identical poll
                if (_current.Active && key != _pendingKey)
                {
                    _pendingKey = key;
                    _lastSeenAt = now;
                    return;
                }

                _previousCoverHash = _coverHash;
                _coverHash = "";
                _songChangedAt = now;
                _coverCheckedAt = DateTime.MinValue;
            }

            _pendingKey = "";
            _lastSeenAt = now;
            _current = song with { Cover = CoverUrl(coverBaseUrl, now) };
        }
    }

    private async Task UpdateCover(string url)
    {
        var now = DateTime.UtcNow;
        lock (Lock)
        {
            var settling = now - _songChangedAt < CoverSettleTime;
            if (!settling && now - _coverCheckedAt < CoverRecheckDelay) return;
            _coverCheckedAt = now;
        }

        string hash;
        try
        {
            var bytes = await HttpClient.GetByteArrayAsync(url + "/cover.png");
            hash = Convert.ToHexString(SHA1.HashData(bytes))[..12];
        }
        catch (Exception)
        {
            // Not written yet, try again next poll
            return;
        }

        lock (Lock)
        {
            _coverHash = hash;
            if (_current.Active) _current = _current with { Cover = CoverUrl(url, DateTime.UtcNow) };
        }
    }

    // Busting by content instead of by song, Tuna serves every cover from the same url and replaces it late
    private string CoverUrl(string baseUrl, DateTime now)
    {
        if (baseUrl == "" || _coverHash == "") return "";
        if (_coverHash == _previousCoverHash && now - _songChangedAt < StaleCoverTime) return "";
        return $"{baseUrl}/cover.png?v={_coverHash}";
    }

    private static Song GetSpotifySong()
    {
        var song = NoSong;

        foreach (var process in Process.GetProcessesByName("Spotify"))
        {
            var windowTitle = process.MainWindowTitle;
            if (string.IsNullOrEmpty(windowTitle) || !windowTitle.Contains(" - ")) continue;

            // Only the first separator, titles like "Song - Remastered" have more
            var splitTitle = windowTitle.Split(" - ", 2);
            song = new Song(true, splitTitle[0], splitTitle[1], "");
        }

        return song;
    }

    private static async Task<(Song Song, bool HasCover)> GetTunaSong(string url)
    {
        try
        {
            var song = JObject.Parse(await HttpClient.GetStringAsync(url));

            var title = song.Value<string>("title") ?? "";
            var artists = song["artists"] is JArray array ? array.Values<string>() : Array.Empty<string>();
            var artist = string.Join(", ", artists.Where(a => !string.IsNullOrEmpty(a)));
            var active = song.Value<string>("status") == "playing" && title != "";

            // Tuna always reports the cover on localhost, which is wrong if it runs on another machine
            var hasCover = !string.IsNullOrEmpty(song.Value<string>("cover_url"));

            return (new Song(active, artist, title, ""), hasCover);
        }
        catch (Exception)
        {
            // Tuna is not running or returned garbage, treat it as nothing playing
            return (NoSong, false);
        }
    }
}
