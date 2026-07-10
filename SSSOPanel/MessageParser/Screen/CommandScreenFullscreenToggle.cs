using ElectronNET.API;

namespace SSSOPanel.MessageParser;

public class CommandScreenFullscreenToggle: ICommand
{
    public async Task Execute(BrowserWindow? sender, object? data)
    {
        if (sender == null) return;
        
        var isFullScreen = await sender.IsFullScreenAsync();
        sender.SetFullScreen(!isFullScreen);
        
        await Task.Yield();
    }
}