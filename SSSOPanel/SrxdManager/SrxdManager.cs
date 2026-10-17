using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json.Linq;
using SSSOPanel.MessageParser;

namespace SSSOPanel.SrxdManager;

/// <summary>
/// Client for the local websocket Spin Rhythm XD hosts itself.
/// Keeps the latest game snapshot and forwards every update to the panel and the screens.
/// </summary>
public class SrxdManager
{
    public const string DefaultUrl = "ws://localhost:6767";

    private const int ReconnectDelay = 5000;

    private static SrxdManager? _instance;
    private static readonly object Lock = new();

    private CancellationTokenSource? _cancellation;
    private string _url = "";

    public bool Enabled { get; private set; }
    public bool Connected { get; private set; }
    public string Error { get; private set; } = "";

    // Same shape as the game's "state" message, plus the hello info
    private JObject _game = EmptyGame();

    public static SrxdManager GetInstance()
    {
        if (_instance == null)
        {
            lock (Lock)
            {
                if (_instance == null)
                {
                    _instance = new SrxdManager();
                }
            }
        }
        return _instance;
    }

    /// <summary>
    /// Applies the current settings, (re)connecting when the connection details changed.
    /// </summary>
    /// <param name="force">Reconnect even when nothing changed</param>
    public void Configure(bool force = false)
    {
        var settings = SettingsManager.SettingsManager.GetInstance().GetFull();

        // On by default, the game only listens locally anyway
        var enabled = settings.GetValueOrDefault("srxd.enabled") is not false;
        var url = settings.GetValueOrDefault("srxd.url") as string ?? "";
        if (string.IsNullOrWhiteSpace(url)) url = DefaultUrl;

        lock (Lock)
        {
            if (!force && enabled == Enabled && url == _url) return;

            Enabled = enabled;
            _url = url;

            _cancellation?.Cancel();
            _cancellation = null;
            Error = "";

            // Reset right away, the cancelled loop leaves the shared state alone so it can't race the new one
            Connected = false;
            _game = EmptyGame();
            Broadcast(new JObject { { "type", "status" } });

            if (!enabled) return;

            Console.WriteLine("[SrxdManager] Connecting to {0}", url);
            _cancellation = new CancellationTokenSource();
            _ = Run(url, _cancellation.Token);
        }
    }

    public JObject GetSnapshot()
    {
        lock (Lock)
        {
            var snapshot = (JObject)_game.DeepClone();
            snapshot["enabled"] = Enabled;
            snapshot["connected"] = Connected;
            snapshot["error"] = Error;
            return snapshot;
        }
    }

    private static JObject EmptyGame()
    {
        return new JObject
        {
            { "hello", null },
            { "room", null },
            { "track", null },
            { "phase", "none" },
            { "round", 0 },
            { "scores", null },
            { "results", null },
        };
    }

    private async Task Run(string url, CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            using var socket = new ClientWebSocket();
            var error = "Connection closed";
            try
            {
                await socket.ConnectAsync(new Uri(url), cancellation);

                lock (Lock)
                {
                    // Reconfigured while connecting, the new loop owns the state now
                    if (cancellation.IsCancellationRequested) return;

                    Console.WriteLine("[SrxdManager] Connected");
                    Connected = true;
                    Error = "";
                }
                Broadcast(new JObject { { "type", "status" } });

                while (true)
                {
                    foreach (var message in await Receive(socket, cancellation))
                    {
                        Apply(message, cancellation);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Reconfigured or disabled
            }
            catch (Exception ex)
            {
                if (!cancellation.IsCancellationRequested && Connected)
                {
                    Console.WriteLine("[SrxdManager] Disconnected: {0}", ex.Message);
                }
                error = ex.Message;
            }

            bool changed;
            lock (Lock)
            {
                if (cancellation.IsCancellationRequested) return;

                // Failed attempts are reported too, so the settings page can show why
                changed = Connected || Error != error;
                Connected = false;
                Error = error;
                _game = EmptyGame();
            }
            if (changed) Broadcast(new JObject { { "type", "status" } });

            try
            {
                await Task.Delay(ReconnectDelay, cancellation);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
        }
    }

    private void Apply(JObject message, CancellationToken cancellation)
    {
        var type = message["type"]?.ToString();

        lock (Lock)
        {
            // A cancelled loop may still be holding one last message
            if (cancellation.IsCancellationRequested) return;

            switch (type)
            {
                case "hello":
                    // Copied, Broadcast adds fields to the message outside the lock
                    _game["hello"] = message.DeepClone();
                    break;
                case "state":
                    foreach (var key in new[] { "room", "track", "phase", "round", "scores", "results" })
                    {
                        _game[key] = message[key];
                    }
                    break;
                case "room":
                case "track":
                case "scores":
                case "results":
                    _game[type] = message[type];
                    break;
                case "phase":
                    _game["phase"] = message["phase"];
                    _game["round"] = message["round"];
                    break;
                default:
                    return;
            }
        }

        Broadcast(message);
    }

    private void Broadcast(JObject message)
    {
        message["enabled"] = Enabled;
        message["connected"] = Connected;
        message["error"] = Error;

        var response = new
        {
            Command = "srxd-update",
            Data = message,
        };

        MessageHandler.SendResponse(MessageHandler.PanelClient, response);
        MessageHandler.SendScreenResponse(response);
    }

    private static async Task<List<JObject>> Receive(ClientWebSocket socket, CancellationToken cancellation)
    {
        var buffer = new byte[16384];
        using var message = new MemoryStream();

        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, cancellation);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                throw new Exception(result.CloseStatusDescription ?? "Connection closed");
            }

            message.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        // One message per frame so far, but the format is newline delimited
        var messages = new List<JObject>();
        foreach (var line in Encoding.UTF8.GetString(message.ToArray()).Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                messages.Add(JObject.Parse(line));
            }
            catch (Exception)
            {
                // ignored
            }
        }

        return messages;
    }
}
