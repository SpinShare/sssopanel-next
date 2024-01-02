using System.Diagnostics;
using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandScreenSpotifyGet: ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        var spotifyProcesses = Process.GetProcessesByName("Spotify");

        var spotifyActive = false;
        var spotifyArtist = "";
        var spotifyTitle = "";
        foreach (var process in spotifyProcesses)
        {
            var windowTitle = process.MainWindowTitle;
            if (string.IsNullOrEmpty(windowTitle) || !windowTitle.Contains(" - ")) continue;
            
            var splitTitle = windowTitle.Split(" - ");

            spotifyActive = true;
            spotifyArtist = splitTitle[0];
            spotifyTitle = splitTitle[1];
        }
        
        MessageHandler.SendScreenResponse(new
        {
            Command = "screen-spotify-get-response",
            Data = new
            {
                active = spotifyActive,
                artist = spotifyArtist,
                title = spotifyTitle,
            },
        });
        
        await Task.Yield();
    }
}