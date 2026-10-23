using System.Diagnostics;
using SSSOPanel.MessageParser;

namespace SSSOPanel.VoiceBotManager;

/// <summary>
/// Runs the PharahP discord bot (VoiceBot submodule) as a child process in local mode.
/// In remote mode it only tells the screens which bot to connect to.
/// </summary>
public class VoiceBotManager
{
    public const string DefaultRemoteUrl = "ws://207.180.220.125:8765";
    public const int DefaultPort = 8765;

    private const int MaxLogLines = 50;
    private const int StopTimeout = 3000;

    private static VoiceBotManager? _instance;
    private static readonly object Lock = new();

    private Process? _process;
    private readonly LinkedList<string> _logs = new();

    // What the running bot was started with, a change restarts it
    private string _startedWith = "";

    // Lets the panel send commands to the local bot, screens never get it
    private string _key = "";

    public string Mode { get; private set; } = "local";
    public string Status { get; private set; } = "stopped";
    public string Error { get; private set; } = "";
    private string _url = DefaultRemoteUrl;

    public static VoiceBotManager GetInstance()
    {
        if (_instance == null)
        {
            lock (Lock)
            {
                if (_instance == null)
                {
                    _instance = new VoiceBotManager();
                }
            }
        }
        return _instance;
    }

    /// <summary>
    /// Applies the current settings, (re)starting the local bot when its settings changed.
    /// </summary>
    public void Configure()
    {
        var options = ReadOptions();

        bool start;
        Process? old;
        lock (Lock)
        {
            var modeChanged = Mode != options.Mode;
            Mode = options.Mode;
            _url = options.Url;

            var hasToken = !string.IsNullOrWhiteSpace(options.Token);
            if (Mode != "local") Error = "";
            else if (!hasToken) Error = "No bot token set";

            // A manual stop sticks until something about the bot changes
            start = Mode == "local" && hasToken && (modeChanged || _startedWith != options.Key);
            old = Mode != "local" || !hasToken || start ? Detach() : null;
        }

        Terminate(old);
        if (start) Launch(options);
        Broadcast();
    }

    public void Start()
    {
        var options = ReadOptions();

        Process? old;
        lock (Lock)
        {
            Mode = options.Mode;
            _url = options.Url;
            old = Detach();
        }

        Terminate(old);
        if (Mode != "local")
        {
            // Remote mode, the hosted bot is used instead
            lock (Lock) Error = "";
        }
        else if (string.IsNullOrWhiteSpace(options.Token))
        {
            lock (Lock) Error = "No bot token set";
        }
        else
        {
            Launch(options);
        }
        Broadcast();
    }

    public void Stop()
    {
        Process? old;
        lock (Lock) old = Detach();

        Terminate(old);
        Broadcast();
    }

    public object GetStatus(bool forPanel)
    {
        lock (Lock)
        {
            return new
            {
                status = Status,
                mode = Mode,
                url = _url,
                error = Error,
                logs = _logs.ToArray(),
                key = forPanel && Mode == "local" ? _key : "",
            };
        }
    }

    // AutoJoin is only read on start, picking another channel shouldn't restart the bot
    private record Options(string Mode, string Token, int Port, string NodePath, string Url, string AutoJoin)
    {
        public string Key => $"{Token}|{Port}|{NodePath}";
    }

    private static Options ReadOptions()
    {
        var settings = SettingsManager.SettingsManager.GetInstance().GetFull();

        var mode = settings.GetValueOrDefault("voicebot.mode") as string == "remote" ? "remote" : "local";
        var token = settings.GetValueOrDefault("voicebot.token") as string ?? "";
        var nodePath = settings.GetValueOrDefault("voicebot.nodePath") as string ?? "";

        var port = DefaultPort;
        if (int.TryParse(settings.GetValueOrDefault("voicebot.port")?.ToString(), out var parsedPort) && parsedPort is > 0 and < 65536)
        {
            port = parsedPort;
        }

        var url = mode == "local" ? $"ws://127.0.0.1:{port}" : settings.GetValueOrDefault("voicebot.url") as string ?? "";
        if (string.IsNullOrWhiteSpace(url)) url = DefaultRemoteUrl;

        var autoJoin = settings.GetValueOrDefault("voicebot.autoJoin") is true
            ? settings.GetValueOrDefault("voicebot.voiceChannelId") as string ?? ""
            : "";

        return new Options(mode, token.Trim(), port, nodePath.Trim(), url, autoJoin);
    }

