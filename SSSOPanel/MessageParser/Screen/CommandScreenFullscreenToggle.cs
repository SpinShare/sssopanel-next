using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandScreenFullscreenToggle: ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        if (sender == null) return;
        
        sender.SetFullScreen(!sender.FullScreen);
        
        await Task.Yield();
    }
}