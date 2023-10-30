using Newtonsoft.Json;
using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class MessageHandler
{
    private readonly CommandFactory _commandFactory = new();

    public async void RegisterWebMessageReceivedHandler(object? sender, string message)
    {
        var window = (PhotinoWindow?)sender;
        if (window == null)
        {
            throw new ArgumentNullException($"[MessageHandler] {nameof(sender)} must be of type PhotinoWindow");
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

    public static void SendResponse(PhotinoWindow? sender, object result)
    {
        if (sender == null) return;

        try
        {
            var resultJson = JsonConvert.SerializeObject(result);
            sender.SendWebMessage(resultJson);
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
                screen.SendWebMessage(resultJson);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}