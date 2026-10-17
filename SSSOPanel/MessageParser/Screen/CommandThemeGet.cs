namespace SSSOPanel.MessageParser;

/// <summary>
/// A command that returns the overlay theme (winter, spring, summer, autumn) and the event year for its logo
/// </summary>
public class CommandThemeGet : ICommand
{
    // Spring is what the overlay looked like before themes were hooked up
    public const string DefaultTheme = "spring";

    public static object GetTheme()
    {
        var settings = SettingsManager.SettingsManager.GetInstance().GetFull();

        return new
        {
            theme = settings.GetValueOrDefault("theme") as string is { Length: > 0 } theme ? theme : DefaultTheme,
            // Empty means the latest year that has a logo for the theme
            year = settings.GetValueOrDefault("eventYear")?.ToString() ?? "",
        };
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
