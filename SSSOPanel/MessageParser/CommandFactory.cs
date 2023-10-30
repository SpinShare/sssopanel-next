using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SpinShareClient.MessageParser;

/// <summary>
/// The CommandFactory class is responsible for producing instances of ICommand, based on a provided string.
/// </summary>
public class CommandFactory
{
    /// <summary>
    /// Returns an instance of a class that implements <see cref="ICommand"/>, corresponding to the provided <c>command</c> string.
    /// </summary>
    /// <param name="command">Command as <see cref="String"/></param>
    /// <returns>An instance of a class implementing <see cref="ICommand"/> that corresponds to the <c>command</c> string.</returns>
    /// <exception cref="Exception">Thrown when an unknown command string is provided.</exception>
    public ICommand GetCommand(string command)
    {
        using var serviceProvider = new ServiceCollection()
            .AddLogging(configure => configure.AddConsole())
            .AddLogging(configure => configure.AddDebug())
            .BuildServiceProvider();
        
        switch(command)
        {
            case "open-screen":
                return new CommandOpenScreen();
            case "open-in-browser":
                return new CommandOpenInBrowser(serviceProvider);
            case "open-in-explorer":
                return new CommandOpenInExplorer();
            case "settings-open-in-explorer":
                return new CommandSettingsOpenInExplorer();
            case "settings-get":
                return new CommandSettingsGet(serviceProvider);
            case "settings-get-full":
                return new CommandSettingsGetFull(serviceProvider);
            case "settings-set":
                return new CommandSettingsSet(serviceProvider);
            case "update-get-version":
                return new CommandUpdateGetVersion(serviceProvider);
            case "update-get-latest":
                return new CommandUpdateGetLatest(serviceProvider);
            case "current-route-get":
                return new CommandCurrentRouteGet(serviceProvider);
            case "screen-navigate":
                return new CommandScreenNavigate();
            default:
                throw new Exception($"Unknown command: {command}");
        }
    }
}