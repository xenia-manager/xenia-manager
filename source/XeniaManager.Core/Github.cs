// Imported
using Octokit;

namespace XeniaManager.Core;

/// <summary>
/// Provides methods to interact with the GitHub API for managing and retrieving data
/// such as repository releases and related resources.
/// </summary>
public static class Github
{
    // Variables
    /// <summary>
    /// A static instance of GitHubClient used for interaction with the GitHub API.
    /// </summary>
    private static readonly GitHubClient _githubClient = new GitHubClient(new ProductHeaderValue("Xenia-Manager"));

    /// <summary>
    /// Represents the folder name where game patches are stored in the GitHub repository.
    /// </summary>
    private const string _patchesFolder = "patches";

    /// <summary>
    /// Represents information about a GitHub repository, including its owner, name, and optional asset filter function.
    /// </summary>
    private record RepositoryInfo(string Owner, string Repo, Func<string, bool>? _assetFilter = null);

    /// <summary>
    /// A read-only dictionary that maps each Xenia version to its corresponding repository information,
    /// including owner, repository name, and an optional asset filter function.
    /// Used to retrieve the latest Xenia release.
    /// </summary>
    private static readonly IReadOnlyDictionary<XeniaVersion, RepositoryInfo> _repositoryMappings = new Dictionary<XeniaVersion, RepositoryInfo>
    {
        {
            XeniaVersion.Canary, new RepositoryInfo("xenia-canary", "xenia-canary-releases", a => a.Contains("windows", StringComparison.OrdinalIgnoreCase))
        },
        {
            XeniaVersion.Mousehook, new RepositoryInfo("marinesciencedude", "xenia-canary-releases")
        },
        {
            XeniaVersion.Netplay, new RepositoryInfo("AdrianCassar", "xenia-canary")
        }
    };

    /// <summary>
    /// A dictionary mapping different Xenia versions to their corresponding GitHub repository information.
    /// Used to retrieve game patch data specific to each Xenia version.
    /// </summary>
    private static readonly IReadOnlyDictionary<XeniaVersion, RepositoryInfo> _patchesRepositoryMappings = new Dictionary<XeniaVersion, RepositoryInfo>
    {
        { XeniaVersion.Canary, new RepositoryInfo("xenia-canary", "game-patches") },
        { XeniaVersion.Netplay, new RepositoryInfo("AdrianCassar", "Xenia-WebServices") }
    };


    // Functions
    /// <summary>
    /// Checks if the GitHub API rate limit has been exceeded.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains <c>true</c> if the API rate limit has not been exceeded; otherwise, <c>false</c>.
    /// </returns>
    public static async Task<bool> IsRateLimitAvailableAsync()
    {
        Logger.Info("Checking for remaining Github API requests");

        int remainingRequests = await GetRemainingRequestsCount();
        if (remainingRequests > 0)
        {
            return true;
        }

        Logger.Error("Rate limit exceeded.");
        return false;
    }

    /// <summary>
    /// Retrieves the number of remaining GitHub API requests available using the rate limit information.
    /// </summary>
    /// <returns>
    /// An integer representing the count of remaining API requests. Returns <c>0</c> if the rate limit information cannot be determined.
    /// </returns>
    private static async Task<int> GetRemainingRequestsCount()
    {
        ApiInfo apiInfo = _githubClient.GetLastApiInfo();
        if (apiInfo?.RateLimit != null)
        {
            int remaining = apiInfo.RateLimit.Remaining;
            Logger.Debug($"Remaining API requests (from cached info): {remaining}");
            return remaining;
        }

        try
        {
            MiscellaneousRateLimit rateLimits = await _githubClient.RateLimit.GetRateLimits();
            int remaining = rateLimits?.Resources?.Core?.Remaining ?? 0;
            Logger.Debug($"Remaining API requests (from API call): {remaining}");
            return remaining;
        }
        catch (Exception ex)
        {
            Logger.Error($"Could not get rate limit. \n{ex}");
            return 0;
        }
    }


    /// <summary>
    /// Retrieves the latest release from a specified GitHub repository.
    /// </summary>
    /// <param name="owner">The owner of the GitHub repository.</param>
    /// <param name="repo">The name of the GitHub repository.</param>
    /// <param name="assetFilter">An optional filter function to select specific assets from the release.</param>
    /// <returns>
    /// The latest release from the specified GitHub repository, sorted by release date.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no valid releases are found for the specified repository.
    /// </exception>
    private static async Task<Release> GetRepositoryRelease(string owner, string repo, Func<string, bool>? assetFilter = null)
    {
        IReadOnlyList<Release> releases = await _githubClient.Repository.Release.GetAll(owner, repo).ConfigureAwait(false);
        return releases.OrderByDescending(r => r.PublishedAt)
                   .FirstOrDefault(r => assetFilter == null || r.Assets.Any(a => assetFilter(a.Name)))
               ?? throw new InvalidOperationException($"No valid releases found for {owner}/{repo}");
    }


    /// <summary>
    /// Retrieves the latest release of the specified Xenia version from GitHub.
    /// </summary>
    /// <param name="xeniaVersion">The version of Xenia to retrieve the latest release for.</param>
    /// <returns>
    /// An instance of <see cref="Release"/> representing the latest release of the specified Xenia version.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the GitHub API rate limit is exceeded.</exception>
    /// <exception cref="NotImplementedException">Thrown when the specified Xenia version is not implemented.</exception>
    public static async Task<Release> GetLatestRelease(XeniaVersion xeniaVersion)
    {
        if (!await IsRateLimitAvailableAsync())
        {
            throw new InvalidOperationException("GitHub API rate limit exceeded");
        }

        if (!_repositoryMappings.TryGetValue(xeniaVersion, out var repoInfo))
        {
            throw new NotImplementedException($"Xenia {xeniaVersion} is not implemented.");
        }

        return await GetRepositoryRelease(repoInfo.Owner, repoInfo.Repo, repoInfo._assetFilter);
    }

    /// <summary>
    /// Retrieves the contents of the 'patches' directory from the specified repository based on the Xenia version.
    /// </summary>
    /// <param name="xeniaVersion">The version of Xenia used to determine the repository to query for patches.</param>
    /// <returns>
    /// An <see cref="IReadOnlyList{RepositoryContent}"/> containing details of each file or directory in the 'patches' folder.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if the GitHub API rate limit has been exceeded.</exception>
    /// <exception cref="NotImplementedException">Thrown if the specified Xenia version is not supported.</exception>
    /// <exception cref="Exception">Thrown when any other error occurs during the retrieval process.</exception>
    public static async Task<IReadOnlyList<RepositoryContent>> GetGamePatches(XeniaVersion xeniaVersion)
    {
        if (!await IsRateLimitAvailableAsync())
        {
            throw new InvalidOperationException("GitHub API rate limit exceeded");
        }

        if (!_patchesRepositoryMappings.TryGetValue(xeniaVersion, out var repoInfo))
        {
            throw new NotImplementedException($"Game patches for Xenia {xeniaVersion} are not supported.");
        }

        try
        {
            IReadOnlyList<RepositoryContent> contents = await _githubClient.Repository.Content
                .GetAllContents(repoInfo.Owner, repoInfo.Repo, _patchesFolder)
                .ConfigureAwait(false);

            Logger.Info($"Successfully retrieved patches for {xeniaVersion} from repository '{repoInfo.Owner}/{repoInfo.Repo}'.");
            return contents;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error retrieving patches from repository '{repoInfo.Owner}/{repoInfo.Repo}': {ex}");
            return Array.Empty<RepositoryContent>();
        }
    }
}