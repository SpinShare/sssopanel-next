using Newtonsoft.Json.Linq;
using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandScreenMatchUpdate: ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        if (data == null) return;
        
        var screenManager = ScreenManager.ScreenManager.GetInstance();
        
        var dataItem = (JObject)data;
        screenManager.CurrentRoute.RichData.Merge(dataItem);
            
        MessageHandler.SendScreenResponse(new
        {
            Command = "screen-match-update-response",
            Data = dataItem
        });
        
        await Task.Yield();
    }
}