using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SSSOPanel.MessageParser;

namespace SSSOPanel.SpultiManager;

/// <summary>
/// Follows one spulti match over its live websocket (coin toss, bans, picks, sets).
/// Keeps the latest match plus its mappool and forwards every change to the panel and the screens.
/// </summary>
public partial class SpultiManager
{
    private const int ReconnectDelay = 5000;

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    private static SpultiManager? _instance;
    private static readonly object Lock = new();

    private CancellationTokenSource? _cancellation;
    private string _url = "";

    public bool Connected { get; private set; }
    public string Error { get; private set; } = "";

    private JToken? _match;
    private JToken? _mappool;

    [GeneratedRegex(@"/matches/([0-9a-f-]{36})", RegexOptions.IgnoreCase)]
    private static partial Regex MatchIdRegex();

    public static SpultiManager GetInstance()
    {
        if (_instance == null)
        {
            lock (Lock)
            {
                if (_instance == null)
                {
                    _instance = new SpultiManager();
                }
            }
        }
        return _instance;
    }

    /// <summary>
    /// Starts following a match, the link is per match so it's kept in memory only. Empty url = stop.
    /// </summary>
    public void Follow(string? url)
    {
        url = (url ?? "").Trim();

        lock (Lock)
        {
            _url = url;

            _cancellation?.Cancel();
            _cancellation = null;
            Error = "";
            Connected = false;
            _match = null;
            _mappool = null;

            if (url != "")
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || !MatchIdRegex().IsMatch(uri.AbsolutePath))
                {
                    Error = "Not a spulti match link";
                }
                else
                {
                    var matchId = MatchIdRegex().Match(uri.AbsolutePath).Groups[1].Value;
                    Console.WriteLine("[SpultiManager] Following match {0}", matchId);
                    _cancellation = new CancellationTokenSource();
                    _ = Run(uri, matchId, _cancellation.Token);
                }
            }
        }

        Broadcast();
    }

    public JObject GetSnapshot()
    {
        lock (Lock)
        {
            return new JObject
            {
                { "url", _url },
                { "connected", Connected },
                { "error", Error },
                { "match", _match?.DeepClone() },
                { "mappool", _mappool?.DeepClone() },
            };
        }
    }

    private async Task Run(Uri page, string matchId, CancellationToken cancellation)
    {
        var origin = page.GetLeftPart(UriPartial.Authority);
        var live = new Uri($"{(page.Scheme == "https" ? "wss" : "ws")}://{page.Authority}/api/live");

        while (!cancellation.IsCancellationRequested)
        {
            using var socket = new ClientWebSocket();
            // spulti only takes sockets from its own site, we send no cookies so we're just an anonymous spectator
            socket.Options.SetRequestHeader("Origin", origin);
            var error = "Connection closed";
            try
            {
                await socket.ConnectAsync(live, cancellation);

                var subscribe = Encoding.UTF8.GetBytes(new JObject { { "type", "subscribe" }, { "matchId", matchId } }.ToString(Newtonsoft.Json.Formatting.None));
                await socket.SendAsync(subscribe, WebSocketMessageType.Text, true, cancellation);

                lock (Lock)
                {
                    // Reconfigured while connecting, the new loop owns the state now
                    if (cancellation.IsCancellationRequested) return;

                    Console.WriteLine("[SpultiManager] Connected");
                    Connected = true;
                    Error = "";
                }
                Broadcast();

                while (true)
                {
                    var message = await Receive(socket, cancellation);
                    if (message?["type"]?.ToString() != "match") continue;
                    await ApplyMatch(origin, message["match"], cancellation);
                }
            }
            catch (OperationCanceledException)
            {
                // Reconfigured or cleared
            }
            catch (Exception ex)
            {
                if (!cancellation.IsCancellationRequested && Connected)
                {
                    Console.WriteLine("[SpultiManager] Disconnected: {0}", ex.Message);
                }
                error = ex.Message;
            }

            bool changed;
            lock (Lock)
            {
                if (cancellation.IsCancellationRequested) return;

                // The last match stays, it's still the best we know
                changed = Connected || Error != error;
                Connected = false;
                Error = error;
            }
            if (changed) Broadcast();

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

    private async Task ApplyMatch(string origin, JToken? match, CancellationToken cancellation)
    {
        // Custom pools get slots while the players build them, so the pool is fetched again on every change
        JToken? mappool = null;
        var mappoolId = match?["mappoolId"]?.ToString();
        if (!string.IsNullOrEmpty(mappoolId))
        {
            try
            {
                mappool = JToken.Parse(await HttpClient.GetStringAsync($"{origin}/api/mappools/{mappoolId}", cancellation));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SpultiManager] Could not load mappool {0}: {1}", mappoolId, ex.Message);
                lock (Lock) mappool = _mappool;
            }
        }

        lock (Lock)
        {
            if (cancellation.IsCancellationRequested) return;
            _match = match;
            _mappool = mappool;
        }

        Broadcast();
    }

    private void Broadcast()
    {
        var response = new
        {
            Command = "spulti-update",
            Data = GetSnapshot(),
        };

        MessageHandler.SendResponse(MessageHandler.PanelClient, response);
        MessageHandler.SendScreenResponse(response);
    }

    private static async Task<JObject?> Receive(ClientWebSocket socket, CancellationToken cancellation)
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

        try
        {
            return JObject.Parse(Encoding.UTF8.GetString(message.ToArray()));
        }
        catch (Exception)
        {
            return null;
        }
    }
}
