using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandSettingsGetFull : ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        var settingsManager = SettingsManager.SettingsManager.GetInstance();

        Message response = new() {
            Command = "settings-get-full-response",
            Data = settingsManager.GetFull()
        };
        
        await Task.Yield();

        MessageHandler.SendResponse(sender, response);
    }
}