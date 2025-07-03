using System.Text.Json;

// Imported Libraries
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

    private static readonly HttpClientService _httpClient = new HttpClientService(TimeSpan.FromSeconds(60));

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
            XeniaVersion.Mousehook, new RepositoryInfo("marinesciencedude", "xenia-canary-mousehook", a => !a.Contains("netplay", StringComparison.OrdinalIgnoreCase) && a.Contains("mousehook", StringComparison.OrdinalIgnoreCase))
        },
        {
            XeniaVersion.Netplay, new RepositoryInfo("AdrianCassar", "xenia-canary", a => a.Contains("windows", StringComparison.OrdinalIgnoreCase) && !a.Contains("WSASendTo", StringComparison.OrdinalIgnoreCase))
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

    public static async Task<Release> GetLatestRelease(string repositoryOwner, string repositoryName)
    {
        if (!await IsRateLimitAvailableAsync())
        {
            throw new InvalidOperationException("GitHub API rate limit exceeded");
        }

        return await GetRepositoryRelease(repositoryOwner, repositoryName, null);
    }

    public static async Task<string> GetLatestCommitSha(string repositoryOwner, string repositoryName, string branchName)
    {
        if (!await IsRateLimitAvailableAsync())
        {
            throw new InvalidOperationException("GitHub API rate limit exceeded");
        }

        Branch latestCommit = await _githubClient.Repository.Branch.Get(repositoryOwner, repositoryName, branchName);

        return latestCommit.Commit.Sha.Substring(0, 7);
    }

    /// <summary>
    /// Fetches patch data from a raw GitHub URL and converts it to RepositoryContent objects.
    /// </summary>
    /// <param name="url">The raw GitHub URL to fetch patch data from.</param>
    /// <returns>A list of RepositoryContent objects representing the patch files.</returns>
    private static async Task<IReadOnlyList<RepositoryContent>> FetchPatchesFromUrl(string url)
    {
        try
        {
            string jsonContent = await _httpClient.GetAsync(url);

            // Deserialize directly to anonymous objects using JsonDocument
            using JsonDocument document = JsonDocument.Parse(jsonContent);
            List<RepositoryContent> repositoryContents = new List<RepositoryContent>();

            foreach (JsonElement patch in document.RootElement.EnumerateArray())
            {
                // Extract properties directly from JsonElement
                string name = patch.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty;
                string sha = patch.TryGetProperty("sha", out var shaElement) ? shaElement.GetString() ?? string.Empty : string.Empty;
                int size = patch.TryGetProperty("size", out var sizeElement) ? sizeElement.GetInt32() : 0;
                string downloadUrl = patch.TryGetProperty("download_url", out var downloadElement) ? downloadElement.GetString() ?? string.Empty : string.Empty;
                string gitUrl = patch.TryGetProperty("git_url", out var gitElement) ? gitElement.GetString() ?? string.Empty : string.Empty;
                string htmlUrl = patch.TryGetProperty("html_url", out var htmlElement) ? htmlElement.GetString() ?? string.Empty : string.Empty;
                string patchUrl = patch.TryGetProperty("url", out var urlElement) ? urlElement.GetString() ?? string.Empty : string.Empty;
                string encoding = patch.TryGetProperty("encoding", out var encodingElement) ? encodingElement.GetString() ?? "base64" : "base64";
                string content = patch.TryGetProperty("content", out var contentElement) ? contentElement.GetString() ?? string.Empty : string.Empty;
                string target = patch.TryGetProperty("target", out var targetElement) ? targetElement.GetString() ?? string.Empty : string.Empty;
                string submoduleGitUrl = patch.TryGetProperty("submodule_git_url", out var submoduleElement) ? submoduleElement.GetString() ?? string.Empty : string.Empty;
                repositoryContents.Add(new RepositoryContent(
                    name: name,
                    path: $"{_patchesFolder}/{name}",
                    sha: sha,
                    size: size,
                    type: ContentType.File,
                    downloadUrl: downloadUrl,
                    gitUrl: gitUrl,
                    htmlUrl: htmlUrl,
                    url: patchUrl,
                    encoding: encoding,
                    encodedContent: content,
                    target: target,
                    submoduleGitUrl: submoduleGitUrl
                ));
            }

            Logger.Info($"Successfully fetched {repositoryContents.Count} patches from fallback URL.");
            return repositoryContents.AsReadOnly();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to fetch patches from URL '{url}': {ex}");
            throw new InvalidOperationException("GitHub API rate limit exceeded and no fallback available for this version.");
        }
    }

    /// <summary>
    /// Retrieves the contents of the 'patches' directory from the specified repository based on the Xenia version.
    /// First attempts to fetch from fallback URLs, then falls back to GitHub API if available.
    /// </summary>
    /// <param name="xeniaVersion">The version of Xenia used to determine the repository to query for patches.</param>
    /// <returns>
    /// An <see cref="IReadOnlyList{RepositoryContent}"/> containing details of each file or directory in the 'patches' folder.
    /// </returns>
    /// <exception cref="NotImplementedException">Thrown if the specified Xenia version is not supported.</exception>
    /// <exception cref="InvalidOperationException">Thrown when both fallback URL and GitHub API fail.</exception>
    public static async Task<IReadOnlyList<RepositoryContent>> GetGamePatches(XeniaVersion xeniaVersion)
    {
        if (!_patchesRepositoryMappings.TryGetValue(xeniaVersion, out RepositoryInfo repoInfo))
        {
            throw new NotImplementedException($"Game patches for Xenia {xeniaVersion} are not supported.");
        }

        // First, try to fetch from fallback URLs
        Logger.Info($"Attempting to fetch patches for {xeniaVersion} from fallback URL.");

        try
        {
            string fallbackUrl = xeniaVersion switch
            {
                XeniaVersion.Canary => "https://raw.githubusercontent.com/xenia-manager/Database/refs/heads/main/Database/Patches/canary_patches.json",
                XeniaVersion.Netplay => "https://raw.githubusercontent.com/xenia-manager/Database/refs/heads/main/Database/Patches/netplay_patches.json",
                _ => throw new NotImplementedException($"No fallback URL available for Xenia {xeniaVersion}.")
            };

            return await FetchPatchesFromUrl(fallbackUrl);
        }
        catch (Exception ex)
        {
            Logger.Warning($"Failed to fetch patches from fallback URL: {ex.Message}");
        }

        // Fallback URL failed, try GitHub API if rate limit allows
        Logger.Info("Fallback URL failed. Checking GitHub API rate limit and attempting API fetch.");

        if (!await IsRateLimitAvailableAsync())
        {
            Logger.Error("GitHub API rate limit exceeded and fallback URL failed.");
            throw new InvalidOperationException("Both fallback URL and GitHub API are unavailable. Cannot retrieve patches.");
        }

        try
        {
            IReadOnlyList<RepositoryContent> contents = await _githubClient.Repository.Content
                .GetAllContents(repoInfo.Owner, repoInfo.Repo, _patchesFolder)
                .ConfigureAwait(false);

            Logger.Info($"Successfully retrieved patches for {xeniaVersion} from repository '{repoInfo.Owner}/{repoInfo.Repo}' via GitHub API.");
            return contents;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error retrieving patches from repository '{repoInfo.Owner}/{repoInfo.Repo}': {ex}");
            return [];
        }
    }
}