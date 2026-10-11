using Newtonsoft.Json.Linq;
using SSSOPanel.MessageParser;

namespace SSSOPanel.ScreenManager;

public class ScreenRoute
{
    public string Path = "/screen";
    public JObject Parameters = new JObject();
    public JObject Query = new JObject();
    public JObject RichData = new JObject();
}

public class ScreenManager
{
    private static ScreenManager? _instance;
    private static readonly object Lock = new();

    private List<IClient> _screens = new();
    public ScreenRoute CurrentRoute = new();
    public JObject State = new JObject();

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

    public void RegisterScreen(IClient screen)
    {
        Console.WriteLine("Registering Screen");
        lock (Lock)
        {
            _screens.Add(screen);
        }
    }

    public void UnregisterScreen(IClient screen)
    {
        Console.WriteLine("Deregistering Screen");
        lock (Lock)
        {
            _screens.Remove(screen);
        }
    }

    public List<IClient> GetScreens()
    {
        lock (Lock)
        {
            return new List<IClient>(_screens);
        }
    }
}
