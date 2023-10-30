using System.Diagnostics;
using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandScreenSpotifyGet: ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        var spotifyProcesses = Process.GetProcesses();

        foreach (var process in spotifyProcesses)
        {
            Console.WriteLine(process.MainWindowTitle);
        }
        
        MessageHandler.SendScreenResponse(new
        {
            Command = "screen-spotify-get-response",
            Data = new {},
        });
        
        await Task.Yield();
    }
}