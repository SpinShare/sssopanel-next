using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandCurrentRouteGet : ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        var screenManager = ScreenManager.ScreenManager.GetInstance();
        
        Message response = new() {
            Command = "current-route-get-response",
            Data = screenManager.CurrentRoute
        };
        
        await Task.Yield();
        
        MessageHandler.SendResponse(sender, response);
    }
}