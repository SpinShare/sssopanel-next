using ElectronNET.API;

namespace SSSOPanel.MessageParser;

public class CommandSettingsGetFull : ICommand
{
    public async Task Execute(IClient? sender, object? data)
    {
        var settingsManager = SettingsManager.SettingsManager.GetInstance();

        Message response = new() {
            Command = "settings-get-full-response",
            Data = sender?.IsPanel == true ? settingsManager.GetFull() : settingsManager.GetFullWithoutSecrets()
        };
        
        await Task.Yield();

        MessageHandler.SendResponse(sender, response);
    }
}