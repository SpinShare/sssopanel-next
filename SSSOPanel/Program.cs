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

        // Overlays (OBS browser sources) connect to this instead of Electron IPC
        var wwwrootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
        await OverlayServer.OverlayServer.GetInstance().Start(wwwrootPath, messageHandler);

        ObsManager.ObsManager.GetInstance().Configure();

#if DEBUG
        Console.WriteLine("Debug Mode, starting dev site");
        var panelUrl = "http://localhost:5173/#/panel";
#else
        Console.WriteLine("Production Mode, starting built site");
        var panelUrl = $"file://{wwwrootPath}/index.html#/panel";
#endif

        Console.WriteLine("Creating Panel Window");
        _panelWindow = await Electron.WindowManager.CreateWindowAsync(
            new BrowserWindowOptions
            {
                Title = "Panel",
                Width = Convert.ToInt32(400 * ScreenScaleFactor.Get()),
                Height = Convert.ToInt32(750 * ScreenScaleFactor.Get()),
                // The default size is the smallest the panel layout is made for
                MinWidth = Convert.ToInt32(400 * ScreenScaleFactor.Get()),
                MinHeight = Convert.ToInt32(750 * ScreenScaleFactor.Get()),
                Resizable = true,
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

        var panelClient = new ElectronClient(_panelWindow);
        MessageHandler.PanelClient = panelClient;

        // Connected after the panel exists so it gets the first game updates
        SrxdManager.SrxdManager.GetInstance().Configure();

        // Register IPC handler, only the panel talks over IPC
        _ = Electron.IpcMain.On("message", async (args) =>
        {
            var msg = args.ToString();
            if (string.IsNullOrEmpty(msg)) return;

            var jObj = Newtonsoft.Json.Linq.JObject.Parse(msg);
            jObj.Remove("__windowId");
            jObj.Remove("__sender");

            await messageHandler.HandleMessageAsync(panelClient, jObj.ToString(Newtonsoft.Json.Formatting.None));
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
