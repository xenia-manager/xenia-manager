// Imported
using Octokit;

namespace XeniaManager.Core;

/// <summary>
/// Easy interaction with GitHub (Grabbing releases)
/// </summary>
public static class Github
{
    // Variables
    /// <summary>
    /// GitHub Client used to interact with GitHub API
    /// </summary>
    private static readonly GitHubClient _githubClient = new GitHubClient(new ProductHeaderValue("Xenia-Manager"));

    // Functions
    /// <summary>
    /// Checks whether the GitHub API rate limit has not been exceeded.
    /// </summary>
    /// <returns>
    /// <c>true</c> if there are remaining API requests; otherwise, <c>false</c>.
    /// </returns>
    public static async Task<bool> IsRateLimitAvailableAsync()
    {
        int remainingRequests;

        Logger.Info("Checking for remaining Github API requests");
        // Try to use the cached API info.
        ApiInfo apiInfo = _githubClient.GetLastApiInfo();
        if (apiInfo != null)
        {
            remainingRequests = apiInfo.RateLimit?.Remaining ?? 0;
            Logger.Debug($"Remaining API requests (from cached info): {remainingRequests}");
        }
        else
        {
            try
            {
                // Fallback: query the API for the latest rate limit.
                MiscellaneousRateLimit rateLimits = await _githubClient.RateLimit.GetRateLimits();
                remainingRequests = rateLimits?.Resources?.Core?.Remaining ?? 0;
                Logger.Debug($"Remaining API requests (from API call): {remainingRequests}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not get rate limit. \n{ex}");
                return false;
            }
        }

        if (remainingRequests > 0)
        {
            return true;
        }
        else
        {
            Logger.Error("Rate limit exceeded.");
            return false;
        }
    }

    /*
    /// <summary>
    /// Grabs the latest release for Xenia Canary
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static async Task<Release> GetLatestRelease(Xenia xeniaVersion)
    {
        if (await IsRateLimitAvailableAsync())
        {
            IReadOnlyList<Release> releases;
            List<Release> sortedReleases;
            // Grabbing all the releases
            switch (xeniaVersion)
            {
                case Xenia.Canary:
                    releases = await _githubClient.Repository.Release.GetAll("xenia-canary", "xenia-canary-releases");
                    sortedReleases = releases.OrderByDescending(r => r.PublishedAt).ToList();
                    foreach (Release release in sortedReleases)
                    {
                        // Checking if the release has Windows build
                        foreach (ReleaseAsset asset in release.Assets)
                        {
                            if (asset.Name.Contains("windows"))
                            {
                                return release; // Return first release that has Windows version
                            }
                        }
                    }
                    throw new Exception("Couldn't find Xenia Canary release with Windows build.");
                case Xenia.Mousehook:
                    releases = await _githubClient.Repository.Release.GetAll("marinesciencedude", "xenia-canary-releases");
                    sortedReleases = releases.OrderByDescending(r => r.PublishedAt).ToList();
                    if (sortedReleases.Count > 0)
                    {
                        return sortedReleases[0]; // Return latest release
                    }
                    throw new Exception("Couldn't find Xenia Mousehook release with Windows build.");
                case Xenia.Netplay:
                    releases = await _githubClient.Repository.Release.GetAll("AdrianCassar", "xenia-canary");
                    sortedReleases = releases.OrderByDescending(r => r.PublishedAt).ToList();
                    if (sortedReleases.Count > 0)
                    {
                        return sortedReleases[0]; // Return latest release
                    }
                    throw new Exception("Couldn't find Xenia Netplay release with Windows build.");
                default:
                    throw new Exception("Unknown Xenia release type.");
            }
        }
        else
        {
            throw new Exception("Rate limit exceeded.");
        }*/
    
    /// <summary>
    /// Grabs the latest release from the repository
    /// </summary>
    /// <param name="owner">Owner of the repository</param>
    /// <param name="repo">Repository name</param>
    /// <param name="assetFilter">Asset filter</param>
    /// <returns>All releases from a certain repository sorted by release date</returns>
    private static async Task<Release> GetRepositoryRelease(string owner, string repo, Func<string, bool>? assetFilter = null)
    {
        IReadOnlyList<Release> releases = await _githubClient.Repository.Release.GetAll(owner, repo).ConfigureAwait(false);

        return releases.OrderByDescending(r => r.PublishedAt)
                   .FirstOrDefault(r => assetFilter == null || r.Assets.Any(a => assetFilter(a.Name)))
               ?? throw new Exception($"No valid releases found for {owner}/{repo}");
    }
    
    /// <summary>
    /// Grabs the latest release of Xenia
    /// </summary>
    /// <param name="xeniaVersion">Version of Xenia that is being grabbed</param>
    /// <returns></returns>
    /// <exception cref="Exception">GitHub API rate limit exceeded</exception>
    /// <exception cref="NotImplementedException">Xenia version is not implemented</exception>
    public static async Task<Release> GetLatestRelease(XeniaVersion xeniaVersion)
    {
        if (!await IsRateLimitAvailableAsync().ConfigureAwait(false))
        {
            throw new Exception("GitHub API rate limit exceeded");
        }

        return xeniaVersion switch
        {
            XeniaVersion.Canary => await GetRepositoryRelease(
                "xenia-canary",
                "xenia-canary-releases",
                a => a.Contains("windows", StringComparison.OrdinalIgnoreCase)),

            XeniaVersion.Mousehook => await GetRepositoryRelease(
                "marinesciencedude",
                "xenia-canary-releases"),

            XeniaVersion.Netplay => await GetRepositoryRelease(
                "AdrianCassar",
                "xenia-canary"),

            _ => throw new NotImplementedException($"Xenia {xeniaVersion} is not implemented.")
        };
    }
}