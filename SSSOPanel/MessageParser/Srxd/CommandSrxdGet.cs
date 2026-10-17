namespace SSSOPanel.MessageParser;

/// <summary>
/// A command that returns the latest Spin Rhythm XD snapshot and the connection status
/// </summary>
public class CommandSrxdGet : ICommand
{
    public async Task Execute(IClient? sender, object? data)
    {
        MessageHandler.SendResponse(sender, new
        {
            Command = "srxd-get-response",
            Data = SrxdManager.SrxdManager.GetInstance().GetSnapshot(),
        });

        await Task.Yield();
    }
}
