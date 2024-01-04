using Newtonsoft.Json.Linq;
using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandScreenMatchUpdate: ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        if (data == null) return;
        
        var dataItem = (JObject)data;
            
        MessageHandler.SendScreenResponse(new
        {
            Command = "screen-match-update-response",
            Data = dataItem
        });
        
        await Task.Yield();
    }
}