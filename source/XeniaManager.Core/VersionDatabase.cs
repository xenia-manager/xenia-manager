using System.Text.Json;
using System.Text.Json.Serialization;
using Octokit;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Enum;

namespace XeniaManager.Core;

/// <summary>
/// Provides methods to fetch version information from the Xenia Manager version database JSON endpoint.
/// This class fetches data from https://xenia-manager.github.io/database/data/version.json
/// </summary>
public static class VersionDatabase
{
    /// <summary>
    /// Represents the folder name where game patches are stored in the GitHub repository.
    /// </summary>
    private const string _patchesFolder = "patches";

    private static readonly HttpClientService _httpClient = new HttpClientService(TimeSpan.FromSeconds(60));

    /// <summary>
    /// JSON model for version information
    /// </summary>
    private record VersionInfo(
        [property: JsonPropertyName("tag_name")]
        string TagName,
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("date")] string? Date = null,
        [property: JsonPropertyName("commit_sha")]
        string? CommitSha = null
    );

    /// <summary>
    /// JSON model for Xenia Manager version data
    /// </summary>
    private record XeniaManagerVersionData(
        [property: JsonPropertyName("stable")] VersionInfo Stable,
        [property: JsonPropertyName("experimental")]
        VersionInfo Experimental
    );

    /// <summary>
    /// JSON model for Xenia version data
    /// </summary>
    private record XeniaVersionData(
        [property: JsonPropertyName("canary")] VersionInfo Canary,
        [property: JsonPropertyName("netplay")]
        XeniaNetplayVersionData Netplay,
        [property: JsonPropertyName("mousehook")]
        XeniaMousehookVersionData Mousehook
    );

    /// <summary>
    /// JSON model for Xenia Netplay version data
    /// </summary>
    private record XeniaNetplayVersionData(
        [property: JsonPropertyName("stable")] VersionInfo Stable,
        [property: JsonPropertyName("nightly")]
        VersionInfo Nightly
    );

    /// <summary>
    /// JSON model for Xenia Mousehook version data
    /// </summary>
    private record XeniaMousehookVersionData(
        [property: JsonPropertyName("standard")]
        VersionInfo Standard,
        [property: JsonPropertyName("netplay")]
        VersionInfo Netplay
    );

    /// <summary>
    /// Root JSON model for the version database
    /// </summary>
    private record VersionDatabaseRoot(
        [property: JsonPropertyName("xenia_manager")]
        XeniaManagerVersionData XeniaManager,
        [property: JsonPropertyName("xenia")] XeniaVersionData Xenia
    );

    /// <summary>
    /// Cached version database
    /// </summary>
    private static VersionDatabaseRoot? _cachedVersionDatabase;

    private static DateTime _cacheTimestamp;
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Fetches and caches the version database from the JSON endpoint
    /// </summary>
    private static async Task<VersionDatabaseRoot> GetVersionDatabaseAsync()
    {
        // Return the cached version if still valid
        if (_cachedVersionDatabase != null && DateTime.Now - _cacheTimestamp < _cacheDuration)
        {
            return _cachedVersionDatabase;
        }

        try
        {
            string jsonContent = await _httpClient.GetAsync(Urls.LatestVersions);

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            _cachedVersionDatabase = JsonSerializer.Deserialize<VersionDatabaseRoot>(jsonContent, options)
                                     ?? throw new InvalidOperationException("Failed to deserialize version database");

            _cacheTimestamp = DateTime.Now;
            return _cachedVersionDatabase;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to fetch version database: {ex}");
            throw new InvalidOperationException("Failed to fetch version database from JSON endpoint", ex);
        }
    }

    /// <summary>
    /// Converts VersionInfo to an Octokit Release object for compatibility with existing code.
    /// </summary>
    private static Release CreateReleaseFromVersionInfo(VersionInfo versionInfo)
    {
        DateTimeOffset releaseDate = string.IsNullOrEmpty(versionInfo.Date)
            ? DateTimeOffset.Now
            : DateTimeOffset.Parse(versionInfo.Date);

        string assetName = Path.GetFileName(new Uri(versionInfo.Url).AbsolutePath) ?? "xenia.zip";

        ReleaseAsset asset = new ReleaseAsset(
            url: versionInfo.Url,
            id: 0,
            nodeId: string.Empty,
            name: assetName,
            label: string.Empty,
            state: "uploaded",
            contentType: "application/zip",
            size: 0,
            downloadCount: 0,
            createdAt: releaseDate,
            updatedAt: releaseDate,
            browserDownloadUrl: versionInfo.Url,
            uploader: null);

        return new Release(
            url: versionInfo.Url,
            htmlUrl: versionInfo.Url,
            assetsUrl: versionInfo.Url,
            uploadUrl: string.Empty,
            id: 0,
            nodeId: string.Empty,
            tagName: versionInfo.TagName,
            targetCommitish: string.Empty,
            name: versionInfo.TagName,
            body: string.Empty,
            draft: false,
            prerelease: false,
            createdAt: releaseDate,
            publishedAt: releaseDate,
            author: null,
            tarballUrl: string.Empty,
            zipballUrl: versionInfo.Url,
            assets: new List<ReleaseAsset> { asset }.AsReadOnly());
    }

    /// <summary>
    /// Retrieves the latest release for the specified Xenia version from the version database.
    /// Returns an Octokit Release object for compatibility with existing code.
    /// </summary>
    /// <param name="xeniaVersion">The version of Xenia to retrieve the latest release for.</param>
    /// <param name="useNightlyBuild">For Netplay, if true, returns nightly build; otherwise, returns stable build.</param>
    /// <returns>
    /// An instance of <see cref="Release"/> representing the latest release of the specified Xenia version.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the version data cannot be retrieved.</exception>
    /// <exception cref="NotImplementedException">Thrown when the specified Xenia version is not implemented.</exception>
    public static async Task<Release> GetLatestRelease(XeniaVersion xeniaVersion, bool useNightlyBuild = false)
    {
        VersionDatabaseRoot database = await GetVersionDatabaseAsync();

        VersionInfo versionInfo = xeniaVersion switch
        {
            XeniaVersion.Canary => database.Xenia.Canary,
            XeniaVersion.Mousehook => database.Xenia.Mousehook.Standard,
            XeniaVersion.Netplay => useNightlyBuild ? database.Xenia.Netplay.Nightly : database.Xenia.Netplay.Stable,
            _ => throw new NotImplementedException($"Xenia {xeniaVersion} is not implemented.")
        };

        return CreateReleaseFromVersionInfo(versionInfo);
    }

    /// <summary>
    /// Retrieves the latest release for Xenia Manager from the version database.
    /// Returns an Octokit Release object for compatibility with existing code.
    /// </summary>
    /// <param name="useExperimental">If true, returns experimental version; otherwise, returns stable version.</param>
    /// <returns>
    /// An instance of <see cref="Release"/> representing the latest Xenia Manager release.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the version data cannot be retrieved.</exception>
    public static async Task<Release> GetManagerRelease(bool useExperimental = false)
    {
        VersionDatabaseRoot database = await GetVersionDatabaseAsync();

        VersionInfo versionInfo = useExperimental
            ? database.XeniaManager.Experimental
            : database.XeniaManager.Stable;

        return CreateReleaseFromVersionInfo(versionInfo);
    }

    /// <summary>
    /// Retrieves the latest commit SHA for Netplay builds from the version database.
    /// </summary>
    /// <param name="useNightlyBuild">If true, returns nightly commit SHA; otherwise, returns stable tag.</param>
    /// <returns>
    /// The latest commit SHA (first 7 characters) or tag.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the version data cannot be retrieved.</exception>
    public static async Task<string> GetLatestCommitSha(bool useNightlyBuild = true)
    {
        VersionDatabaseRoot database = await GetVersionDatabaseAsync();

        // Get the version info based on the build type
        VersionInfo versionInfo = useNightlyBuild ? database.Xenia.Netplay.Nightly : database.Xenia.Netplay.Stable;

        // If commit_sha is available in the JSON, use it
        if (!string.IsNullOrEmpty(versionInfo.CommitSha))
        {
            return versionInfo.CommitSha.Substring(0, Math.Min(7, versionInfo.CommitSha.Length));
        }

        // Otherwise, extract from tag_name (format: "758e1ac" or similar)
        return versionInfo.TagName.Substring(0, Math.Min(7, versionInfo.TagName.Length));
    }

    /// <summary>
    /// Retrieves the release date for the specified Xenia version from the version database.
    /// </summary>
    /// <param name="xeniaVersion">The version of Xenia to retrieve the release date for.</param>
    /// <param name="useNightlyBuild">For Netplay, if true, returns nightly build date; otherwise, returns stable build date.</param>
    /// <returns>
    /// The release date as a DateTimeOffset, or null if not available.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the version data cannot be retrieved.</exception>
    /// <exception cref="NotImplementedException">Thrown when the specified Xenia version is not implemented.</exception>
    public static async Task<DateTimeOffset?> GetReleaseDate(XeniaVersion xeniaVersion, bool useNightlyBuild = false)
    {
        VersionDatabaseRoot database = await GetVersionDatabaseAsync();

        VersionInfo versionInfo = xeniaVersion switch
        {
            XeniaVersion.Canary => database.Xenia.Canary,
            XeniaVersion.Mousehook => database.Xenia.Mousehook.Standard,
            XeniaVersion.Netplay => useNightlyBuild ? database.Xenia.Netplay.Nightly : database.Xenia.Netplay.Stable,
            _ => throw new NotImplementedException($"Xenia {xeniaVersion} is not implemented.")
        };

        if (string.IsNullOrEmpty(versionInfo.Date))
        {
            return null;
        }

        return DateTimeOffset.Parse(versionInfo.Date);
    }

    /// <summary>
    /// Retrieves the release date for Xenia Manager from the version database.
    /// </summary>
    /// <param name="useExperimental">If true, returns experimental version date; otherwise, returns stable version date.</param>
    /// <returns>
    /// The release date as a DateTimeOffset, or null if not available.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when the version data cannot be retrieved.</exception>
    public static async Task<DateTimeOffset?> GetManagerReleaseDate(bool useExperimental = false)
    {
        VersionDatabaseRoot database = await GetVersionDatabaseAsync();

        VersionInfo versionInfo = useExperimental
            ? database.XeniaManager.Experimental
            : database.XeniaManager.Stable;

        if (string.IsNullOrEmpty(versionInfo.Date))
        {
            return null;
        }

        return DateTimeOffset.Parse(versionInfo.Date);
    }

    /// <summary>
    /// Invalidates the cached version database, forcing a fresh fetch on the next call.
    /// </summary>
    public static void InvalidateCache()
    {
        _cachedVersionDatabase = null;
        _cacheTimestamp = DateTime.MinValue;
    }

    /// <summary>
    /// Fetches patch data from a raw GitHub URL and converts it to RepositoryContent objects.
    /// </summary>
    /// <param name="url">The raw GitHub URL to fetch patch data from.</param>
    /// <returns>A list of RepositoryContent objects representing the patch files, or an empty list if the fetch fails.</returns>
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
                    type: Octokit.ContentType.File,
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

            Logger.Info($"Successfully fetched {repositoryContents.Count} patches from JSON endpoint.");
            return repositoryContents.AsReadOnly();
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to fetch patches from URL '{url}': {ex}");
            return [];
        }
    }

    /// <summary>
    /// Retrieves the game patches for the specified Xenia version from the version database.
    /// Fetches patch data from the JSON endpoint.
    /// </summary>
    /// <param name="xeniaVersion">The version of Xenia used to determine the repository to query for patches.</param>
    /// <returns>
    /// An <see cref="IReadOnlyList{RepositoryContent}"/> containing details of each file or directory in the 'patches' folder.
    /// </returns>
    /// <exception cref="NotImplementedException">Thrown if the specified Xenia version is not supported.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the JSON endpoint fetch fails.</exception>
    public static async Task<IReadOnlyList<RepositoryContent>> GetGamePatches(XeniaVersion xeniaVersion)
    {
        Logger.Info($"Attempting to fetch patches for {xeniaVersion} from JSON endpoint.");

        string fallbackUrl = xeniaVersion switch
        {
            XeniaVersion.Canary => "https://xenia-manager.github.io/database/data/patches/canary.json",
            XeniaVersion.Netplay => "https://xenia-manager.github.io/database/data/patches/netplay.json",
            _ => throw new NotImplementedException($"No fallback URL available for Xenia {xeniaVersion}.")
        };

        return await FetchPatchesFromUrl(fallbackUrl);
    }
}