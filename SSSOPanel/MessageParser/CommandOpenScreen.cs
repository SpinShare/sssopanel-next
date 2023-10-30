using PhotinoNET;

namespace SpinShareClient.MessageParser;

public class CommandOpenScreen: ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        MessageHandler messageHandler = new MessageHandler();
        ScreenManager screenManager = ScreenManager.GetInstance();
        
        Console.WriteLine("Creating Screen Window");
        var windowScreen = new PhotinoWindow()
            .SetLogVerbosity(2)
            .SetTitle("Screen")
            .SetSize(1280, 720)
            .SetUseOsDefaultSize(false)
            .Center()
            .SetResizable(false)
            .RegisterWindowClosingHandler(WindowClosingHandler)
            .RegisterWebMessageReceivedHandler(messageHandler.RegisterWebMessageReceivedHandler);
        
        screenManager.RegisterScreen(windowScreen);

#if DEBUG
        Console.WriteLine("Debug Mode, starting dev site");
        
        windowScreen.SetDevToolsEnabled(true);
        windowScreen.Load(new Uri($"http://localhost:5173/#/screen"));
#else
        _logger.LogInformation("Production Mode, starting built site");
        windowScreen.Load($"{baseUrl}/index.html#/screen");
#endif
        windowScreen.WaitForClose();
        
        await Task.Yield();
    }

    bool WindowClosingHandler(object sender, EventArgs e)
    {
        var window = (PhotinoWindow)sender;
        var screenManager = ScreenManager.GetInstance();
        
        screenManager.UnregisterScreen(window);
        return false;
    }
}