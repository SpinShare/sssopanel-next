using System.Diagnostics;
using PhotinoNET;

namespace SSSOPanel.MessageParser;

public class CommandOpenInExplorer : ICommand
{
    public async Task Execute(PhotinoWindow? sender, object? data)
    {
        if (data == null) return;
        var path = data.ToString();
        
        if (!Directory.Exists(path)) return;
        
        var openExplorerProcess = new Process();
        openExplorerProcess.StartInfo.FileName = "explorer.exe";
        openExplorerProcess.StartInfo.Arguments = $@"""{path}""";
        openExplorerProcess.StartInfo.UseShellExecute = false;
        openExplorerProcess.StartInfo.CreateNoWindow = true;
        openExplorerProcess.Start();
        
        await Task.Yield();
    }
}