using System.Text.Json;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Interface for managing manifest cache of Xenia emulator and Xenia Manager releases
/// </summary>
public interface IReleaseService
{
    /// <summary>
    /// Gets the full manifest cache asynchronously
    /// </summary>
    /// <returns>The complete manifest cache</returns>
    Task<ReleaseCache> GetAsync();

    /// <summary>
    /// Gets cached build information for a specific Xenia release type
    /// </summary>
    /// <param name="type">The type of Xenia release to retrieve</param>
    /// <returns>Cached build information or null if not available</returns>
    Task<CachedBuild?> GetCachedBuildAsync(ReleaseType type);

    /// <summary>
    /// Gets cached build information for a specific Xenia Manager release type
    /// </summary>
    /// <param name="type">The type of Xenia Manager release to retrieve</param>
    /// <returns>Manager build information or null if not available</returns>
    Task<ManagerBuild?> GetManagerBuildAsync(ReleaseType type);

    /// <summary>
    /// Gets the current manifest cache (maybe null if not loaded yet)
    /// </summary>
    ReleaseCache? Current { get; }

    /// <summary>
    /// Event raised when the manifest cache is updated
    /// </summary>
    event Action<ReleaseCache>? ManifestUpdated;

    /// <summary>
    /// Forces a refresh of the manifest cache bypassing the cache expiration
    /// </summary>
    /// <returns>The refreshed manifest cache</returns>
    Task ForceRefreshAsync();
}

/// <summary>
/// Service for fetching and caching manifest information about Xenia emulator and Xenia Manager releases
/// Implements caching with automatic refresh and fallback URLs for reliability
/// </summary>
public sealed class ReleaseService : IReleaseService
{
    /// <summary>
    /// List of URLs to try when fetching the manifest, in order of preference
    /// These are fallback URLs that provide the same manifest data from different sources
    /// </summary>
    private static readonly string[] ManifestUrls = Urls.Manifest;

    /// <summary>
    /// Duration after which the cached manifest data expires and needs to be refreshed
    /// Currently set to 7 minutes to balance between fresh data and API usage
    /// </summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(7);

    /// <summary>
    /// HTTP client timeout duration for manifest fetch requests
    /// Set to 15 seconds to prevent hanging requests
    /// </summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// HTTP client instance for making web requests to fetch manifest data
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Semaphore to ensure thread-safe access to the manifest cache
    /// Prevents multiple concurrent refresh operations
    /// </summary>
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    /// <summary>
    /// The currently cached manifest data
    /// </summary>
    private ReleaseCache? _current;

    /// <summary>
    /// Timestamp of the last successful manifest fetch
    /// Used to determine when the cache needs to be refreshed
    /// </summary>
    private DateTimeOffset _lastFetch = DateTimeOffset.MinValue;

    /// <summary>
    /// Gets the current manifest cache (thread-safe)
    /// </summary>
    public ReleaseCache? Current => _current;

    /// <summary>
    /// Event raised when the manifest cache is updated with new data
    /// </summary>
    public event Action<ReleaseCache>? ManifestUpdated;

    /// <summary>
    /// Initializes a new instance of the ManifestService
    /// Sets up the HTTP client with the configured timeout
    /// </summary>
    public ReleaseService()
    {
        Logger.Info<ReleaseService>("Initializing ManifestService");
        _httpClient = new HttpClient { Timeout = Timeout };
        Logger.Debug<ReleaseService>($"Initialized HTTP client with {Timeout.TotalSeconds}s timeout");
    }

    /// <summary>
    /// Gets the full manifest cache, refreshing it if expired
    /// </summary>
    /// <returns>The current manifest cache</returns>
    public async Task<ReleaseCache> GetAsync()
    {
        Logger.Debug<ReleaseService>("Getting manifest cache");

        // Return cached data if it's still valid
        if (_current != null && !IsExpired())
        {
            Logger.Debug<ReleaseService>("Returning cached manifest data");
            return _current;
        }

        Logger.Debug<ReleaseService>("Manifest cache is expired or null, refreshing...");
        // Refresh the cache and return the updated data
        await RefreshAsync();
        Logger.Debug<ReleaseService>("Returning refreshed manifest cache");
        return _current!;
    }

