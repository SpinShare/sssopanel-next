using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;

namespace SSSOPanel.ObsManager;

/// <summary>
/// Client for obs-websocket (protocol v5, built into OBS 28+).
/// Used to switch the OBS scene together with the active screen.
/// </summary>
public class ObsManager
{
    public const string DefaultUrl = "ws://localhost:4455";

    private const int ReconnectDelay = 5000;
    private const int RequestTimeout = 3000;

    // Screens take 1s to fade out to a solid color, the scene is switched while it is covered
    private const int SceneSwitchDelay = 1000;

    // obs-websocket OpCodes
    private const int OpHello = 0;
    private const int OpIdentify = 1;
    private const int OpIdentified = 2;
    private const int OpRequest = 6;
    private const int OpRequestResponse = 7;

    private static ObsManager? _instance;
    private static readonly object Lock = new();

    private readonly ConcurrentDictionary<string, TaskCompletionSource<JObject>> _pendingRequests = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cancellation;
    private TaskCompletionSource _firstAttempt = new();
    private string _url = "";
    private string _password = "";

    public bool Enabled { get; private set; }
    public bool Connected { get; private set; }
    public string Error { get; private set; } = "";

    public static ObsManager GetInstance()
    {
        if (_instance == null)
        {
            lock (Lock)
            {
                if (_instance == null)
                {
                    _instance = new ObsManager();
                }
            }
        }
        return _instance;
    }

    /// <summary>
    /// Applies the current settings, (re)connecting when the connection details changed.
    /// </summary>
    public void Configure()
    {
        var settings = SettingsManager.SettingsManager.GetInstance().GetFull();

        var enabled = settings.GetValueOrDefault("obs.enabled") is true;
        var url = settings.GetValueOrDefault("obs.url") as string ?? "";
        var password = settings.GetValueOrDefault("obs.password") as string ?? "";
        if (string.IsNullOrWhiteSpace(url)) url = DefaultUrl;

        lock (Lock)
        {
            if (enabled == Enabled && url == _url && password == _password) return;

            Enabled = enabled;
            _url = url;
            _password = password;

            _cancellation?.Cancel();
            _cancellation = null;
            _firstAttempt = new TaskCompletionSource();

            if (!enabled)
            {
                Error = "";
                _firstAttempt.TrySetResult();
                return;
            }

            Console.WriteLine("[ObsManager] Connecting to {0}", url);
            _cancellation = new CancellationTokenSource();
            _ = Run(url, password, _firstAttempt, _cancellation.Token);
        }
    }

    /// <summary>
    /// Waits until the first connection attempt after <see cref="Configure"/> has succeeded or failed.
    /// </summary>
    public async Task WaitForConnection()
    {
        await Task.WhenAny(_firstAttempt.Task, Task.Delay(RequestTimeout));
    }

    public async Task<List<string>> GetScenes()
    {
        var scenes = new List<string>();

        var response = await Request("GetSceneList");
        if (response?["scenes"] is not JArray sceneList) return scenes;

        // OBS lists scenes bottom to top
        foreach (var scene in sceneList.Reverse())
        {
            var sceneName = scene["sceneName"]?.ToString();
            if (!string.IsNullOrEmpty(sceneName)) scenes.Add(sceneName);
        }

        return scenes;
    }

    public async Task SetScene(string sceneName)
    {
        Console.WriteLine("[ObsManager] Switching to scene: {0}", sceneName);
        await Request("SetCurrentProgramScene", new JObject { { "sceneName", sceneName } });
    }

    /// <summary>
    /// Switches OBS to the scene mapped to a screen, if there is one.
    /// </summary>
    public void SetSceneForScreen(string screen)
    {
        if (!Enabled) return;

        var settings = SettingsManager.SettingsManager.GetInstance().GetFull();
        var key = "obs.scene." + (screen == "" ? "init" : screen);

        if (settings.GetValueOrDefault(key) is not string sceneName || sceneName == "") return;

        _ = Task.Delay(SceneSwitchDelay).ContinueWith(_ => SetScene(sceneName));
    }

