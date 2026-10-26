using ElectronNET.API;
using Newtonsoft.Json;

namespace SSSOPanel.MessageParser;

using UpdateManager;

public class CommandUpdateGetLatest : ICommand
{
    public async Task Execute(IClient? sender, object? data)
    {
        var updateManager = UpdateManager.GetInstance();
        Console.WriteLine("Getting latest Version");

        var releases = await updateManager.LoadReleases();
        // Only version tags, the repo also has dated and other one-off releases that can't be compared
        var latestRelease = releases.FirstOrDefault(x => !x.Prerelease && Version.TryParse(x.TagName.TrimStart('v').Replace("-preview", ""), out _));

        Message response = new() {
            Command = "update-get-latest-response",
            Data = JsonConvert.SerializeObject(latestRelease)
        };

        MessageHandler.SendResponse(sender, response);
    }
}