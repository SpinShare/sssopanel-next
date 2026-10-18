namespace SSSOPanel.MessageParser;

/// <summary>
/// A command that returns the overlay theme, one event as "<season>-<year>" (e.g. "autumn-2026")
/// </summary>
public class CommandThemeGet : ICommand
{
    public static object GetTheme()
    {
        var settings = SettingsManager.SettingsManager.GetInstance().GetFull();
        var theme = settings.GetValueOrDefault("theme") as string ?? "";

        // Old settings were a season plus eventYear. Empty or unknown themes fall back to the newest one in the ui
        if (theme.Length > 0 && !theme.Contains('-') && settings.GetValueOrDefault("eventYear")?.ToString() is { Length: > 0 } year)
        {
            theme = $"{theme}-{year}";
        }

        return new { theme };
    }

    public async Task Execute(IClient? sender, object? data)
    {
        MessageHandler.SendResponse(sender, new
        {
            Command = "theme-get-response",
            Data = GetTheme(),
        });

        await Task.Yield();
    }
}
