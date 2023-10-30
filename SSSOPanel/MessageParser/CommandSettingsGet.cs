using Newtonsoft.Json.Linq;
using PhotinoNET;

namespace SSSOPanel.MessageParser;

/// <summary>
/// A command that returns the value of a setting given a <c>key</c>
/// </summary>
public class CommandSettingsGet : ICommand
{
    private SettingsManager.SettingsManager? _settingsManager;
    
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        if (data == null) return;
        _settingsManager = SettingsManager.SettingsManager.GetInstance();
        
        var key = data.ToString();
        if (key == null) return;

        var responseData = new JObject(
            new JProperty("key", key),
            new JProperty("data", _settingsManager.Get<object>(key))
        );

        Message response = new() {
            Command = "settings-get-response",
            Data = responseData
        };
        
        await Task.Yield();

        MessageHandler.SendResponse(sender, response);
    }
}