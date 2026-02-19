using System.Text.Json;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Database;
using XeniaManager.Core.Models.Database.Patches;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Database;

/// <summary>
/// Handles the loading and searching of patch files from the patches database.
/// Provides functionality to load patch databases for both Canary and Netplay versions,
/// search for patches by name or SHA, and retrieve patch information.
/// Implements caching for API responses with 1-day expiration.
/// </summary>
public class PatchesDatabase
{
    /// <summary>
    /// Cache duration for API responses (1 day)
    /// </summary>
    private static readonly TimeSpan ApiCacheDuration = TimeSpan.FromDays(1);

    /// <summary>
    /// State for each patch database variant
    /// </summary>
    private static readonly Dictionary<PatchDatabaseType, PatchDatabaseState> _databaseStates = new Dictionary<PatchDatabaseType, PatchDatabaseState>
    {
        [PatchDatabaseType.Canary] = new PatchDatabaseState(),
        [PatchDatabaseType.Netplay] = new PatchDatabaseState()
    };

    /// <summary>
    /// HttpClient used to fetch the database
    /// Reuses the same client instance for efficiency and connection pooling
    /// </summary>
    private static readonly HttpClientService _client = new HttpClientService();

    /// <summary>
    /// Fallback URLs for each patch database variant
    /// </summary>
    private static readonly Dictionary<PatchDatabaseType, string[]> _databaseUrls = new Dictionary<PatchDatabaseType, string[]>
    {
        [PatchDatabaseType.Canary] = Urls.PatchesDatabase.CanaryPatches,
        [PatchDatabaseType.Netplay] = Urls.PatchesDatabase.NetplayPatches
    };

    /// <summary>
    /// Gets the filtered Canary patches database (used for displaying patches after search)
    /// </summary>
    public static List<PatchInfo> CanaryFilteredDatabase
    {
        get => _databaseStates[PatchDatabaseType.Canary].FilteredDatabase;
        private set => _databaseStates[PatchDatabaseType.Canary].FilteredDatabase = value;
    }

    /// <summary>
    /// Gets the filtered Netplay patches database (used for displaying patches after search)
    /// </summary>
    public static List<PatchInfo> NetplayFilteredDatabase
    {
        get => _databaseStates[PatchDatabaseType.Netplay].FilteredDatabase;
        private set => _databaseStates[PatchDatabaseType.Netplay].FilteredDatabase = value;
    }

    /// <summary>
    /// Loads the Canary patches database from the marketplace into memory.
    /// The database is only loaded once; further calls will be skipped if already loaded.
    /// Response is cached for 1 day to reduce API calls.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <exception cref="AggregateException">Thrown when all database URLs fail to provide data</exception>
    public static async Task LoadCanaryAsync(CancellationToken cancellationToken = default)
        => await LoadDatabaseAsync(PatchDatabaseType.Canary, cancellationToken);

    /// <summary>
    /// Loads the Netplay patches database from the marketplace into memory.
    /// The database is only loaded once; further calls will be skipped if already loaded.
    /// Response is cached for 1 day to reduce API calls.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <exception cref="AggregateException">Thrown when all database URLs fail to provide data</exception>
    public static async Task LoadNetplayAsync(CancellationToken cancellationToken = default)
        => await LoadDatabaseAsync(PatchDatabaseType.Netplay, cancellationToken);

    /// <summary>
    /// Loads a patch database variant from the marketplace into memory.
    /// </summary>
    /// <param name="type">The database variant to load (Canary or Netplay)</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <exception cref="AggregateException">Thrown when all database URLs fail to provide data</exception>
    private static async Task LoadDatabaseAsync(PatchDatabaseType type, CancellationToken cancellationToken)
    {
        PatchDatabaseState state = _databaseStates[type];

        if (state.IsLoaded)
        {
            Logger.Debug<PatchesDatabase>($"{type} patches database already loaded, skipping");
            return;
        }

        Logger.Info<PatchesDatabase>($"Loading {type} patches database");

        string cacheKey = $"{type.ToString().ToLowerInvariant()}_patches";
        string[] urls = _databaseUrls[type];
        string? response = null;

        foreach (string url in urls)
        {
            try
            {
                response = await _client.GetAsync(url, cancellationToken, cacheKey: cacheKey, cacheDuration: ApiCacheDuration, cacheDirectory: AppPaths.PatchesCacheDirectory);
                Logger.Info<PatchesDatabase>($"Successfully fetched from: {url}");
                break;
            }
            catch (Exception ex)
            {
                Logger.Warning<PatchesDatabase>($"Failed to fetch from '{url}'");
                Logger.LogExceptionDetails<PatchesDatabase>(ex);
            }
        }

        if (response == null)
        {
            Logger.Error<PatchesDatabase>($"All {urls.Length} URLs failed to provide data");
            return;
        }

        Logger.Debug<PatchesDatabase>("Deserializing JSON data");

        List<PatchInfo>? patches = JsonSerializer.Deserialize<List<PatchInfo>>(response);

        if (patches is null || patches.Count == 0)
        {
            Logger.Warning<PatchesDatabase>("Patches database was empty or failed to deserialize.");
            return;
        }

        foreach (PatchInfo patch in patches)
        {
            if (patch.Name != null)
            {
                AddPatchToIndex(patch, state);
            }
        }

        state.IsLoaded = true;
        state.FilteredDatabase = state.PatchNameMap.Values.ToList();
        Logger.Info<PatchesDatabase>($"{type} patches loaded: {state.PatchNameMap.Count} patches");
    }

