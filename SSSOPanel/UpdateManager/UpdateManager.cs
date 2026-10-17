using System.Reflection;
using Newtonsoft.Json;

namespace SSSOPanel.UpdateManager;

public class UpdateManager
{
    private static UpdateManager? _instance;
    private static readonly object _lock = new();

    public string CurrentVersion = "0.0.0";

    public UpdateManager()
    {
        Console.WriteLine("Getting current version");
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        CurrentVersion = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
    }

    public static UpdateManager GetInstance()
    {
        if (_instance == null)
        {
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new UpdateManager();
                }
            }
        }
        return _instance;
    }

    // Commands run one at a time, so a slow github must not hold up the panel for long
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private List<GithubRelease>? _cachedReleases;
    private DateTime _cachedAt;

    public async Task<List<GithubRelease>> LoadReleases()
    {
        // Unauthenticated github api is limited to 60 requests an hour, failures are cached too
        if (_cachedReleases != null && DateTime.UtcNow - _cachedAt < CacheDuration) return _cachedReleases;

        _cachedReleases = await FetchReleases();
        _cachedAt = DateTime.UtcNow;
        return _cachedReleases;
    }

    private async Task<List<GithubRelease>> FetchReleases()
    {
        // TODO: Change Owner/Repo
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/LauraWebdev/sssopanel-next/releases");
            request.Headers.UserAgent.Add(new("SSSOPanel", CurrentVersion));

            using var response = await HttpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var rawResponse = await response.Content.ReadAsStringAsync();
            var releases = JsonConvert.DeserializeObject<List<GithubRelease>>(rawResponse);
            Console.WriteLine("Found {0} Releases", releases?.Count ?? 0);

            return releases ?? new List<GithubRelease>();
        }
        catch (Exception e)
        {
            Console.WriteLine("Error while getting latest GitHub Releases: {0}", e?.Message);
            return new List<GithubRelease>();
        }
    }
}