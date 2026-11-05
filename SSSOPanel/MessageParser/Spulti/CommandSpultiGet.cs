namespace SSSOPanel.MessageParser;

/// <summary>
/// A command that returns the followed spulti match and the connection status
/// </summary>
public class CommandSpultiGet : ICommand
{
    public async Task Execute(IClient? sender, object? data)
    {
        MessageHandler.SendResponse(sender, new
        {
            Command = "spulti-get-response",
            Data = SpultiManager.SpultiManager.GetInstance().GetSnapshot(),
        });

        await Task.Yield();
    }
}
