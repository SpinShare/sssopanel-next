using Newtonsoft.Json.Linq;

namespace SSSOPanel.MessageParser;

/// <summary>
/// A command that tries the given tuna address once (the settings page field, saved or not) and reports what happened
/// </summary>
public class CommandTunaCheck : ICommand
{
    public Task Execute(IClient? sender, object? data)
    {
        var url = (data as JObject)?.GetValue("url")?.ToObject<string>();

        // Can take the whole timeout, the command queue shouldn't wait on it
        _ = Task.Run(async () =>
        {
            var (ok, message) = await MusicManager.MusicManager.CheckTuna(url);
            MessageHandler.SendResponse(sender, new
            {
                Command = "tuna-check-response",
                Data = new { ok, message },
            });
        });

        return Task.CompletedTask;
    }
}
