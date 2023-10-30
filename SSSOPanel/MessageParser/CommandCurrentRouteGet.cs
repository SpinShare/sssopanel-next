using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhotinoNET;

namespace SpinShareClient.MessageParser;

public class CommandCurrentRouteGet : ICommand
{
    private readonly ILogger<CommandCurrentRouteGet> _logger;

    public CommandCurrentRouteGet(ServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<CommandCurrentRouteGet>>();
    }
    
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        var screenManager = ScreenManager.GetInstance();
        
        Message response = new() {
            Command = "current-route-get-response",
            Data = screenManager.CurrentRoute
        };
        
        await Task.Yield();
        
        MessageHandler.SendResponse(sender, response);
    }
}