    /// <summary>
    /// Gets cached build information for a specific Xenia release type
    /// Refreshes the cache if it's expired or not yet loaded
    /// </summary>
    /// <param name="type">The type of Xenia release to retrieve</param>
    /// <returns>Cached build information or null if not available</returns>
    public async Task<CachedBuild?> GetCachedBuildAsync(ReleaseType type)
    {
        Logger.Debug<ReleaseService>($"Getting cached build for release type: {type}");

        // Ensure the cache is loaded and not expired
        if (_current == null || IsExpired())
        {
            Logger.Debug<ReleaseService>("Cache is null or expired, refreshing before retrieving build info");
            await RefreshAsync();
        }

        // Return the requested build type based on the enum value
        CachedBuild? result = await (type switch
        {
            ReleaseType.XeniaCanary => Task.FromResult(_current?.XeniaCanary),
            ReleaseType.NetplayStable => Task.FromResult(_current?.NetplayStable),
            ReleaseType.NetplayNightly => Task.FromResult(_current?.NetplayNightly),
            ReleaseType.MousehookStandard => Task.FromResult(_current?.MousehookStandard),
            ReleaseType.MousehookNetplay => Task.FromResult(_current?.MousehookNetplay),
            _ => Task.FromResult<CachedBuild?>(null)
        });

        Logger.Debug<ReleaseService>($"Retrieved build info for {type}: {(result != null ? result.TagName : "null")}");
        return result;
    }

    /// <summary>
    /// Gets cached build information for a specific Xenia Manager release type
    /// Refreshes the cache if it's expired or not yet loaded
    /// </summary>
    /// <param name="type">The type of Xenia Manager release to retrieve</param>
    /// <returns>Manager build information or null if not available</returns>
    public async Task<ManagerBuild?> GetManagerBuildAsync(ReleaseType type)
    {
        Logger.Debug<ReleaseService>($"Getting manager build for release type: {type}");

        // Ensure the cache is loaded and not expired
        if (_current == null || IsExpired())
        {
            Logger.Debug<ReleaseService>("Cache is null or expired, refreshing before retrieving manager build info");
            await RefreshAsync();
        }

        // Return the requested manager build type based on the enum value
        ManagerBuild? result = await (type switch
        {
            ReleaseType.XeniaManagerStable => Task.FromResult(_current?.XeniaManagerStable),
            ReleaseType.XeniaManagerExperimental => Task.FromResult(_current?.XeniaManagerExperimental),
            _ => Task.FromResult<ManagerBuild?>(null)
        });

        Logger.Debug<ReleaseService>($"Retrieved manager build info for {type}: {(result != null ? result.Version : "null")}");
        return result;
    }

    /// <summary>
    /// Checks if the current cache has expired based on the cache duration
    /// </summary>
    /// <returns>True if the cache has expired, false otherwise</returns>
    private bool IsExpired() => DateTimeOffset.UtcNow - _lastFetch >= CacheDuration;

    /// <summary>
    /// Forces a refresh of the manifest cache, bypassing the cache expiration
    /// This method will always fetch fresh data from the manifest URLs
    /// </summary>
    /// <returns>The refreshed manifest cache</returns>
    public async Task ForceRefreshAsync()
    {
        Logger.Info<ReleaseService>("Forcing manifest cache refresh");
        await RefreshAsync();
    }

