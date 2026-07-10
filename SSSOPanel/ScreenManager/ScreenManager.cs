using ElectronNET.API;
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

    private List<BrowserWindow> _screens = new();
    public ScreenRoute CurrentRoute = new();
    public string BaseUrl = "";
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

    public void RegisterScreen(BrowserWindow screen)
    {
        Console.WriteLine("Registering Screen: {0}", screen.Id);
        _screens.Add(screen);
    }

    public void UnregisterScreen(BrowserWindow screen)
    {
        Console.WriteLine("Deregistering Screen: {0}", screen.Id);
        _screens.Remove(screen);
    }

    public List<BrowserWindow> GetScreens()
    {
        return _screens;
    }

    public async Task<BrowserWindow> CreateNewScreen(bool isFullScreen)
    {
        Console.WriteLine("Creating Screen Window");
        var windowScreen = await Electron.WindowManager.CreateWindowAsync();

        if (isFullScreen)
        {
            windowScreen.SetFullScreen(true);
        }
        else
        {
            windowScreen.SetSize(
                Convert.ToInt32(1280 * ScreenScaleFactor.Get()),
                Convert.ToInt32(720 * ScreenScaleFactor.Get())
            );
        }

        windowScreen.OnClosed += () =>
        {
            UnregisterScreen(windowScreen);
        };

        RegisterScreen(windowScreen);

#if DEBUG
        Console.WriteLine("Debug Mode, starting dev site");
        windowScreen.WebContents.OpenDevTools();
        windowScreen.LoadURL($"http://localhost:5173/#/screen");
#else
        Console.WriteLine("Production Mode, starting built site");
        windowScreen.LoadURL($"{BaseUrl}/index.html#/screen");
#endif

        return windowScreen;
    }
}
