using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandSettingsOpenInExplorer : ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        CommandOpenInExplorer command = new();
        await command.Execute(sender, SettingsManager.SettingsManager.GetAppFolder());
    }
}