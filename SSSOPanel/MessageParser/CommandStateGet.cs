using ElectronNET.API;
using Newtonsoft.Json.Linq;

namespace SSSOPanel.MessageParser;

public class CommandStateGet: ICommand
{
    public async Task Execute(BrowserWindow? sender, object? data)
    {
        var screenManager = ScreenManager.ScreenManager.GetInstance();
            
        MessageHandler.SendResponse(sender, new
        {
            Command = "state-get-response",
            Data = screenManager.State
        });
        
        MessageHandler.SendScreenResponse(new
        {
            Command = "state-get-response",
            Data = screenManager.State
        });
        
        await Task.Yield();
    }
}