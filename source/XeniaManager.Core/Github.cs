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
    
    /// <summary>
    /// Retrieves the contents of the 'patches' directory from the specified repository based on the Xenia version.
    /// </summary>
    /// <param name="xeniaVersion">The version of Xenia to determine which repository to query.</param>
    /// <returns>
    /// An <see cref="IReadOnlyList{RepositoryContent}"/> containing details about each file or directory in the 'patches' folder.
    /// </returns>
    /// <exception cref="Exception">Throws an exception if the GitHub API rate limit is exceeded.</exception>
    public static async Task<IReadOnlyList<RepositoryContent>> GetGamePatches(XeniaVersion xeniaVersion)
    {
        if (!await IsRateLimitAvailableAsync().ConfigureAwait(false))
        {
            throw new Exception("GitHub API rate limit exceeded");
        }

        // Determine repository details based on Xenia version.
        string owner, repo;
        switch (xeniaVersion)
        {
            case XeniaVersion.Canary:
                owner = "xenia-canary";
                repo = "game-patches";
                break;
            case XeniaVersion.Netplay:
                owner = "AdrianCassar";
                repo = "Xenia-WebServices";
                break;
            default:
                throw new NotImplementedException($"Xenia {xeniaVersion} is not supported.");
        }

        try
        {
            // Retrieve the contents of the 'patches' directory.
            IReadOnlyList<RepositoryContent> contents = await _githubClient.Repository.Content.GetAllContents(owner, repo, "patches").ConfigureAwait(false);
            Logger.Info($"Successfully retrieved patches folder contents for {xeniaVersion} from repository '{owner}/{repo}'.");
            return contents;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error retrieving patches folder contents from repository '{owner}/{repo}': {ex}");
            // If it fails return empty list
            return new List<RepositoryContent>();
        }
    }
}