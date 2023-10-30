using PhotinoNET;

namespace SSSOPanel.MessageParser;

using UpdateManager;

public class CommandUpdateGetVersion : ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        var updateManager = UpdateManager.GetInstance();
        
        Message response = new() {
            Command = "update-get-version-response",
            Data = updateManager.CurrentVersion
        };

        await Task.Yield();

        MessageHandler.SendResponse(sender, response);
    }
}