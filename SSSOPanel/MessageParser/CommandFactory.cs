namespace SSSOPanel.MessageParser;

public class CommandFactory
{
    public ICommand GetCommand(string command)
    {
        return command switch
        {
            "open-screen" => new CommandOpenScreen(),
            "open-in-browser" => new CommandOpenInBrowser(),
            "open-in-explorer" => new CommandOpenInExplorer(),
            "settings-open-in-explorer" => new CommandSettingsOpenInExplorer(),
            "settings-get" => new CommandSettingsGet(),
            "settings-get-full" => new CommandSettingsGetFull(),
            "settings-set" => new CommandSettingsSet(),
            "update-get-version" => new CommandUpdateGetVersion(),
            "update-get-latest" => new CommandUpdateGetLatest(),
            "current-route-get" => new CommandCurrentRouteGet(),
            "screen-navigate" => new CommandScreenNavigate(),
            "screen-spotify-get" => new CommandScreenSpotifyGet(),
            "screen-fullscreen-toggle" => new CommandScreenFullscreenToggle(),
            "screen-match-update" => new CommandScreenMatchUpdate(),
            _ => throw new Exception($"Unknown command: {command}")
        };
    }
}