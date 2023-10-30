using Newtonsoft.Json;

namespace SSSOPanel.SettingsManager;

public class SettingsManager
{
    private static SettingsManager? _instance;
    private static readonly object Lock = new();
    private Dictionary<string, object?> _settings = new();
    private readonly string _settingsFilePath;

    public SettingsManager()
    {
        Console.WriteLine("Initializing Settings");
        
        string appFolder = GetAppFolder();

        _settingsFilePath = Path.Combine(appFolder, "settings.json");

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
        if (_settings.TryGetValue(key, out var value))
        {
            return (T)Convert.ChangeType(value, typeof(T))!;
        }

        return default;
    }

    public void Set<T>(string key, T? value)
    {
        _settings[key] = value;
        SaveSettings();
    }

    public bool Exists(string key)
    {
        return _settings.ContainsKey(key);
    }

    public void Clear()
    {
        _settings.Clear();
        SaveSettings();
    }

    public Dictionary<string, object?> GetFull()
    {
        return _settings;
    }

    public static bool SettingsFileExists()
    {
        return File.Exists(Path.Combine(GetAppFolder(), "settings.json"));
    }

    private void LoadSettings()
    {
        string json = File.ReadAllText(_settingsFilePath);
        _settings = JsonConvert.DeserializeObject<Dictionary<string, object?>>(json) ?? new();
    }

    private void SaveSettings()
    {
        string json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
        File.WriteAllText(_settingsFilePath, json);
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