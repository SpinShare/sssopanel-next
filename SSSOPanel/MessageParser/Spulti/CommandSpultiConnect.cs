using Newtonsoft.Json.Linq;

namespace SSSOPanel.MessageParser;

/// <summary>
/// A command that starts following a spulti match link, an empty link stops
/// </summary>
public class CommandSpultiConnect : ICommand
{
    public async Task Execute(IClient? sender, object? data)
    {
        if (data == null) return;

        var dataItem = (JObject)data;
        var url = dataItem.GetValue("url")?.ToObject<string>();

        // Broadcasts a spulti-update on its own
        await Task.Run(() => SpultiManager.SpultiManager.GetInstance().Follow(url));
    }
}
