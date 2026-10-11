using Process = System.Diagnostics.Process;
using ElectronNET.API;
using Newtonsoft.Json.Linq;

namespace SSSOPanel.MessageParser;

/// <summary>
/// A command that returns the currently playing song from the configured music source
/// </summary>
public class CommandScreenMusicGet : ICommand
{
    private const string SourceTuna = "tuna";
    private const string DefaultTunaUrl = "http://localhost:1608";

    // Screens poll every second, so a hanging Tuna must not hold up other commands
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(1) };

    private record Song(bool Active, string Artist, string Title, string Cover);

    public async Task Execute(BrowserWindow? sender, object? data)
    {
        // Spotify needs no external setup, so it stays the default
        var source = SettingsManager.SettingsManager.GetInstance().Get<string>("music.source");
        var song = source == SourceTuna ? await GetTunaSong() : GetSpotifySong();

        MessageHandler.SendScreenResponse(new
        {
            Command = "screen-music-get-response",
            Data = new
            {
                active = song.Active,
                artist = song.Artist,
                title = song.Title,
                cover = song.Cover,
            },
        });
    }

    private static Song GetSpotifySong()
    {
        var song = new Song(false, "", "", "");

        foreach (var process in Process.GetProcessesByName("Spotify"))
        {
            var windowTitle = process.MainWindowTitle;
            if (string.IsNullOrEmpty(windowTitle) || !windowTitle.Contains(" - ")) continue;

            var splitTitle = windowTitle.Split(" - ");
            song = new Song(true, splitTitle[0], splitTitle[1], "");
        }

        return song;
    }

    private static async Task<Song> GetTunaSong()
    {
        var settingsManager = SettingsManager.SettingsManager.GetInstance();
        var showCover = !settingsManager.Exists("tuna.showCover") || settingsManager.Get<bool>("tuna.showCover");

        var url = settingsManager.Get<string>("tuna.url");
        if (string.IsNullOrWhiteSpace(url)) url = DefaultTunaUrl;
        url = url.TrimEnd('/');

        try
        {
            var song = JObject.Parse(await HttpClient.GetStringAsync(url));

            var title = song.Value<string>("title") ?? "";
            var artist = string.Join(", ", song["artists"]?.Values<string>() ?? Array.Empty<string>());
            var active = song.Value<string>("status") == "playing" && title != "";

            // Tuna always reports the cover on localhost, which is wrong if it runs on another machine
            var hasCover = showCover && !string.IsNullOrEmpty(song.Value<string>("cover_url"));

            return new Song(active, artist, title, hasCover ? url + "/cover.png" : "");
        }
        catch (Exception)
        {
            // Tuna is not running or returned garbage, treat it as nothing playing
            return new Song(false, "", "", "");
        }
    }
}
