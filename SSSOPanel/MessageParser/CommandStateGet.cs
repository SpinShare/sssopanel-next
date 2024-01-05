using Newtonsoft.Json.Linq;
using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandStateGet: ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        var screenManager = ScreenManager.ScreenManager.GetInstance();
            
        MessageHandler.SendResponse(sender, new
        {
            Command = "state-get-response",
            Data = screenManager.State
        });
        
        MessageHandler.SendScreenResponse(new
        {
            Command = "state-set-response",
            Data = screenManager.State
        });
        
        await Task.Yield();
    }
}