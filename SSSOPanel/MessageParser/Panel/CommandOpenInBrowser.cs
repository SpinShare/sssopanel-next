using Process = System.Diagnostics.Process;
using ElectronNET.API;

namespace SSSOPanel.MessageParser;

public class CommandOpenInBrowser : ICommand
{
    public async Task Execute(BrowserWindow? sender, object? data)
    {
        if (data == null) return;
        var url = data.ToString();

        var openBrowserProcess = new Process();
        openBrowserProcess.StartInfo.UseShellExecute = true;
        openBrowserProcess.StartInfo.FileName = url;
        openBrowserProcess.Start();

        await Task.Yield();
    }
}