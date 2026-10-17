using Newtonsoft.Json;

namespace SSSOPanel.MessageParser;

public class MessageHandler
{
    // Commands an overlay is allowed to send, everything else is panel only
    private static readonly HashSet<string> ScreenCommands = new()
    {
        "current-route-get",
        "state-get",
        "state-set",
        "settings-get-full",
        "screen-music-get",
        "srxd-get",
        "theme-get",
        "voicebot-status-get",
    };

    // Set once the panel window exists, for messages that don't answer a command
    public static IClient? PanelClient;

    private readonly CommandFactory _commandFactory = new();
    private readonly SemaphoreSlim _commandLock = new(1, 1);

    public async Task HandleMessageAsync(IClient? client, string message)
    {
        if (client == null)
        {
            throw new ArgumentNullException($"[MessageHandler] client must be of type IClient");
        }
        
        var msg = JsonConvert.DeserializeObject<Message>(message);
        if (msg?.Command == null) return;

        if (!client.IsPanel && !ScreenCommands.Contains(msg.Command))
        {
            Console.WriteLine("[MessageHandler] Command {0} is not allowed for screens.", msg.Command);
            return;
        }
        
        var command = _commandFactory.GetCommand(msg.Command);
        if (command == null) 
        {
            throw new Exception($"[MessageHandler] Command {msg.Command} is not registered.");
        }
        
        // Panel and screens send from different threads, run commands one at a time
        await _commandLock.WaitAsync();
        try
        {
            await command.Execute(client, msg.Data);
        }
        finally
        {
            _commandLock.Release();
        }
    }

    public static void SendResponse(IClient? sender, object result)
    {
        if (sender == null) return;

        try
        {
            sender.Send(JsonConvert.SerializeObject(result));
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public static void SendScreenResponse(object result)
    {
        var screens = ScreenManager.ScreenManager.GetInstance().GetScreens();
        var resultJson = JsonConvert.SerializeObject(result);

        foreach (var screen in screens)
        {
            try
            {
                screen.Send(resultJson);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
