namespace SSSOPanel.MessageParser;

/// <summary>
/// A command that stops the local voice bot, the new status is broadcast
/// </summary>
public class CommandVoiceBotStop : ICommand
{
    public async Task Execute(IClient? sender, object? data)
    {
        await Task.Run(() => VoiceBotManager.VoiceBotManager.GetInstance().Stop());
    }
}
