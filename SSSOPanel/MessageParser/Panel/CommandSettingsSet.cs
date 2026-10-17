using ElectronNET.API;
using Newtonsoft.Json.Linq;

namespace SSSOPanel.MessageParser;

public class CommandSettingsSet : ICommand
{
    public async Task Execute(IClient? sender, object? data)
    {
        if (data == null) return;
        var settingsManager = SettingsManager.SettingsManager.GetInstance();

        var dataArray = (JArray)data;
        foreach (var jToken in dataArray)
        {
            var dataItem = (JObject)jToken;
            
            var key = dataItem.GetValue("key")?.ToObject<string>();
            var value = dataItem.GetValue("value")?.ToObject<object>();

            if (key == null || value == null) continue;
            
            Console.WriteLine("Writing Setting: {0}", key);
            settingsManager.Set(key, value);
        }

        // Connection details or scene mapping may have changed
        ObsManager.ObsManager.GetInstance().Configure();
        SrxdManager.SrxdManager.GetInstance().Configure();

        // Screens switch their colors right away
        MessageHandler.SendScreenResponse(new
        {
            Command = "theme-get-response",
            Data = CommandThemeGet.GetTheme(),
        });

        Message response = new() {
            Command = "settings-set-response",
            Data = settingsManager.GetFull()
        };
        
        await Task.Yield();

        MessageHandler.SendResponse(sender, response);
    }
}