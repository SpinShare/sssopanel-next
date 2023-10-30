using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SpinShareClient;

public class SettingsManager
{
    private readonly ILogger<SettingsManager> _logger;

    private static SettingsManager? _instance;
    private static readonly object _lock = new();
    private Dictionary<string, object?> _settings = new();
    private readonly string _settingsFilePath;

    public SettingsManager()
    {
        using var serviceProvider = new ServiceCollection()
            .AddLogging(configure => configure.AddConsole())
            .AddLogging(configure => configure.AddDebug())
            .BuildServiceProvider();
        
        _logger = serviceProvider.GetRequiredService<ILogger<SettingsManager>>();
        
        _logger.LogInformation("Initializing");
        
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

    /// <summary>
    /// Returns an instance of <see cref="SettingsManager"/>
    /// </summary>
    /// <returns><see cref="SettingsManager"/> Instance</returns>
    public static SettingsManager GetInstance()
    {
        if (_instance == null)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new SettingsManager();
                }
            }
        }
        return _instance;
    }

    /// <summary>
    /// Returns a value by key
    /// </summary>
    /// <param name="key">Settings key</param>
    /// <typeparam name="T">Type of the value</typeparam>
    /// <returns>Value or the default value for the type</returns>
    public T? Get<T>(string key)
    {
        if (_settings.TryGetValue(key, out var value))
        {
            return (T)Convert.ChangeType(value, typeof(T))!;
        }

        return default;
    }

    /// <summary>
    /// Sets a value by key
    /// </summary>
    /// <param name="key">Settings key</param>
    /// <param name="value">Settings value</param>
    /// <typeparam name="T">Type of the value</typeparam>
    public void Set<T>(string key, T? value)
    {
        _settings[key] = value;
        SaveSettings();
    }

    /// <summary>
    /// Returns whether a key exists
    /// </summary>
    /// <param name="key">Settings key</param>
    /// <returns>True/False</returns>
    public bool Exists(string key)
    {
        return _settings.ContainsKey(key);
    }

    /// <summary>
    /// Clears all settings and saves
    /// </summary>
    public void Clear()
    {
        _settings.Clear();
        SaveSettings();
    }

    /// <summary>
    /// Returns a <see cref="Dictionary{TKey,TValue}"/> of all settings
    /// </summary>
    /// <returns><see cref="Dictionary{TKey,TValue}"/></returns>
    public Dictionary<string, object?> GetFull()
    {
        return _settings;
    }

    /// <summary>
    /// Returns whether the persistent settings file exists
    /// </summary>
    /// <returns>True/False</returns>
    public static bool SettingsFileExists()
    {
        return File.Exists(Path.Combine(GetAppFolder(), "settings.json"));
    }

    /// <summary>
    /// Loads all settings from the persistent settings file
    /// </summary>
    private void LoadSettings()
    {
        string json = File.ReadAllText(_settingsFilePath);
        _settings = JsonConvert.DeserializeObject<Dictionary<string, object?>>(json) ?? new();
    }

    /// <summary>
    /// Saves all settings to the persistent settings file
    /// </summary>
    private void SaveSettings()
    {
        string json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
        File.WriteAllText(_settingsFilePath, json);
    }

    /// <summary>
    /// Returns the application folder
    /// </summary>
    /// <remarks>If the folder does not exist, it will be created</remarks>
    /// <returns>Path as <see cref="String"/></returns>
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