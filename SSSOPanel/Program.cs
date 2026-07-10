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
        Electron.IpcMain.On("message", async (evt, message) =>
        {
            var msg = message.ToString();
            if (string.IsNullOrEmpty(msg)) return;
            
            // Resolve the sender BrowserWindow from the event's webContentsId
            BrowserWindow? sender = null;
            var allWindows = Electron.WindowManager.GetAllWindows();
            foreach (var w in allWindows)
            {
                if (w.WebContents.Id == evt.Sender.Id)
                {
                    sender = w;
                    break;
                }
            }
            
            await messageHandler.HandleMessageAsync(sender, msg);
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
