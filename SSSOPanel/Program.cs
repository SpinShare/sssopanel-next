using ElectronNET.API;
using ElectronNET.API.Entities;
using SSSOPanel.ScreenManager;

namespace SSSOPanel;

using MessageParser;

public class Program
{
    private static FileStream? _lockFile;
    
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

        // Register IPC handler
        Electron.IpcMain.On("message", async (message) =>
        {
            var msg = message.ToString();
            if (string.IsNullOrEmpty(msg)) return;
            
            // Get the sender window from the event
            // Note: In Electron.NET, we need to handle this differently
            // The message handler will need to determine the sender
            await messageHandler.HandleMessageAsync(null, msg);
        });

        Console.WriteLine("Creating Panel Window");
        var windowPanel = await Electron.WindowManager.CreateWindowAsync(
            new BrowserWindowOptions
            {
                Title = "Panel",
                Width = Convert.ToInt32(400 * ScreenScaleFactor.Get()),
                Height = Convert.ToInt32(750 * ScreenScaleFactor.Get()),
                Resizable = false,
#if DEBUG
                DevTools = true,
#endif
            }
        );

#if DEBUG
        Console.WriteLine("Debug Mode, starting dev site");
        windowPanel.LoadURL($"http://localhost:5173/#/panel");
#else
        Console.WriteLine("Production Mode, starting built site");
        windowPanel.LoadURL($"{screenManager.BaseUrl}/index.html#/panel");
#endif

        windowPanel.OnClosed += () =>
        {
            Console.WriteLine("Closing Screen Windows");
            foreach (var screen in screenManager.GetScreens())
            {
                screen.Close();
            }
        };

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
            using(_lockFile = new FileStream(
                lockFileName,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None
            )) 
            {
                return true;
            }
        }
        catch (IOException)
        {
            return false;
        }
    }
}
