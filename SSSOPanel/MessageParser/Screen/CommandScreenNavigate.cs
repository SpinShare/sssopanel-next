using Newtonsoft.Json.Linq;
using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandScreenNavigate: ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        if (data == null) return;

        var screenManager = ScreenManager.ScreenManager.GetInstance();
        
        var dataItem = (JObject)data;
            
        var path = dataItem.GetValue("path")?.ToObject<string>();
        var query = dataItem.GetValue("query")?.ToObject<JObject>();
        var parameters = dataItem.GetValue("params")?.ToObject<JObject>();

        // Force Navigation even if same route
        var navigationGuid = Guid.NewGuid();
        if (query != null)
        {
            query.Add("_", navigationGuid.ToString());
        }
        else
        {
            query = new JObject
            {
                { "_", navigationGuid.ToString() }
            };
        }
            
        if (path != null)
        {
            var newCurrentRoute = new
            {
                Path = "/screen/" + path,
                Query = query,
                Params = parameters
            };

            screenManager.CurrentRoute = newCurrentRoute;
            
            MessageHandler.SendResponse(sender, new
            {
                Command = "screen-navigate-response",
                Data = newCurrentRoute,
            });
            
            MessageHandler.SendScreenResponse(new
            {
                Command = "screen-navigate-response",
                Data = newCurrentRoute,
            });
        }
        
        await Task.Yield();
    }
}