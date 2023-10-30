using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandOpenScreen: ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        var screenManager = ScreenManager.ScreenManager.GetInstance();

        screenManager.CreateNewScreen();
        
        await Task.Yield();
    }
}