    /// <summary>
    /// Refreshes the manifest cache by fetching new data from the manifest URLs
    /// Thread-safe implementation using semaphore to prevent concurrent updates
    /// </summary>
    private async Task RefreshAsync()
    {
        Logger.Debug<ReleaseService>("Starting manifest cache refresh");

        // Acquire the lock to ensure only one refresh operation at a time
        await _lock.WaitAsync();
        try
        {
            // Double-check if another thread already refreshed the cache while we were waiting
            if (_current != null && !IsExpired())
            {
                Logger.Debug<ReleaseService>("Another thread already refreshed the cache, skipping refresh");
                return;
            }

            Logger.Debug<ReleaseService>("Attempting to fetch manifest data from URLs");
            // Attempt to fetch manifest data from available URLs
            JsonDocument? manifest = await TryFetchAsync();
            if (manifest != null)
            {
                Logger.Debug<ReleaseService>("Successfully fetched manifest data, building cache");
                // Parse the manifest data and update the cache
                _current = BuildCache(manifest);
                _lastFetch = DateTimeOffset.UtcNow;

                Logger.Info<ReleaseService>("Manifest cache updated successfully");
                // Notify subscribers that the manifest has been updated
                ManifestUpdated?.Invoke(_current);
            }
            else
            {
                Logger.Warning<ReleaseService>("Failed to fetch manifest data from any URL");
            }
        }
        finally
        {
            // Always release the lock to prevent deadlocks
            _lock.Release();
            Logger.Debug<ReleaseService>("Released lock after refresh operation");
        }
    }

    /// <summary>
    /// Attempts to fetch manifest data from the available URLs in order
    /// Tries each URL until one succeeds or all fail
    /// </summary>
    /// <returns>The parsed JSON document from the manifest, or null if all attempts failed</returns>
    private async Task<JsonDocument?> TryFetchAsync()
    {
        Logger.Debug<ReleaseService>($"Attempting to fetch manifest data from {ManifestUrls.Length} URLs");

        foreach (string url in ManifestUrls)
        {
            Logger.Debug<ReleaseService>($"Trying to fetch manifest from URL: {url}");

            try
            {
                // Make an HTTP GET request to the manifest URL
                using HttpResponseMessage response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Warning<ReleaseService>($"Failed to fetch manifest from {url}, status code: {response.StatusCode}");
                    // Skip to the next URL if this one returns an error
                    continue;
                }

                Logger.Debug<ReleaseService>($"Successfully fetched manifest from {url}");
                // Parse the response content as JSON
                await using Stream stream = await response.Content.ReadAsStreamAsync();
                JsonDocument result = await JsonDocument.ParseAsync(stream);
                Logger.Debug<ReleaseService>($"Parsed manifest JSON from {url}");
                return result;
            }
            catch (Exception ex)
            {
                // Ignore exceptions and try the next URL
                Logger.Warning<ReleaseService>($"Exception occurred while fetching manifest from {url}: {ex.Message}");
            }
        }

