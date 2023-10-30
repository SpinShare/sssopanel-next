using Newtonsoft.Json.Linq;
using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandSettingsSet : ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
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

        Message response = new() {
            Command = "settings-set-response",
            Data = settingsManager.GetFull()
        };
        
        await Task.Yield();

        MessageHandler.SendResponse(sender, response);
    }
}