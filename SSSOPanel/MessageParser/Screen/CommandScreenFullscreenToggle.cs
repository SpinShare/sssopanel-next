using ElectronNET.API;

namespace SSSOPanel.MessageParser;

public class CommandScreenFullscreenToggle: ICommand
{
    public async Task Execute(BrowserWindow? sender, object? data)
    {
        if (sender == null) return;
        
        sender.SetFullScreen(!sender.FullScreen);
        
        await Task.Yield();
    }
}