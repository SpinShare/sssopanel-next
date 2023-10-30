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

    public async Task<List<GithubRelease>> LoadReleases()
    {
        // TODO: Change Owner/Repo
        try
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Add(new("SSSOPanel", CurrentVersion));

            var rawResponse =
                await client.GetStringAsync("https://api.github.com/repos/LauraWebdev/sssopanel-next/releases");
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