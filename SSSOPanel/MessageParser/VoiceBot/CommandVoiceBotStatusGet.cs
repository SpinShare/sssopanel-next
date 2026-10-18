namespace SSSOPanel.MessageParser;

/// <summary>
/// A command that returns the voice bot status and the url screens should connect to
/// </summary>
public class CommandVoiceBotStatusGet : ICommand
{
    public async Task Execute(IClient? sender, object? data)
    {
        MessageHandler.SendResponse(sender, new
        {
            Command = "voicebot-status-response",
            Data = VoiceBotManager.VoiceBotManager.GetInstance().GetStatus(sender?.IsPanel ?? false),
        });

        await Task.Yield();
    }
}