    private async Task<JObject?> Request(string requestType, JObject? requestData = null)
    {
        if (!Connected) return null;

        var requestId = Guid.NewGuid().ToString();
        var pending = new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[requestId] = pending;

        try
        {
            var data = new JObject { { "requestType", requestType }, { "requestId", requestId } };
            if (requestData != null) data.Add("requestData", requestData);

            await Send(_socket, OpRequest, data, CancellationToken.None);

            var response = await pending.Task.WaitAsync(TimeSpan.FromMilliseconds(RequestTimeout));
            if (response["requestStatus"]?["result"]?.ToObject<bool>() != true)
            {
                Console.WriteLine("[ObsManager] {0} failed: {1}", requestType, response["requestStatus"]?["comment"]);
                return null;
            }

            return response["responseData"] as JObject ?? new JObject();
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ObsManager] {0} failed: {1}", requestType, ex.Message);
            return null;
        }
        finally
        {
            _pendingRequests.TryRemove(requestId, out _);
        }
    }

    private async Task Run(string url, string password, TaskCompletionSource firstAttempt, CancellationToken cancellation)
    {
        while (!cancellation.IsCancellationRequested)
        {
            using var socket = new ClientWebSocket();
            try
            {
                await socket.ConnectAsync(new Uri(url), cancellation);
                await Identify(socket, password, cancellation);

                Console.WriteLine("[ObsManager] Connected");
                _socket = socket;
                Connected = true;
                Error = "";
                firstAttempt.TrySetResult();

                while (true)
                {
                    var message = await Receive(socket, cancellation);
                    if (message["op"]?.ToObject<int>() != OpRequestResponse) continue;

                    var requestId = message["d"]?["requestId"]?.ToString() ?? "";
                    if (_pendingRequests.TryGetValue(requestId, out var pending))
                    {
                        pending.TrySetResult((JObject)message["d"]!);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Reconfigured or disabled
            }
            catch (Exception ex)
            {
                if (!cancellation.IsCancellationRequested)
                {
                    if (Connected) Console.WriteLine("[ObsManager] Disconnected: {0}", ex.Message);
                    Error = ex.Message;
                }
            }

            // A newer connection may already have taken over
            if (_socket == socket)
            {
                Connected = false;
                _socket = null;
            }
            firstAttempt.TrySetResult();

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

    private async Task Identify(ClientWebSocket socket, string password, CancellationToken cancellation)
    {
        var hello = await Receive(socket, cancellation);
        if (hello["op"]?.ToObject<int>() != OpHello) throw new Exception("Not an obs-websocket server");

        var identify = new JObject { { "rpcVersion", 1 }, { "eventSubscriptions", 0 } };

        if (hello["d"]?["authentication"] is JObject authentication)
        {
            if (password == "") throw new Exception("OBS requires a password");

            var secret = Hash(password + authentication["salt"]);
            identify.Add("authentication", Hash(secret + authentication["challenge"]));
        }

        await Send(socket, OpIdentify, identify, cancellation);

        var identified = await Receive(socket, cancellation);
        if (identified["op"]?.ToObject<int>() != OpIdentified) throw new Exception("OBS did not accept the connection");
    }

    private static string Hash(string value)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private async Task Send(ClientWebSocket? socket, int op, JObject data, CancellationToken cancellation)
    {
        if (socket == null) throw new Exception("Not connected to OBS");

        var message = new JObject { { "op", op }, { "d", data } };
        var bytes = Encoding.UTF8.GetBytes(message.ToString(Newtonsoft.Json.Formatting.None));

        // A WebSocket only allows one send at a time
        await _sendLock.WaitAsync(cancellation);
        try
        {
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellation);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private static async Task<JObject> Receive(ClientWebSocket socket, CancellationToken cancellation)
    {
        var buffer = new byte[8192];
        using var message = new MemoryStream();

        WebSocketReceiveResult result;
        do
        {
            result = await socket.ReceiveAsync(buffer, cancellation);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                // 4009 is obs-websocket's AuthenticationFailed
                throw new Exception((int?)result.CloseStatus == 4009 ? "Wrong OBS password" : result.CloseStatusDescription ?? "Connection closed");
            }

            message.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        return JObject.Parse(Encoding.UTF8.GetString(message.ToArray()));
    }
}