        Logger.Warning<ReleaseService>("Failed to fetch manifest data from any URL");
        // Return null if all URLs failed
        return null;
    }

    /// <summary>
    /// Builds a ManifestCache object from the parsed JSON manifest document
    /// Extracts information for various Xenia and Xenia Manager releases
    /// </summary>
    /// <param name="doc">The parsed JSON document containing manifest data</param>
    /// <returns>A populated ManifestCache object</returns>
    private static ReleaseCache BuildCache(JsonDocument doc)
    {
        Logger.Debug<ReleaseService>("Building manifest cache from JSON document");

        JsonElement root = doc.RootElement;

        // Helper function to extract build information from a JSON element
        CachedBuild? GetBuild(JsonElement elem)
        {
            // Extract required properties from the JSON element
            if (!elem.TryGetProperty("tag_name", out JsonElement tagProp) ||
                !elem.TryGetProperty("date", out JsonElement dateProp) ||
                !elem.TryGetProperty("url", out JsonElement urlProp))
            {
                Logger.Warning<ReleaseService>("Failed to extract required properties from build element");
                return null;
            }

            // Parse the date string to a DateTime object
            if (!DateTime.TryParse(dateProp.GetString(), out DateTime date))
            {
                Logger.Warning<ReleaseService>("Failed to parse date from build element");
                return null;
            }

            // Create and return a new CachedBuild object
            CachedBuild build = new CachedBuild(tagProp.GetString() ?? "", date, urlProp.GetString() ?? "");
            Logger.Debug<ReleaseService>($"Parsed build: {build.TagName} ({build.Date})");
            return build;
        }

        // Initialize the cache object
        ReleaseCache cache = new ReleaseCache();
        Logger.Debug<ReleaseService>("Initialized empty manifest cache");

        // Extract Xenia Manager version information from the root level
        if (root.TryGetProperty("stable", out JsonElement stableVer) && stableVer.GetString() is { } stableVersion)
        {
            string url = $"https://github.com/xenia-manager/xenia-manager/releases/download/{stableVersion}/xenia_manager.zip";
            cache.XeniaManagerStable = new ManagerBuild(stableVersion, url);
            Logger.Debug<ReleaseService>($"Added stable manager version: {cache.XeniaManagerStable.Version}");
        }

        if (root.TryGetProperty("experimental", out JsonElement expVer) && expVer.GetString() is { } expVersion)
        {
            string url = $"https://github.com/xenia-manager/experimental-builds/releases/download/{expVersion}/xenia_manager.zip";
            cache.XeniaManagerExperimental = new ManagerBuild(expVersion, url);
            Logger.Debug<ReleaseService>($"Added experimental manager version: {cache.XeniaManagerExperimental.Version}");
        }

        // Extract Xenia emulator release information from the xenia section
        if (root.TryGetProperty("xenia", out JsonElement xenia))
        {
            Logger.Debug<ReleaseService>("Processing Xenia section of manifest");

            // Extract Xenia Canary information
            if (xenia.TryGetProperty("canary", out JsonElement canary))
            {
                cache.XeniaCanary = GetBuild(canary);
                Logger.Debug<ReleaseService>($"Added Xenia Canary: {(cache.XeniaCanary != null ? cache.XeniaCanary.TagName : "null")}");
            }

            // Extract Xenia Netplay information (stable and nightly)
            if (xenia.TryGetProperty("netplay", out JsonElement netplay))
            {
                if (netplay.TryGetProperty("stable", out JsonElement stable))
                {
                    cache.NetplayStable = GetBuild(stable);
                    Logger.Debug<ReleaseService>($"Added Netplay Stable: {(cache.NetplayStable != null ? cache.NetplayStable.TagName : "null")}");
                }
                if (netplay.TryGetProperty("nightly", out JsonElement nightly))
                {
                    cache.NetplayNightly = GetBuild(nightly);
                    Logger.Debug<ReleaseService>($"Added Netplay Nightly: {(cache.NetplayNightly != null ? cache.NetplayNightly.TagName : "null")}");
                }
            }

            // Extract Xenia Mousehook information (standard and netplay)
            if (xenia.TryGetProperty("mousehook", out JsonElement mousehook))
            {
                if (mousehook.TryGetProperty("standard", out JsonElement standard))
                {
                    cache.MousehookStandard = GetBuild(standard);
                    Logger.Debug<ReleaseService>($"Added Mousehook Standard: {(cache.MousehookStandard != null ? cache.MousehookStandard.TagName : "null")}");
                }
                if (mousehook.TryGetProperty("netplay", out JsonElement netplayHook))
                {
                    cache.MousehookNetplay = GetBuild(netplayHook);
                    Logger.Debug<ReleaseService>($"Added Mousehook Netplay: {(cache.MousehookNetplay != null ? cache.MousehookNetplay.TagName : "null")}");
                }
            }
        }

        Logger.Info<ReleaseService>($"Built manifest cache with {GetPopulatedCount(cache)} entries");
        return cache;
    }

    /// <summary>
    /// Helper method to count how many entries in the cache are populated
    /// </summary>
    /// <param name="cache">The manifest cache to count</param>
    /// <returns>The number of populated entries in the cache</returns>
    private static int GetPopulatedCount(ReleaseCache cache)
    {
        int count = 0;
        if (cache.XeniaCanary != null)
        {
            count++;
        }
        if (cache.NetplayStable != null)
        {
            count++;
        }
        if (cache.NetplayNightly != null)
        {
            count++;
        }
        if (cache.MousehookStandard != null)
        {
            count++;
        }
        if (cache.MousehookNetplay != null)
        {
            count++;
        }
        if (cache.XeniaManagerStable != null)
        {
            count++;
        }
        if (cache.XeniaManagerExperimental != null)
        {
            count++;
        }
        return count;
    }
}