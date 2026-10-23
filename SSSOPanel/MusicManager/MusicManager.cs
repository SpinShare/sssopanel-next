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
    // Pausing or a short gap between tracks shouldn't flash the footer, only hide after this long
    private static readonly TimeSpan HideAfter = TimeSpan.FromSeconds(5);
    // A new song must look the same for this many polls before it's shown, so skipping through tracks doesn't flicker
    private const int StablePolls = 2;
    // Same album tracks share the cover, so stop waiting for a different one after this long
    private static readonly TimeSpan CoverWait = TimeSpan.FromSeconds(5);
    // Tuna sometimes writes cover.png a while after the title changes, keep updating it for this long
    private static readonly TimeSpan CoverSettleTime = TimeSpan.FromSeconds(10);

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(2) };
    private static readonly HttpClient RemoteHttpClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    private static MusicManager? _instance;
    private static readonly object Lock = new();

    public record Song(bool Active, string Artist, string Title, string Cover);

    private static readonly Song NoSong = new(false, "", "", "");

    private Song _current = NoSong;
    private bool _started;

    private DateTime _lastSeenAt = DateTime.MinValue;
    private DateTime _shownAt = DateTime.MinValue;
    private string _currentKey = "";
    private string _currentCoverHash = "";

    // The song tuna reports that isn't shown yet
    private string _candidateKey = "";
    private int _candidatePolls;
    private DateTime _candidateSince = DateTime.MinValue;

    // Latest downloaded cover, plus how many polls in a row it stayed the same
    private string _coverSource = "";
    private string _coverHash = "";
    private int _coverPolls;

    // Served by OverlayServer, only the shown cover and the latest one are kept
    private readonly Dictionary<string, byte[]> _covers = new();

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

    public byte[]? GetCover(string hash)
    {
        lock (Lock)
        {
            return _covers.GetValueOrDefault(hash);
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
            var spotify = GetSpotifySong();
            Apply(spotify, spotify.Artist + "\n" + spotify.Title, "");
            return;
        }

        var url = settingsManager.Get<string>("tuna.url");
        if (string.IsNullOrWhiteSpace(url)) url = DefaultTunaUrl;
        url = url.TrimEnd('/');

        var (song, key, coverSource) = await GetTunaSong(url);
        var showCover = !settingsManager.Exists("tuna.showCover") || settingsManager.Get<bool>("tuna.showCover");
        if (!showCover) coverSource = "";

        if (song.Active && coverSource != "") await FetchCover(coverSource, coverSource != url + "/cover.png");
        Apply(song, key, coverSource);
    }

    // Remote art urls belong to one track and never change, tuna's cover.png is rewritten in place
    private async Task FetchCover(string source, bool immutable)
    {
        lock (Lock)
        {
            if (immutable && source == _coverSource && _coverPolls >= StablePolls) return;
        }

        byte[] bytes;
        try
        {
            bytes = await (immutable ? RemoteHttpClient : HttpClient).GetByteArrayAsync(source);
        }
        catch (Exception)
        {
            // Not written or reachable yet, try again next poll
            return;
        }

        var hash = Convert.ToHexString(SHA1.HashData(bytes))[..12];
        lock (Lock)
        {
            _coverPolls = immutable ? StablePolls : source == _coverSource && hash == _coverHash ? _coverPolls + 1 : 1;
            _coverSource = source;
            _coverHash = hash;
            _covers[hash] = bytes;
            foreach (var key in _covers.Keys.Where(k => k != hash && k != _currentCoverHash).ToList())
            {
                _covers.Remove(key);
            }
        }
    }

    private void Apply(Song song, string key, string coverSource)
    {
        var now = DateTime.UtcNow;
        var wantCover = coverSource != "";

        lock (Lock)
        {
            if (!song.Active)
            {
                if (_current.Active && now - _lastSeenAt < HideAfter) return;
                _current = NoSong;
                _currentKey = "";
                _currentCoverHash = "";
                _candidateKey = "";
                return;
            }

            _lastSeenAt = now;
            var coverReady = wantCover && _coverSource == coverSource && _coverPolls >= StablePolls;

            if (key == _currentKey)
            {
                _candidateKey = "";
                // Late cover write for the shown song
                if (coverReady && _coverHash != _currentCoverHash && now - _shownAt < CoverSettleTime) ShowCover(_coverHash);
                if (!wantCover && _currentCoverHash != "") ShowCover("");
                return;
            }

            // Skipping changes the song every poll, and tuna updates the artists a second before the rest
            if (key != _candidateKey)
            {
                _candidateKey = key;
                _candidatePolls = 1;
                _candidateSince = now;
            }
            else
            {
                _candidatePolls++;
            }
            if (_candidatePolls < StablePolls) return;

            // Hold the old song until the new cover is in, unless it's the same one (same album) or never shows up
            var waited = now - _candidateSince >= CoverWait;
            var newCover = coverReady && _coverHash != _currentCoverHash;
            if (wantCover && !newCover && !waited) return;

            _candidateKey = "";
            _shownAt = now;
            _current = song;
            _currentKey = key;
            ShowCover(coverReady ? _coverHash : "");
        }
    }

    private void ShowCover(string hash)
    {
        _currentCoverHash = hash;
        // Content addressed, so the screen never shows a stale cached image
        var cover = hash == "" ? "" : $"http://localhost:{OverlayServer.OverlayServer.Port}/music/cover/{hash}";
        _current = _current with { Cover = cover };
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

    private static async Task<(Song Song, string Key, string CoverSource)> GetTunaSong(string url)
    {
        try
        {
            var song = JObject.Parse(await HttpClient.GetStringAsync(url));

            var title = song.Value<string>("title") ?? "";
            var artists = song["artists"] is JArray array ? array.Values<string>() : Array.Empty<string>();
            var artist = string.Join(", ", artists.Where(a => !string.IsNullOrEmpty(a)));
            var active = song.Value<string>("status") == "playing" && title != "";

            // cover_path is the player's own art url for some players (e.g. tidal), which is per track and right away.
            // Otherwise use cover.png, but from our tuna url since cover_url always says localhost
            var coverPath = song.Value<string>("cover_path") ?? "";
            var coverSource = coverPath.StartsWith("http://") || coverPath.StartsWith("https://")
                ? coverPath
                : string.IsNullOrEmpty(song.Value<string>("cover_url")) ? "" : url + "/cover.png";

            var key = string.Join("\n", artist, title, song.Value<string>("album") ?? "", coverPath);
            return (new Song(active, artist, title, ""), key, coverSource);
        }
        catch (Exception)
        {
            // Tuna is not running or returned garbage, treat it as nothing playing
            return (NoSong, "", "");
        }
    }
}
