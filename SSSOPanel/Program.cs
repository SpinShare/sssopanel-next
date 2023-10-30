using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhotinoNET;
using PhotinoNET.Server;
using Sentry;

namespace SSSOPanel;

using MessageParser;

public class Program
{
    private static FileStream? _lockFile;
    
    [STAThread]
    private static void Main(string[] args)
    {
        // Single Instance Lock
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        if (!IsSingleInstance())
        {
            Console.WriteLine("Single Instance Check failed");
            
            // TODO: Error Dialog
            return;
        }
        
        PhotinoServer
            .CreateStaticFileServer(args, out string baseUrl)
            .RunAsync();

        var messageHandler = new MessageHandler();
        var updateManager = UpdateManager.UpdateManager.GetInstance();
        var settingsManager = SettingsManager.SettingsManager.GetInstance();
        var screenManager = ScreenManager.ScreenManager.GetInstance();

        screenManager.BaseUrl = baseUrl;
        
        Console.WriteLine("Creating Panel Window");
        var windowPanel = new PhotinoWindow()
            .SetLogVerbosity(2)
            .SetTitle("Panel")
            .SetSize(500, 750)
            .SetUseOsDefaultSize(false)
            .Center()
            .SetResizable(false)
            .RegisterWebMessageReceivedHandler(messageHandler.RegisterWebMessageReceivedHandler);

#if DEBUG
        Console.WriteLine("Debug Mode, starting dev site");
        
        windowPanel.SetDevToolsEnabled(true);
        windowPanel.Load(new Uri($"http://localhost:5173/#/panel"));
#else
        Console.WriteLine("Production Mode, starting built site");
        windowPanel.Load($"{screenManager.BaseUrl}/index.html#/panel");
#endif
        
        windowPanel.WaitForClose();
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
