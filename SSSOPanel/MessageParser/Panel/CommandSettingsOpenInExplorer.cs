using ElectronNET.API;

namespace SSSOPanel.MessageParser;

public class CommandSettingsOpenInExplorer : ICommand
{
    public async Task Execute(BrowserWindow? sender, object? data)
    {
        CommandOpenInExplorer command = new();
        await command.Execute(sender, SettingsManager.SettingsManager.GetAppFolder());
    }
}