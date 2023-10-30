using PhotinoNET;

namespace SSSOPanel.ScreenManager;

public class ScreenManager
{
    private static ScreenManager? _instance;
    private static readonly object Lock = new();

    private List<PhotinoWindow> _screens = new();
    public object CurrentRoute = new
    {
        Path = "",
        Parameters = new {},
        Query = new {}
    };
    public string BaseUrl = "";

    public ScreenManager()
    {
        Console.WriteLine("Initializing ScreenManager");
    }
    
    public static ScreenManager GetInstance()
    {
        if (_instance == null)
        {
            lock (Lock)
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
        Console.WriteLine("Registering Screen: {0}", screen.Id);
        _screens.Add(screen);
    }

    public void UnregisterScreen(PhotinoWindow screen)
    {
        Console.WriteLine("Deregistering Screen: {0}", screen.Id);
        _screens.Remove(screen);
    }

    public List<PhotinoWindow> GetScreens()
    {
        return _screens;
    }
}