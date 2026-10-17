using Newtonsoft.Json.Linq;

namespace SSSOPanel.MessageParser;

/// <summary>
/// A command that saves the Spin Rhythm XD connection details and (re)connects
/// </summary>
public class CommandSrxdConnect : ICommand
{
    public async Task Execute(IClient? sender, object? data)
    {
        if (data == null) return;

        var settingsManager = SettingsManager.SettingsManager.GetInstance();

        var dataItem = (JObject)data;
        settingsManager.Set("srxd.enabled", dataItem.GetValue("enabled")?.ToObject<bool>() ?? true);
        settingsManager.Set("srxd.url", dataItem.GetValue("url")?.ToObject<string>() ?? "");

        SrxdManager.SrxdManager.GetInstance().Configure(force: true);

        // The connection result arrives as an srxd-update, this just reports the current state
        await new CommandSrxdGet().Execute(sender, null);
    }
}
