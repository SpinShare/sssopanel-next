using System.Diagnostics;
using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandOpenInBrowser : ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
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