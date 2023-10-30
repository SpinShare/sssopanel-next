using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhotinoNET;
using PhotinoNET.Server;
using Sentry;

namespace SpinShareClient;

using MessageParser;

public class Program
{
    private static ILogger<Program>? _logger;
    private static FileStream? _lockFile;
    
    [STAThread]
    private static void Main(string[] args)
    {
        // Error Logging
        using var serviceProvider = new ServiceCollection()
            .AddLogging(configure => configure.AddConsole())
            .AddLogging(configure => configure.AddDebug())
            .BuildServiceProvider();
        
        _logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        
        // Single Instance Lock
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        if (!IsSingleInstance())
        {
            _logger.LogError("Single Instance Check failed");
            
            // TODO: Error Dialog
            return;
        }
        
        PhotinoServer
            .CreateStaticFileServer(args, out string baseUrl)
            .RunAsync();

        MessageHandler messageHandler = new MessageHandler();
        SettingsManager settingsManager = SettingsManager.GetInstance();
        
        _logger.LogInformation("Creating Panel Window");
        var windowPanel = new PhotinoWindow()
            .SetLogVerbosity(2)
            .SetTitle("Panel")
            .SetSize(500, 750)
            .SetUseOsDefaultSize(false)
            .Center()
            .SetResizable(false)
            .RegisterWebMessageReceivedHandler(messageHandler.RegisterWebMessageReceivedHandler);

#if DEBUG
        _logger.LogInformation("Debug Mode, starting dev site");
        
        windowPanel.SetDevToolsEnabled(true);
        windowPanel.Load(new Uri($"http://localhost:5173/#/panel"));
#else
        _logger.LogInformation("Production Mode, starting built site");
        windowPanel.Load($"{baseUrl}/index.html#/panel");
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
        string appFolder = SettingsManager.GetAppFolder();

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
