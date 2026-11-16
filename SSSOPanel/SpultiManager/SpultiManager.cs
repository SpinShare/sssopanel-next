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
    // Where the match list comes from while nothing is followed
    private const string DefaultOrigin = "https://spulti.ellite.dev";

    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    private static SpultiManager? _instance;
    private static readonly object Lock = new();

    private CancellationTokenSource? _cancellation;
    private string _url = "";

    public bool Connected { get; private set; }
    public string Error { get; private set; } = "";

    private JToken? _match;
    private JToken? _mappool;
    // The match's Twitch prediction (chat's pick), null when there's none or it got cancelled
    private JToken? _prediction;

    // The followed match's socket, the screen we show goes back over it so spulti's referee knows what's on stream
    private ClientWebSocket? _socket;
    private string _matchId = "";
    private readonly SemaphoreSlim _sendLock = new(1, 1);

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
            _prediction = null;

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
                { "prediction", _prediction?.DeepClone() },
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
                    _socket = socket;
                    _matchId = matchId;
                }
                Broadcast();
                await SendScene();
                await LoadPrediction(origin, matchId, cancellation);

                while (true)
                {
                    var message = await Receive(socket, cancellation);
                    if (message?["type"]?.ToString() == "chat")
                    {
                        if (message["matchId"]?.ToString() != matchId) continue;
                        // The match chat goes in the ref den, the panel's relay picks what to show
                        MessageHandler.SendResponse(MessageHandler.PanelClient, new { Command = "spulti-chat", Data = message });
                        continue;
                    }
                    if (message?["type"]?.ToString() == "prediction")
                    {
                        if (message["matchId"]?.ToString() != matchId) continue;
                        SetPrediction(message["prediction"], cancellation);
                        continue;
                    }
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
                if (_socket == socket) _socket = null;
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

    /// <summary>
    /// Tells spulti which screen is on stream, with the tournament's stream key from the settings. Without a key it
    /// sends nothing, spulti ignores scenes it can't check. Spulti forgets it once this socket closes
    /// </summary>
    public async Task SendScene()
    {
        ClientWebSocket? socket;
        string matchId;
        lock (Lock)
        {
            socket = _socket;
            matchId = _matchId;
        }

        var key = SettingsManager.SettingsManager.GetInstance().Get<string>("spulti.streamKey") ?? "";
        if (socket is not { State: WebSocketState.Open } || key == "") return;

        var path = ScreenManager.ScreenManager.GetInstance().CurrentRoute.Path;
        var scene = path.StartsWith("/screen/") ? path["/screen/".Length..] : "";
        var message = new JObject { { "type", "scene" }, { "matchId", matchId }, { "key", key }, { "scene", scene == "" ? null : scene } };

        // ClientWebSocket takes one send at a time
        await _sendLock.WaitAsync();
        try
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes(message.ToString(Newtonsoft.Json.Formatting.None)), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[SpultiManager] Could not send the scene: {0}", ex.Message);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// Matches worth following for the panel's dropdown: everything live (casual too, training isn't listed by spulti)
    /// plus the pending ones of running tournaments. From the followed match's site, spulti.ellite.dev otherwise
    /// </summary>
    public async Task<(JArray Matches, string Origin, string Error)> ListMatches()
    {
        string url;
        lock (Lock) url = _url;
        var origin = Uri.TryCreate(url, UriKind.Absolute, out var page) && page.Scheme is "http" or "https" ? page.GetLeftPart(UriPartial.Authority) : DefaultOrigin;

        // Every request on its own, one tournament timing out shouldn't throw away what did load
        var errors = new List<string>();
        async Task<JArray> Fetch(string path)
        {
            try
            {
                return JArray.Parse(await HttpClient.GetStringAsync($"{origin}{path}"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SpultiManager] Could not load {0}: {1}", path, ex.Message);
                lock (errors) errors.Add(ex.Message);
                return new JArray();
            }
        }

        var found = new List<JToken>();
        var live = Fetch("/api/matches/live");
        var tournaments = (await Fetch("/api/tournaments?upcoming=1"))
            .Where(t => t["status"]?.ToString() == "running")
            .Select(t => Fetch($"/api/tournaments/{t["id"]}/matches"))
            .ToList();

        found.AddRange(await live);
        foreach (var matches in tournaments)
        {
            found.AddRange((await matches).Where(m => m["status"]?.ToString() is "pending" or "live" && m["player1"]?.Type == JTokenType.Object && m["player2"]?.Type == JTokenType.Object));
        }

        // Only what the dropdown shows, live ones first then newest
        var list = found
            .Where(m => m["queue"]?.ToString() != "training")
            .GroupBy(m => m["id"]?.ToString())
            .Select(g => g.First())
            .OrderByDescending(m => m["status"]?.ToString() == "live")
            .ThenByDescending(m => m["tournamentId"]?.Type == JTokenType.String)
            .ThenByDescending(m => m["createdAt"]?.ToString())
            .Select(m => new JObject
            {
                { "id", m["id"] },
                { "url", $"{origin}/matches/{m["id"]}" },
                { "status", m["status"] },
                // SelectToken, casual matches have null tournament/startgg and a JSON null can't be indexed
                { "tournament", m.SelectToken("tournament.name") },
                { "round", m.SelectToken("startgg.round") },
                { "bracket", m.SelectToken("startgg.bracket") },
                { "player1", m.SelectToken("player1.name") },
                { "player2", m.SelectToken("player2.name") },
                { "score", m["score"] },
            });
        return (new JArray(list), origin, string.Join(", ", errors.Distinct()));
    }

    /// <summary>
    /// What chat backed so far, live updates only come on changes. Public, spulti only hides it from the match's players
    /// </summary>
    private async Task LoadPrediction(string origin, string matchId, CancellationToken cancellation)
    {
        try
        {
            var body = await HttpClient.GetStringAsync($"{origin}/api/matches/{matchId}/votes", cancellation);
            SetPrediction(JToken.Parse(body), cancellation);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[SpultiManager] Could not load the prediction: {0}", ex.Message);
        }
    }

    private void SetPrediction(JToken? prediction, CancellationToken cancellation)
    {
        lock (Lock)
        {
            // Another match was followed meanwhile, this one's prediction is stale
            if (cancellation.IsCancellationRequested) return;
            // A cancelled one paid nothing out, like spulti's own page it's just gone
            _prediction = prediction is JObject && prediction["status"]?.ToString() != "cancelled" ? prediction : null;
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
