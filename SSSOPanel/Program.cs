using ElectronNET.API;
using ElectronNET.API.Entities;
using SSSOPanel.ScreenManager;

namespace SSSOPanel;

using MessageParser;

public class Program
{
    private static FileStream? _lockFile;
    private static BrowserWindow? _panelWindow;
    
    public static async Task Main(string[] args)
    {
        // Single Instance Lock
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        if (!IsSingleInstance())
        {
            Console.WriteLine("Single Instance Check failed");
            return;
        }
        
        var messageHandler = new MessageHandler();
        var updateManager = UpdateManager.UpdateManager.GetInstance();
        var settingsManager = SettingsManager.SettingsManager.GetInstance();
        var screenManager = ScreenManager.ScreenManager.GetInstance();

        Console.WriteLine("Creating Panel Window");
        _panelWindow = await Electron.WindowManager.CreateWindowAsync(
            new BrowserWindowOptions
            {
                Title = "Panel",
                Width = Convert.ToInt32(400 * ScreenScaleFactor.Get()),
                Height = Convert.ToInt32(750 * ScreenScaleFactor.Get()),
                Resizable = false,
#if DEBUG
                WebPreferences = new WebPreferences { DevTools = true },
#endif
            }
        );

#if DEBUG
        Console.WriteLine("Debug Mode, starting dev site");
        _panelWindow.LoadURL($"http://localhost:5173/#/panel");
#else
        Console.WriteLine("Production Mode, starting built site");
        _panelWindow.LoadURL($"{screenManager.BaseUrl}/index.html#/panel");
#endif

        _panelWindow.OnClosed += () =>
        {
            Console.WriteLine("Closing Screen Windows");
            foreach (var screen in screenManager.GetScreens())
            {
                screen.Close();
            }
        };

        // Register IPC handler
        Electron.IpcMain.On("message", async (args) =>
        {
            var msg = args.ToString();
            if (string.IsNullOrEmpty(msg)) return;
            
            var jObj = Newtonsoft.Json.Linq.JObject.Parse(msg);
            var senderType = jObj.GetValue("__sender")?.ToString();
            jObj.Remove("__sender");
            
            BrowserWindow? sender = senderType == "panel" ? _panelWindow : null;
            
            await messageHandler.HandleMessageAsync(sender, jObj.ToString(Newtonsoft.Json.Formatting.None));
        });

        // Keep the app running
        await Task.Delay(-1);
    }

    private static void OnProcessExit(object? sender, EventArgs e)
    {
        if (_lockFile is null) return;
        
        _lockFile.Close();
        _lockFile = null;
    }
    
    static bool IsSingleInstance()
    {
        string appFolder = SettingsManager.SettingsManager.GetAppFolder();

        if (string.IsNullOrEmpty(appFolder) || !Directory.Exists(appFolder))
        {
            throw new InvalidOperationException("[IsSingleInstance] Application folder not found.");
        }

        string lockFileName = Path.Combine(appFolder, "app.lock");

        try
        {
            _lockFile = new FileStream(
                lockFileName,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None
            );
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }
}
