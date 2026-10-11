
namespace SSSOPanel.MessageParser;
using System.Threading.Tasks;

public interface ICommand
{
    Task Execute(IClient? sender, object? data);
}
