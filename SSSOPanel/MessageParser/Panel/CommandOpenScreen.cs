using ElectronNET.API;
using Newtonsoft.Json.Linq;

namespace SSSOPanel.MessageParser;

public class CommandOpenScreen: ICommand
{
    public async Task Execute(BrowserWindow? sender, object? data)
    {
        if (data == null) return;
        var screenManager = ScreenManager.ScreenManager.GetInstance();

        var dataItem = (JObject)data;
        await screenManager.CreateNewScreen(dataItem.GetValue("fullscreen")?.ToObject<bool>() ?? false);
        
        await Task.Yield();
    }
}