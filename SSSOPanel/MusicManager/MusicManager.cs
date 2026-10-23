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

    // Online cover per song for when tuna has none, misses included so we don't hit the rate limits every poll
    private static readonly TimeSpan LookupRetry = TimeSpan.FromMinutes(1);
    private readonly Dictionary<string, (string Url, DateTime At, bool Failed)> _lookups = new();

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
        url = url.Trim().TrimEnd('/');
        // "localhost:1609" would otherwise be read as a url with the scheme "localhost"
        if (!url.Contains("://")) url = "http://" + url;

        var (song, key, coverSource) = await GetTunaSong(url);
        var showCover = !settingsManager.Exists("tuna.showCover") || settingsManager.Get<bool>("tuna.showCover");
        if (!showCover) coverSource = "";
        // Not found online still tries cover.png, in case a source writes one without a cover_path
        else if (coverSource == LookupCover) coverSource = song.Active ? await LookupCoverOnline(song, key) is var found && found != "" ? found : url + "/cover.png" : "";

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

        // Placeholder pages or empty files aren't covers
        if (!IsImage(bytes)) return;

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

    private static bool IsImage(byte[] bytes)
    {
        if (bytes.Length < 12) return false;
        var png = bytes[0] == 0x89 && bytes[1] == 'P' && bytes[2] == 'N' && bytes[3] == 'G';
        var jpeg = bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;
        var webp = bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F' && bytes[8] == 'W' && bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P';
        var gif = bytes[0] == 'G' && bytes[1] == 'I' && bytes[2] == 'F';
        return png || jpeg || webp || gif;
    }

    private async Task<string> LookupCoverOnline(Song song, string key)
    {
        lock (Lock)
        {
            if (_lookups.TryGetValue(key, out var cached) && (!cached.Failed || DateTime.UtcNow - cached.At < LookupRetry)) return cached.Url;
        }

        // Offline or rate limited, try again later
        var failed = false;
        string? url = null;
        try
        {
            url = await LookupItunes(song);
        }
        catch (Exception)
        {
            failed = true;
        }
        if (url == null)
        {
            try
            {
                url = await LookupDeezer(song);
            }
            catch (Exception)
            {
                failed = true;
            }
        }
        url ??= "";
        if (url != "") failed = false;

        lock (Lock)
        {
            if (_lookups.Count > 200) _lookups.Clear();
            _lookups[key] = (url, DateTime.UtcNow, failed);
        }
        return url;
    }

    // Only trust the same artist and song, a wrong cover is worse than none
    private static bool SameSong(Song song, string? artist, string? title)
    {
        return Similar(song.Artist.Split(", ")[0], artist) && Similar(song.Title, title);
    }

    // Loose on punctuation and "(feat. x)" style extras
    private static bool Similar(string a, string? b)
    {
        static string Normalize(string s) => new(s.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        var (x, y) = (Normalize(a), Normalize(b ?? ""));
        return x != "" && y != "" && (x.Contains(y) || y.Contains(x));
    }

    private static async Task<string?> LookupItunes(Song song)
    {
        var term = Uri.EscapeDataString(song.Artist + " " + song.Title);
        var json = JObject.Parse(await RemoteHttpClient.GetStringAsync($"https://itunes.apple.com/search?media=music&entity=song&limit=5&term={term}"));
        var match = json["results"]?.FirstOrDefault(r => SameSong(song, r.Value<string>("artistName"), r.Value<string>("trackName")));
        // The 100x100 url takes any size
        return match?.Value<string>("artworkUrl100")?.Replace("100x100bb", "600x600bb");
    }

    private static async Task<string?> LookupDeezer(Song song)
    {
        var query = Uri.EscapeDataString($"artist:\"{song.Artist.Split(", ")[0]}\" track:\"{song.Title}\"");
        var json = JObject.Parse(await RemoteHttpClient.GetStringAsync($"https://api.deezer.com/search?limit=5&q={query}"));
        var match = json["data"]?.FirstOrDefault(r => SameSong(song, r["artist"]?.Value<string>("name"), r.Value<string>("title")));
        return match?["album"]?.Value<string>("cover_xl");
    }

    private const string LookupCover = "lookup";

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
            // "n/a" means tuna found no art (e.g. mpd without a music folder), cover.png is then missing or stale, so look it up online.
            // Otherwise use cover.png, but from our tuna url since cover_url always says localhost
            var coverPath = song.Value<string>("cover_path") ?? "";
            var coverSource = coverPath.StartsWith("http://") || coverPath.StartsWith("https://")
                ? coverPath
                : coverPath is "" or "n/a" ? LookupCover
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
