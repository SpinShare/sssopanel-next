using System.Net.WebSockets;
using System.Text;
using ElectronNET.API;

namespace SSSOPanel.MessageParser;

/// <summary>
/// A connected frontend that messages can be sent to.
/// The panel talks over Electron IPC, overlays (OBS browser sources) talk over a WebSocket.
/// </summary>
public interface IClient
{
    bool IsPanel { get; }
    void Send(string json);
}

public class ElectronClient : IClient
{
    private readonly BrowserWindow _window;

    public ElectronClient(BrowserWindow window)
    {
        _window = window;
    }

    public bool IsPanel => true;

    public void Send(string json)
    {
        Electron.IpcMain.Send(_window, "message", json);
    }
}

public class WebSocketClient : IClient
{
    private readonly WebSocket _socket;
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public WebSocketClient(WebSocket socket)
    {
        _socket = socket;
    }

    public bool IsPanel => false;

    public void Send(string json)
    {
        _ = SendAsync(json);
    }

    private async Task SendAsync(string json)
    {
        // A WebSocket only allows one send at a time
        await _sendLock.WaitAsync();
        try
        {
            if (_socket.State != WebSocketState.Open) return;

            await _socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            _sendLock.Release();
        }
    }
}
