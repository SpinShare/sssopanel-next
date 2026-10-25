using Microsoft.AspNetCore.DataProtection;
using Newtonsoft.Json;

namespace SSSOPanel.SettingsManager;

public class SettingsManager
{
    private static SettingsManager? _instance;
    private static readonly object Lock = new();
    private Dictionary<string, object?> _settings = new();
    // MusicManager reads from a background thread while commands write
    private readonly object _settingsLock = new();
    private readonly string _settingsFilePath;

    // Only these are encrypted on disk, in memory everything stays plaintext for the managers
    private static readonly string[] SecretKeys = ["voicebot.token", "obs.password", "currentEvent.startgg.apiToken"];
    private const string EncryptedPrefix = "enc:";
    private readonly IDataProtector _protector;

    public SettingsManager()
    {
        Console.WriteLine("Initializing Settings");
        
        string appFolder = GetAppFolder();

        _settingsFilePath = Path.Combine(appFolder, "settings.json");
        _protector = CreateProtector(appFolder);

        if (File.Exists(_settingsFilePath))
        {
            LoadSettings();
        }
        else
        {
            _settings = new Dictionary<string, object?>();
        }
    }

    public static SettingsManager GetInstance()
    {
        if (_instance == null)
        {
            lock (Lock)
            {
                if (_instance == null)
                {
                    _instance = new SettingsManager();
                }
            }
        }
        return _instance;
    }

    public T? Get<T>(string key)
    {
        lock (_settingsLock)
        {
            if (_settings.TryGetValue(key, out var value))
            {
                return (T)Convert.ChangeType(value, typeof(T))!;
            }
        }

        return default;
    }

    public void Set<T>(string key, T? value)
    {
        lock (_settingsLock)
        {
            _settings[key] = value;
            SaveSettings();
        }
    }

    public bool Exists(string key)
    {
        lock (_settingsLock)
        {
            return _settings.ContainsKey(key);
        }
    }

    public void Clear()
    {
        lock (_settingsLock)
        {
            _settings.Clear();
            SaveSettings();
        }
    }

    // A copy, so callers can read it while settings are written
    public Dictionary<string, object?> GetFull()
    {
        lock (_settingsLock)
        {
            return new Dictionary<string, object?>(_settings);
        }
    }

    // For the screens, they never need the secrets and anything on the overlay port could ask for them
    public Dictionary<string, object?> GetFullWithoutSecrets()
    {
        lock (_settingsLock)
        {
            return _settings.Where(setting => !SecretKeys.Contains(setting.Key)).ToDictionary();
        }
    }

    public static bool SettingsFileExists()
    {
        return File.Exists(Path.Combine(GetAppFolder(), "settings.json"));
    }

    private void LoadSettings()
    {
        string json = File.ReadAllText(_settingsFilePath);
        _settings = JsonConvert.DeserializeObject<Dictionary<string, object?>>(json) ?? new();

        var hasPlaintextSecret = false;
        foreach (var key in SecretKeys)
        {
            if (_settings.GetValueOrDefault(key) is not string value || value == "") continue;
            if (!value.StartsWith(EncryptedPrefix))
            {
                hasPlaintextSecret = true;
                continue;
            }

            try
            {
                _settings[key] = _protector.Unprotect(value[EncryptedPrefix.Length..]);
            }
            catch (Exception)
            {
                // Keys are gone or the file came from another machine/user, it has to be entered again
                Console.WriteLine("[Settings] Could not decrypt {0}, clearing it", key);
                _settings[key] = "";
            }
        }

        // Settings from before encryption get rewritten right away
        if (hasPlaintextSecret) SaveSettings();
    }

    private void SaveSettings()
    {
        var onDisk = new Dictionary<string, object?>(_settings);
        foreach (var key in SecretKeys)
        {
            if (onDisk.GetValueOrDefault(key) is string value && value != "")
            {
                onDisk[key] = EncryptedPrefix + _protector.Protect(value);
            }
        }

        string json = JsonConvert.SerializeObject(onDisk, Formatting.Indented);
        File.WriteAllText(_settingsFilePath, json);
        RestrictToUser(_settingsFilePath);
    }

    // DPAPI ties the keys to the windows user. Elsewhere the key ring is a plain file next to
    // settings.json, so there it only keeps the token out of casual view (screenshots, sharing the file)
    private static IDataProtector CreateProtector(string appFolder)
    {
        var keysFolder = new DirectoryInfo(Path.Combine(appFolder, "keys"));
        var provider = DataProtectionProvider.Create(keysFolder, builder =>
        {
            builder.SetApplicationName("SSSOPanel");
            if (OperatingSystem.IsWindows()) builder.ProtectKeysWithDpapi();
        });
        var protector = provider.CreateProtector("SSSOPanel.Settings");

        // Creates the key ring now so its files can be locked down
        protector.Protect("");
        RestrictToUser(keysFolder.FullName);
        foreach (var file in keysFolder.EnumerateFiles()) RestrictToUser(file.FullName);

        return protector;
    }

    private static void RestrictToUser(string path)
    {
        if (OperatingSystem.IsWindows()) return;
        try
        {
            File.SetUnixFileMode(path, Directory.Exists(path)
                ? UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                : UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (Exception)
        {
            // ignored
        }
    }

    public static string GetAppFolder()
    {
        string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appFolder = Path.Combine(folder, "SSSOPanel");

        if (!Directory.Exists(appFolder))
        {
            Directory.CreateDirectory(appFolder);
        }

        return appFolder;
    }
}