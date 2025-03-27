// Imported
using Octokit;

namespace XeniaManager.Core;

public static class Github
{
    // Variables
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

        // Try to use the cached API info.
        ApiInfo apiInfo = _githubClient.GetLastApiInfo();
        if (apiInfo != null)
        {
            remainingRequests = apiInfo.RateLimit?.Remaining ?? 0;
            Logger.Info($"Remaining API requests (from cached info): {remainingRequests}");
        }
        else
        {
            try
            {
                // Fallback: query the API for the latest rate limit.
                MiscellaneousRateLimit rateLimits = await _githubClient.RateLimit.GetRateLimits();
                remainingRequests = rateLimits?.Resources?.Core?.Remaining ?? 0;
                Logger.Info($"Remaining API requests (from API call): {remainingRequests}");
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
    
    /// <summary>
    /// Grabs the latest release for Xenia Canary
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static async Task<string> GetLatestRelease(Xenia xeniaVersion)
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
                                return asset.BrowserDownloadUrl; // Return first release that has Windows version
                            }
                        }
                    }
                    throw new Exception("Couldn't find Xenia Canary release with Windows build.");
                case Xenia.Mousehook:
                    // TODO: Xenia Mousehook GrabLatestRelease
                    //releases = await _githubClient.Repository.Release.GetAll("marinesciencedude", "xenia-canary-releases");
                    //sortedReleases = releases.OrderByDescending(r => r.PublishedAt).ToList();
                    throw new Exception("Couldn't find Xenia Mousehook release with Windows build.");
                case Xenia.Netplay:
                    // TODO: Xenia Netplay GrabLatestRelease
                    //releases = await _githubClient.Repository.Release.GetAll("AdrianCassar", "xenia-canary");
                    //sortedReleases = releases.OrderByDescending(r => r.PublishedAt).ToList();
                    throw new Exception("Couldn't find Xenia Netplay release with Windows build.");
                default:
                    throw new Exception("Unknown Xenia release type.");
            }
        }
        else
        {
            throw new Exception("Rate limit exceeded.");
        }
    }
}