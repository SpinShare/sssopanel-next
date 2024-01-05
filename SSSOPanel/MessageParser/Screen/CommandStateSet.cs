using Newtonsoft.Json.Linq;
using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandStateSet: ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        if (data == null) return;
        
        var screenManager = ScreenManager.ScreenManager.GetInstance();
        
        var dataItem = (JObject)data;
        screenManager.State.Merge(dataItem);
            
        MessageHandler.SendResponse(sender, new
        {
            Command = "state-set-response",
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