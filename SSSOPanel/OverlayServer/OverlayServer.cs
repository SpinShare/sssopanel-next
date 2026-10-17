using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using SSSOPanel.MessageParser;

namespace SSSOPanel.OverlayServer;

/// <summary>
/// Local HTTP server for overlays running outside of Electron, like an OBS browser source.
/// Serves the built UI and a WebSocket that speaks the same messages as Electron IPC.
/// </summary>
public class OverlayServer
{
    public const int Port = 7274;

    private static OverlayServer? _instance;
    private static readonly object Lock = new();

    // Only pages served by us (or the vite dev server) may open the WebSocket
    private static readonly HashSet<string> AllowedOrigins = new(StringComparer.OrdinalIgnoreCase)
    {
        $"http://localhost:{Port}",
        $"http://127.0.0.1:{Port}",
#if DEBUG
        "http://localhost:5173",
        "http://127.0.0.1:5173",
#endif
    };

    private WebApplication? _app;

    public static OverlayServer GetInstance()
    {
        if (_instance == null)
        {
            lock (Lock)
            {
                if (_instance == null)
                {
                    _instance = new OverlayServer();
                }
            }
        }
        return _instance;
    }

    public async Task Start(string wwwrootPath, MessageHandler messageHandler)
    {
        Console.WriteLine("Starting Overlay Server on port {0}", Port);

        try
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.Logging.SetMinimumLevel(LogLevel.Warning);
            builder.WebHost.UseUrls($"http://localhost:{Port}");

            _app = builder.Build();
            _app.UseWebSockets();

            _app.Map("/ws", async (HttpContext context) =>
            {
                if (!context.WebSockets.IsWebSocketRequest || !AllowedOrigins.Contains(context.Request.Headers.Origin.ToString()))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                await HandleScreen(socket, messageHandler);
            });

            if (Directory.Exists(wwwrootPath))
            {
                var fileProvider = new PhysicalFileProvider(wwwrootPath);
                _app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
                _app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
            }

            await _app.StartAsync();
        }
        catch (Exception ex)
        {
            // The panel still works without overlays, e.g. when the port is taken
            Console.WriteLine($"[OverlayServer] Failed to start: {ex.Message}");
            _app = null;
        }
    }

    private static async Task HandleScreen(WebSocket socket, MessageHandler messageHandler)
    {
        var screenManager = ScreenManager.ScreenManager.GetInstance();
        var client = new WebSocketClient(socket);
        screenManager.RegisterScreen(client);

        var buffer = new byte[8192];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                using var message = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                    message.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Close) break;
                if (result.MessageType != WebSocketMessageType.Text) continue;

                try
                {
                    await messageHandler.HandleMessageAsync(client, Encoding.UTF8.GetString(message.ToArray()));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OverlayServer] {ex.Message}");
                }
            }
        }
        catch (Exception)
        {
            // Connection dropped
        }
        finally
        {
            screenManager.UnregisterScreen(client);
        }
    }
}
