using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SpinShareClient.MessageParser;

public class CommandFactory
{
    public ICommand GetCommand(string command)
    {
        switch(command)
        {
            case "open-screen":
                return new CommandOpenScreen();
            case "open-in-browser":
                return new CommandOpenInBrowser();
            case "open-in-explorer":
                return new CommandOpenInExplorer();
            case "settings-open-in-explorer":
                return new CommandSettingsOpenInExplorer();
            case "settings-get":
                return new CommandSettingsGet();
            case "settings-get-full":
                return new CommandSettingsGetFull();
            case "settings-set":
                return new CommandSettingsSet();
            case "update-get-version":
                return new CommandUpdateGetVersion();
            case "update-get-latest":
                return new CommandUpdateGetLatest();
            case "current-route-get":
                return new CommandCurrentRouteGet();
            case "screen-navigate":
                return new CommandScreenNavigate();
            default:
                throw new Exception($"Unknown command: {command}");
        }
    }
}