using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhotinoNET;

namespace SpinShareClient;

public class ScreenManager
{
    private readonly ILogger<ScreenManager> _logger;

    private static ScreenManager? _instance;
    private static readonly object _lock = new();

    private List<PhotinoWindow> _screens = new();
    public object CurrentRoute = new
    {
        Path = "",
        Parameters = new {},
        Query = new {}
    };

    public ScreenManager()
    {
        using var serviceProvider = new ServiceCollection()
            .AddLogging(configure => configure.AddConsole())
            .AddLogging(configure => configure.AddDebug())
            .BuildServiceProvider();
        
        _logger = serviceProvider.GetRequiredService<ILogger<ScreenManager>>();
        
        _logger.LogInformation("Initializing");
    }
    
    public static ScreenManager GetInstance()
    {
        if (_instance == null)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new ScreenManager();
                }
            }
        }
        return _instance;
    }

    public void RegisterScreen(PhotinoWindow screen)
    {
        _logger.LogInformation("Registering Screen: {ScreenId}", screen.Id);
        _screens.Add(screen);
    }

    public void UnregisterScreen(PhotinoWindow screen)
    {
        _logger.LogInformation("Deregistering Screen: {ScreenId}", screen.Id);
        _screens.Remove(screen);
    }

    public List<PhotinoWindow> GetScreens()
    {
        return _screens;
    }
}