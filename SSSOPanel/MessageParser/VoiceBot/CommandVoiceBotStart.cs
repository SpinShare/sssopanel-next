namespace SSSOPanel.MessageParser;

/// <summary>
/// A command that (re)starts the local voice bot, the new status is broadcast
/// </summary>
public class CommandVoiceBotStart : ICommand
{
    public async Task Execute(IClient? sender, object? data)
    {
        // Stopping the old one waits for it to leave the voice channel
        await Task.Run(() => VoiceBotManager.VoiceBotManager.GetInstance().Start());
    }
}
