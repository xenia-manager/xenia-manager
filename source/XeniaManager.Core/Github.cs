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
        int remainingRequests = 0;

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
    public static async Task<string> GetLatestRelease(string repoOwner, string repoName)
    {
        if (await IsRateLimitAvailableAsync())
        {
            // Grabbing all the releases
            IReadOnlyList<Release> releases = await _githubClient.Repository.Release.GetAll(repoOwner, repoName);
            List<Release> sortedReleases = releases.OrderByDescending(r => r.PublishedAt).ToList();
            
            // 
            Xenia xeniaVersion = (repoOwner, repoName) switch
            {
                ("xenia-canary","xenia-canary-releases") => Xenia.Canary,
                ("marinesciencedude", "xenia-canary-mousehook") => Xenia.Mousehook,
                ("AdrianCassar", "xenia-canary") => Xenia.Netplay,
                _ => throw new Exception("Unknown repository")
            };

            switch (xeniaVersion)
            {
                case Xenia.Canary:
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
                    break;
                case Xenia.Mousehook:
                    // TODO: Xenia Mousehook GrabLatestRelease
                    throw new Exception("Couldn't find Xenia Mousehook release with Windows build.");
                    break;
                case Xenia.Netplay:
                    // TODO: Xenia Netplay GrabLatestRelease
                    throw new Exception("Couldn't find Xenia Netplay release with Windows build.");
                    break;
                default:
                    break;
            }
        }
        else
        {
            throw new Exception("Rate limit exceeded.");
        }
        throw new Exception("Couldn't find Xenia release with Windows build.");
    }
}