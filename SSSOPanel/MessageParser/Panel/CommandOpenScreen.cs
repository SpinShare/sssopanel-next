using Newtonsoft.Json.Linq;
using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandOpenScreen: ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        if (data == null) return;
        var screenManager = ScreenManager.ScreenManager.GetInstance();

        var dataItem = (JObject)data;
        screenManager.CreateNewScreen(dataItem.GetValue("fullscreen")?.ToObject<bool>() ?? false);
        
        await Task.Yield();
    }
}