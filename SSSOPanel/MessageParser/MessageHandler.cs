using ElectronNET.API;
using Newtonsoft.Json;

namespace SSSOPanel.MessageParser;

public class MessageHandler
{
    private readonly CommandFactory _commandFactory = new();

    public async Task HandleMessageAsync(BrowserWindow? window, string message)
    {
        if (window == null)
        {
            throw new ArgumentNullException($"[MessageHandler] window must be of type BrowserWindow");
        }
        
        var msg = JsonConvert.DeserializeObject<Message>(message);
        if (msg?.Command == null) return;
        
        var command = _commandFactory.GetCommand(msg.Command);
        if (command == null) 
        {
            throw new Exception($"[MessageHandler] Command {msg.Command} is not registered.");
        }
        
        await command.Execute(window, msg.Data);
    }

    public static void SendResponse(BrowserWindow? sender, object result)
    {
        if (sender == null) return;

        try
        {
            var resultJson = JsonConvert.SerializeObject(result);
            Electron.IpcMain.Send(sender, "message", resultJson);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public static void SendScreenResponse(object result)
    {
        var screens = ScreenManager.ScreenManager.GetInstance().GetScreens();

        foreach (var screen in screens)
        {
            try
            {
                var resultJson = JsonConvert.SerializeObject(result);
                Electron.IpcMain.Send(screen, "message", resultJson);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