    /// <summary>
    /// Adds a patch to the Canary index.
    /// The patch name is normalized to uppercase for consistent comparisons.
    /// </summary>
    /// <param name="patch">The PatchInfo object to add to the index</param>
    public static void AddPatchToIndex(PatchInfo patch)
        => AddPatchToIndex(patch, _databaseStates[PatchDatabaseType.Canary]);

    /// <summary>
    /// Adds a patch to the Netplay index.
    /// The patch name is normalized to uppercase for consistent comparisons.
    /// This method is primarily used for testing purposes.
    /// </summary>
    /// <param name="patch">The PatchInfo object to add to the Netplay index</param>
    internal static void AddPatchToNetplayIndex(PatchInfo patch)
        => AddPatchToIndex(patch, _databaseStates[PatchDatabaseType.Netplay]);

    /// <summary>
    /// Adds a patch to the specified database state.
    /// </summary>
    /// <param name="patch">The PatchInfo object to add</param>
    /// <param name="state">The database state to add the patch to</param>
    private static void AddPatchToIndex(PatchInfo patch, PatchDatabaseState state)
    {
        if (patch.Name == null)
        {
            Logger.Warning<PatchesDatabase>("Attempted to add patch with null name to index");
            return;
        }

        string normalized = patch.Name.ToUpperInvariant();

        if (state.PatchNameMap.TryAdd(normalized, patch))
        {
            state.PatchNames.Add(normalized);
        }
    }

    /// <summary>
    /// Filters the Canary patches database based on the provided search query.
    /// If the search query is empty or whitespace, the full database is restored.
    /// The search is case-insensitive and matches both patch names and SHA values.
    /// </summary>
    /// <param name="searchQuery">The query string to search for</param>
    public static Task SearchCanaryDatabase(string searchQuery)
        => SearchDatabaseAsync(PatchDatabaseType.Canary, searchQuery);

    /// <summary>
    /// Filters the Netplay patches database based on the provided search query.
    /// If the search query is empty or whitespace, the full database is restored.
    /// The search is case-insensitive and matches both patch names and SHA values.
    /// </summary>
    /// <param name="searchQuery">The query string to search for</param>
    public static Task SearchNetplayDatabase(string searchQuery)
        => SearchDatabaseAsync(PatchDatabaseType.Netplay, searchQuery);

    /// <summary>
    /// Searches a patch database variant.
    /// </summary>
    /// <param name="type">The database variant to search</param>
    /// <param name="searchQuery">The query string to search for</param>
    private static Task SearchDatabaseAsync(PatchDatabaseType type, string searchQuery)
    {
        return Task.Run(() =>
        {
            PatchDatabaseState state = _databaseStates[type];
            string variantName = type.ToString();

            Logger.Debug<PatchesDatabase>($"Searching {variantName} database with query: '{searchQuery}'");

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                state.FilteredDatabase = state.PatchNameMap.Values.ToList();
                Logger.Debug<PatchesDatabase>($"Reset complete, showing all {state.FilteredDatabase.Count} {variantName} patches");
                return;
            }

            string upperQuery = searchQuery.ToUpperInvariant();

            state.FilteredDatabase = state.PatchNames
                .Where(name => name.Contains(upperQuery)
                               || state.PatchNameMap[name].Sha?.ToUpperInvariant().Contains(upperQuery) == true)
                .Select(name => state.PatchNameMap[name])
                .ToList();

            Logger.Debug<PatchesDatabase>($"Search completed, found {state.FilteredDatabase.Count} matching {variantName.ToLowerInvariant()} patches");
        });
    }

    /// <summary>
    /// Retrieves PatchInfo for a patch with the specified name from the Canary database.
    /// Performs a case-insensitive lookup using the internal index.
    /// </summary>
    /// <param name="patchName">The name of the patch to search for</param>
    /// <returns>The PatchInfo object if found, null otherwise</returns>
    public static PatchInfo? GetCanaryPatchInfo(string? patchName)
        => GetPatchInfo(patchName, _databaseStates[PatchDatabaseType.Canary]);

    /// <summary>
    /// Retrieves PatchInfo for a patch with the specified name from the Netplay database.
    /// Performs a case-insensitive lookup using the internal index.
    /// </summary>
    /// <param name="patchName">The name of the patch to search for</param>
    /// <returns>The PatchInfo object if found, null otherwise</returns>
    public static PatchInfo? GetNetplayPatchInfo(string? patchName)
        => GetPatchInfo(patchName, _databaseStates[PatchDatabaseType.Netplay]);

    /// <summary>
    /// Retrieves a patch from the specified database state by name.
    /// </summary>
    /// <param name="patchName">The name of the patch to search for</param>
    /// <param name="state">The database state to search</param>
    /// <returns>The PatchInfo object if found, null otherwise</returns>
    private static PatchInfo? GetPatchInfo(string? patchName, PatchDatabaseState state)
    {
        if (string.IsNullOrEmpty(patchName))
        {
            return null;
        }

        string normalized = patchName.ToUpperInvariant();
        return state.PatchNameMap.GetValueOrDefault(normalized);
    }

    /// <summary>
    /// Resets all static states. Intended for test isolation only.
    /// </summary>
    public static void Reset()
    {
        foreach (PatchDatabaseState state in _databaseStates.Values)
        {
            state.PatchNames.Clear();
            state.PatchNameMap.Clear();
            state.FilteredDatabase = [];
            state.IsLoaded = false;
        }
        Logger.Info<PatchesDatabase>("PatchesDatabase reset complete");
    }
}