    private static string ResolveNode(string nodePath)
    {
        if (!string.IsNullOrWhiteSpace(nodePath)) return nodePath;

        // Shipped by ci next to the exe, dev falls back to the node on PATH
        var bundled = Path.Combine(AppContext.BaseDirectory, "node", OperatingSystem.IsWindows() ? "node.exe" : "node");
        return File.Exists(bundled) ? bundled : "node";
    }

    private void Launch(Options options)
    {
        lock (Lock)
        {
            _startedWith = options.Key;
            _key = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
            _logs.Clear();
            Error = "";
        }

        var botDir = Path.Combine(AppContext.BaseDirectory, "VoiceBot");
        var script = Path.Combine(botDir, "index.js");
        if (!File.Exists(script))
        {
            lock (Lock)
            {
                Status = "crashed";
                Error = $"Voice bot not found at {script}";
            }
            Console.WriteLine("[VoiceBotManager] Voice bot not found at {0}", script);
            return;
        }

        var dataDir = Path.Combine(SettingsManager.SettingsManager.GetAppFolder(), "voicebot");

        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveNode(options.NodePath),
            WorkingDirectory = botDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add(script);
        // Token goes through the environment, arguments show up in process lists
        startInfo.Environment["token"] = options.Token;
        startInfo.Environment["wssport"] = options.Port.ToString();
        startInfo.Environment["wsshost"] = "127.0.0.1";
        startInfo.Environment["datadir"] = dataDir;
        // The bot exits once our end of stdin closes, also when we crash
        startInfo.Environment["parentwatch"] = "1";
        lock (Lock) startInfo.Environment["wsskey"] = _key;
        if (options.AutoJoin != "") startInfo.Environment["autojoin"] = options.AutoJoin;

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => OnOutput(process, e.Data);
        process.ErrorDataReceived += (_, e) => OnOutput(process, e.Data);
        process.Exited += (_, _) => OnExited(process);

        try
        {
            Console.WriteLine("[VoiceBotManager] Starting {0} {1}", startInfo.FileName, script);
            lock (Lock)
            {
                // Set before reading output so the first lines aren't dropped
                _process = process;
                Status = "starting";
            }
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            lock (Lock)
            {
                _process = null;
                Status = "crashed";
                Error = $"Could not start node: {ex.Message}";
            }
            Console.WriteLine("[VoiceBotManager] Could not start node: {0}", ex.Message);
        }
    }

    /// <summary>
    /// Forgets the running bot so its exit isn't reported as a crash, Terminate it outside the lock.
    /// </summary>
    private Process? Detach()
    {
        var process = _process;
        _process = null;
        Status = "stopped";
        return process;
    }

    private static void Terminate(Process? process)
    {
        if (process == null) return;

        Console.WriteLine("[VoiceBotManager] Stopping");
        try
        {
            // Lets it leave the voice channel properly, see parentwatch
            process.StandardInput.Close();
            if (!process.WaitForExit(StopTimeout))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(StopTimeout);
            }
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            process.Dispose();
        }
    }

    private void OnOutput(Process process, string? line)
    {
        if (line == null) return;
        Console.WriteLine("[VoiceBot] {0}", line);

        var changed = false;
        lock (Lock)
        {
            if (process != _process) return;

            _logs.AddLast(line);
            while (_logs.Count > MaxLogLines) _logs.RemoveFirst();

            // Logged once discord login worked
            if (Status == "starting" && line.StartsWith("Ready!"))
            {
                Status = "running";
                changed = true;
            }
        }

        if (changed) Broadcast();
    }

    private void OnExited(Process process)
    {
        try
        {
            // Exited can beat the last output lines, which hold the reason
            process.WaitForExit();
        }
        catch (Exception)
        {
            // ignored, already disposed by Terminate
        }

        lock (Lock)
        {
            // Stopped on purpose (or replaced by a restart)
            if (process != _process) return;

            int exitCode;
            try
            {
                exitCode = process.ExitCode;
            }
            catch (Exception)
            {
                exitCode = -1;
            }

            Console.WriteLine("[VoiceBotManager] Bot exited with code {0}", exitCode);
            _process = null;
            Status = "crashed";
            Error = _logs.LastOrDefault(l => l.Contains("Failed to log in") || l.Contains("Error")) ?? $"Exited with code {exitCode}";
        }

        Broadcast();
    }

    private void Broadcast()
    {
        MessageHandler.SendResponse(MessageHandler.PanelClient, new
        {
            Command = "voicebot-status-response",
            Data = GetStatus(forPanel: true),
        });
        MessageHandler.SendScreenResponse(new
        {
            Command = "voicebot-status-response",
            Data = GetStatus(forPanel: false),
        });
    }
}
