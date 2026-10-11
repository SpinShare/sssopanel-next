namespace SSSOPanel.MessageParser;

public class CommandFactory
{
    public ICommand GetCommand(string command)
    {
        return command switch
        {
            "open-in-browser" => new CommandOpenInBrowser(),
            "open-in-explorer" => new CommandOpenInExplorer(),
            "settings-open-in-explorer" => new CommandSettingsOpenInExplorer(),
            "settings-get" => new CommandSettingsGet(),
            "settings-get-full" => new CommandSettingsGetFull(),
            "settings-set" => new CommandSettingsSet(),
            "state-get" => new CommandStateGet(),
            "state-set" => new CommandStateSet(),
            "update-get-version" => new CommandUpdateGetVersion(),
            "update-get-latest" => new CommandUpdateGetLatest(),
            "current-route-get" => new CommandCurrentRouteGet(),
            "screen-navigate" => new CommandScreenNavigate(),
            "screen-spotify-get" => new CommandScreenSpotifyGet(),
            "screen-match-update" => new CommandScreenMatchUpdate(),
            "obs-status-get" => new CommandObsStatusGet(),
            "obs-connect" => new CommandObsConnect(),
            _ => throw new Exception($"Unknown command: {command}")
        };
    }
}