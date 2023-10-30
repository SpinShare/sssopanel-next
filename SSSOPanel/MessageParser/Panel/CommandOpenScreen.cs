using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandOpenScreen: ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        var messageHandler = new MessageHandler();
        var screenManager = ScreenManager.ScreenManager.GetInstance();
        
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
        Console.WriteLine("Production Mode, starting built site");
        windowScreen.Load($"{screenManager.BaseUrl}/index.html#/screen");
#endif
        windowScreen.WaitForClose();
        
        await Task.Yield();
    }

    bool WindowClosingHandler(object sender, EventArgs e)
    {
        var window = (PhotinoWindow)sender;
        var screenManager = ScreenManager.ScreenManager.GetInstance();
        
        screenManager.UnregisterScreen(window);
        return false;
    }
}