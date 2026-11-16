using Newtonsoft.Json.Linq;

namespace SSSOPanel.MessageParser;

/// <summary>
/// A command that lists the spulti matches the panel can follow (live or not played yet)
/// </summary>
public class CommandSpultiMatches : ICommand
{
    public async Task Execute(IClient? sender, object? data)
    {
        // Answers on its own, spulti builds every match of the tournament and other commands shouldn't wait for that
        // Echoed back so the panel can drop answers to older requests
        var requestId = (data as JObject)?["requestId"];
        _ = Task.Run(async () =>
        {
            JArray matches = new();
            var origin = "";
            var error = "";
            try
            {
                (matches, origin, error) = await SpultiManager.SpultiManager.GetInstance().ListMatches();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SpultiManager] Could not list matches: {0}", ex.Message);
                error = ex.Message;
            }

            MessageHandler.SendResponse(sender, new
            {
                Command = "spulti-matches-response",
                Data = new JObject { { "matches", matches }, { "origin", origin }, { "error", error }, { "requestId", requestId } },
            });
        });

        await Task.Yield();
    }
}
