namespace SSSOPanel.MessageParser;

/// <summary>
/// A command that returns the OBS connection status and the available scenes
/// </summary>
public class CommandObsStatusGet : ICommand
{
    public async Task Execute(IClient? sender, object? data)
    {
        var obsManager = ObsManager.ObsManager.GetInstance();

        MessageHandler.SendResponse(sender, new
        {
            Command = "obs-status-get-response",
            Data = new
            {
                enabled = obsManager.Enabled,
                connected = obsManager.Connected,
                error = obsManager.Error,
                scenes = await obsManager.GetScenes(),
            },
        });
    }
}
