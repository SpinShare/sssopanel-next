using Newtonsoft.Json;
using PhotinoNET;

namespace SSSOPanel.MessageParser;

using UpdateManager;

public class CommandUpdateGetLatest : ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        var updateManager = UpdateManager.GetInstance();
        Console.WriteLine("Getting latest Version");

        var releases = await updateManager.LoadReleases();
        var latestRelease = releases.FirstOrDefault(x => x.Prerelease == false);

        Message response = new() {
            Command = "update-get-latest-response",
            Data = JsonConvert.SerializeObject(latestRelease)
        };

        MessageHandler.SendResponse(sender, response);
    }
}