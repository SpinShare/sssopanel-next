using Newtonsoft.Json.Linq;
using PhotinoNET;
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

    private List<PhotinoWindow> _screens = new();
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

    public PhotinoWindow CreateNewScreen(bool isFullScreen)
    {
        var messageHandler = new MessageHandler();
        
        Console.WriteLine("Creating Screen Window");
        var windowScreen = new PhotinoWindow()
            .SetLogVerbosity(2)
            .SetTitle("Screen")
            .SetUseOsDefaultSize(false)
            .Center()
            .SetMediaStreamEnabled(true)
            .SetMediaAutoplayEnabled(true)
            .RegisterWindowClosingHandler(WindowClosingHandler)
            .RegisterWebMessageReceivedHandler(messageHandler.RegisterWebMessageReceivedHandler);

        if (isFullScreen)
        {
            windowScreen.SetFullScreen(true);
            windowScreen.SetResizable(false);
        }
        else
        {
            windowScreen.SetSize(Convert.ToInt32(1280 * ScreenScaleFactor.Get()), Convert.ToInt32(720 * ScreenScaleFactor.Get()));
            windowScreen.SetResizable(true);
        }
        
        RegisterScreen(windowScreen);

#if DEBUG
        Console.WriteLine("Debug Mode, starting dev site");
        
        windowScreen.SetDevToolsEnabled(true);
        windowScreen.Load(new Uri($"http://localhost:5173/#/screen"));
#else
        Console.WriteLine("Production Mode, starting built site");
        windowScreen.Load($"{screenManager.BaseUrl}/index.html#/screen");
#endif
        windowScreen.WaitForClose();

        return windowScreen;
    }

    static bool WindowClosingHandler(object sender, EventArgs e)
    {
        var window = (PhotinoWindow)sender;
        var screenManager = GetInstance();
        
        screenManager.UnregisterScreen(window);
        return false;
    }
}