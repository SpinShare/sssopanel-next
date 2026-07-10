using ElectronNET;
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
        var runtimeController = ElectronNetRuntime.RuntimeController;

        try
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            if (!IsSingleInstance())
            {
                Console.WriteLine("Single Instance Check failed");
                return;
            }

            await runtimeController.Start();
            await runtimeController.WaitReadyTask;

            await ElectronBootstrap();

            await runtimeController.WaitStoppedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
            if (runtimeController != null)
            {
                try
                {
                    await runtimeController.Stop().ConfigureAwait(false);
                    await runtimeController.WaitStoppedTask
                        .WaitAsync(TimeSpan.FromSeconds(2))
                        .ConfigureAwait(false);
                }
                catch (Exception stopEx)
                {
                    Console.WriteLine($"Error during shutdown: {stopEx.Message}");
                }
            }
        }
    }

    private static async Task ElectronBootstrap()
    {
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
                Show = false,
                WebPreferences = new WebPreferences
                {
                    DevTools = true,
                    Preload = Path.GetFullPath("preload.js"),
                },
            }
        );

#if DEBUG
        Console.WriteLine("Debug Mode, starting dev site");
        await _panelWindow.WebContents.LoadURLAsync("http://localhost:5173/#/panel");
#else
        Console.WriteLine("Production Mode, starting built site");
        await _panelWindow.WebContents.LoadURLAsync($"{screenManager.BaseUrl}/index.html#/panel");
#endif

        _panelWindow.OnReadyToShow += () => _panelWindow.Show();

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
