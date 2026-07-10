using ElectronNET.API;

namespace SSSOPanel.MessageParser;
using System.Threading.Tasks;

public interface ICommand
{
    Task Execute(BrowserWindow? sender, object? data);
}
