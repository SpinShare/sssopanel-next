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

        // Production base URL points at the built UI copied next to the executable.
        var wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        screenManager.BaseUrl = $"file://{wwwrootPath}";

#if DEBUG
        Console.WriteLine("Debug Mode, starting dev site");
        var panelUrl = "http://localhost:5173/#/panel";
#else
        Console.WriteLine("Production Mode, starting built site");
        var panelUrl = $"{screenManager.BaseUrl}/index.html#/panel";
#endif

        Console.WriteLine("Creating Panel Window");
        _panelWindow = await Electron.WindowManager.CreateWindowAsync(
            new BrowserWindowOptions
            {
                Title = "Panel",
                Width = Convert.ToInt32(400 * ScreenScaleFactor.Get()),
                Height = Convert.ToInt32(750 * ScreenScaleFactor.Get()),
                Resizable = false,
                Show = false,
                AutoHideMenuBar = true,
                WebPreferences = new WebPreferences
                {
                    DevTools = true,
                    ContextIsolation = true,
                    NodeIntegration = false,
                    Preload = Path.Combine(AppContext.BaseDirectory, "preload.js"),
                },
            },
            panelUrl
        );

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
        _ = Electron.IpcMain.On("message", async (args) =>
        {
            var msg = args.ToString();
            if (string.IsNullOrEmpty(msg)) return;

            var jObj = Newtonsoft.Json.Linq.JObject.Parse(msg);

            var senderId = jObj.GetValue("__windowId")?.ToObject<int?>();
            jObj.Remove("__windowId");

            BrowserWindow? sender = null;
            if (senderId.HasValue)
            {
                sender = Electron.WindowManager.BrowserWindows.FirstOrDefault(w => w.Id == senderId.Value);
            }

            // Fallback for messages without a window id
            if (sender == null)
            {
                var senderType = jObj.GetValue("__sender")?.ToString();
                jObj.Remove("__sender");
                sender = senderType == "panel" ? _panelWindow : null;
            }
            else
            {
                jObj.Remove("__sender");
            }

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
