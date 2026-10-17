using Newtonsoft.Json.Linq;

namespace SSSOPanel.MessageParser;

/// <summary>
/// A command that saves the OBS connection details, connects and returns the resulting status
/// </summary>
public class CommandObsConnect : ICommand
{
    public async Task Execute(IClient? sender, object? data)
    {
        if (data == null) return;

        var settingsManager = SettingsManager.SettingsManager.GetInstance();
        var obsManager = ObsManager.ObsManager.GetInstance();

        var dataItem = (JObject)data;
        settingsManager.Set("obs.enabled", dataItem.GetValue("enabled")?.ToObject<bool>() ?? false);
        settingsManager.Set("obs.url", dataItem.GetValue("url")?.ToObject<string>() ?? "");
        settingsManager.Set("obs.password", dataItem.GetValue("password")?.ToObject<string>() ?? "");

        obsManager.Configure();
        await obsManager.WaitForConnection();

        await new CommandObsStatusGet().Execute(sender, null);
    }
}
