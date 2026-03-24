using System.Text.Json;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Database.OptimizedSettings;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Database;

/// <summary>
/// Handles the loading, searching, and retrieval of optimized settings.
/// Provides functionality to load the complete optimized settings database, search for games by title or ID,
/// and fetch optimized settings as ConfigFile for specific games.
/// Implements caching for API responses with 1-day expiration.
/// </summary>
public class OptimizedSettingsDatabase
{
    /// <summary>
    /// Cache duration for API responses (1 day)
    /// </summary>
    private static readonly TimeSpan ApiCacheDuration = TimeSpan.FromDays(1);

    /// <summary>
    /// State for the optimized settings database
    /// </summary>
    private static readonly OptimizedSettingsDatabaseState _databaseState = new OptimizedSettingsDatabaseState();

    /// <summary>
    /// HttpClient used to fetch the database
    /// Reuses the same client instance for efficiency and connection pooling
    /// </summary>
    private static readonly HttpClientService _client = new HttpClientService();

    /// <summary>
    /// Fallback URLs for the Optimized Settings database
    /// If the primary URL fails, the system will try secondary URLs in sequence
    /// </summary>
    private static readonly string[] _databaseUrls = Urls.OptimizedSettingsDatabase;

    /// <summary>
    /// Gets the filtered optimized settings database (used for displaying entries after search)
    /// This list holds the OptimizedSettingsEntry objects that match the current search query
    /// </summary>
    public static List<OptimizedSettingsEntry> FilteredDatabase
    {
        get => _databaseState.FilteredDatabase;
        private set => _databaseState.FilteredDatabase = value;
    }

    /// <summary>
    /// Loads the complete optimized settings database into memory.
    /// This method populates internal collections for fast game lookups and initializes the search functionality.
    /// The database is only loaded once; following calls will be skipped if already loaded.
    /// Response is cached for 1 day to reduce API calls.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <exception cref="AggregateException">Thrown when all database URLs fail to provide data</exception>
    public static async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_databaseState.IsLoaded)
        {
            Logger.Debug<OptimizedSettingsDatabase>("Database already loaded, skipping load operation");
            return;
        }

        Logger.Info<OptimizedSettingsDatabase>("Loading optimized settings database");

        string? response = null;

        foreach (string url in _databaseUrls)
        {
            try
            {
                response = await _client.GetAsync(url, cancellationToken, cacheKey: "optimized_settings_database", cacheDuration: ApiCacheDuration, cacheDirectory: AppPaths.DatabaseCacheDirectory);
                Logger.Info<OptimizedSettingsDatabase>($"Successfully fetched from: {url}");
                break;
            }
            catch (Exception ex)
            {
                Logger.Warning<OptimizedSettingsDatabase>($"Failed to fetch from '{url}'");
                Logger.LogExceptionDetails<OptimizedSettingsDatabase>(ex);
            }
        }

        if (response == null)
        {
            Logger.Error<OptimizedSettingsDatabase>($"All {_databaseUrls.Length} URLs failed to provide data");
            return;
        }

        Logger.Debug<OptimizedSettingsDatabase>("Deserializing JSON data");

        List<OptimizedSettingsEntry>? allEntries = JsonSerializer.Deserialize<List<OptimizedSettingsEntry>>(response);

        if (allEntries is null || allEntries.Count == 0)
        {
            Logger.Warning<OptimizedSettingsDatabase>("Database was empty or failed to deserialize.");
            return;
        }

        Logger.Debug<OptimizedSettingsDatabase>($"Deserialized {allEntries.Count} entries from database");

        int processedEntries = 0;
        foreach (OptimizedSettingsEntry entry in allEntries)
        {
            if (entry.Id != null)
            {
                AddEntryToIndex(entry, entry.Id);
            }

            processedEntries++;
            if (processedEntries % 1000 == 0)
            {
                Logger.Trace<OptimizedSettingsDatabase>($"Processed {processedEntries}/{allEntries.Count} entries");
            }
        }

        _databaseState.IsLoaded = true;
        _databaseState.FilteredDatabase = _databaseState.TitleIdGameMap.Values
            .DistinctBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Logger.Info<OptimizedSettingsDatabase>($"Database loaded: {_databaseState.FilteredDatabase.Count} unique titles, {_databaseState.TitleIds.Count} title IDs");
    }

    /// <summary>
    /// Adds an entry to the internal index using the specified title ID.
    /// This enables fast lookups of optimized settings by title ID.
    /// The title ID is normalized to uppercase for consistent comparisons.
    /// </summary>
    /// <param name="entry">The OptimizedSettingsEntry object to add to the index</param>
    /// <param name="titleId">The title ID to use as the key for indexing the entry</param>
    public static void AddEntryToIndex(OptimizedSettingsEntry entry, string titleId)
    {
        if (entry.Title == null)
        {
            Logger.Warning<OptimizedSettingsDatabase>("Attempted to add entry with null title to index");
            return;
        }

        string normalized = titleId.ToUpperInvariant();

        if (_databaseState.TitleIdGameMap.TryAdd(normalized, entry))
        {
            _databaseState.TitleIds.Add(normalized);
        }
    }

    /// <summary>
    /// Filters the database based on the provided search query.
    /// This method updates the FilteredDatabase property with OptimizedSettingsEntry objects that match the search query.
    /// If the search query is empty or whitespace, the full database is restored.
    /// The search is case-insensitive and matches both title IDs and game titles.
    /// </summary>
    /// <param name="searchQuery">The query string to search for in game titles and IDs</param>
    public static Task SearchDatabase(string searchQuery)
    {
        return Task.Run(() =>
        {
            Logger.Debug<OptimizedSettingsDatabase>($"Searching database with query: '{searchQuery}'");

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                _databaseState.FilteredDatabase = _databaseState.TitleIdGameMap.Values
                    .DistinctBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                Logger.Debug<OptimizedSettingsDatabase>($"Reset complete, showing all {_databaseState.FilteredDatabase.Count} titles");
                return;
            }

            string upperQuery = searchQuery.ToUpperInvariant();

            _databaseState.FilteredDatabase = _databaseState.TitleIds
                .Where(id => id.Contains(upperQuery) || _databaseState.TitleIdGameMap[id].Title!.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                .Select(id => _databaseState.TitleIdGameMap[id])
                .DistinctBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Logger.Debug<OptimizedSettingsDatabase>($"Search completed, found {_databaseState.FilteredDatabase.Count} matching titles");
        });
    }

    /// <summary>
    /// Retrieves OptimizedSettingsEntry for a game with the specified title.
    /// Performs a case-insensitive search through all indexed entries to find a match by title.
    /// </summary>
    /// <param name="gameTitle">The title of the game to search for</param>
    /// <returns>The OptimizedSettingsEntry object if found, null otherwise</returns>
    public static OptimizedSettingsEntry? GetEntryByTitle(string? gameTitle)
    {
        Logger.Debug<OptimizedSettingsDatabase>($"Searching for entry with title: '{gameTitle}'");

        OptimizedSettingsEntry? result = _databaseState.TitleIdGameMap.Values
            .FirstOrDefault(entry => string.Equals(entry.Title, gameTitle, StringComparison.OrdinalIgnoreCase));

        Logger.Debug<OptimizedSettingsDatabase>(result != null
            ? $"Found entry with title: '{gameTitle}'"
            : $"Entry with title '{gameTitle}' not found in database");
        return result;
    }

    /// <summary>
    /// Retrieves OptimizedSettingsEntry for a game with the specified title ID.
    /// Performs a direct lookup using the internal index.
    /// </summary>
    /// <param name="titleId">The title ID of the game to search for</param>
    /// <returns>The OptimizedSettingsEntry object if found, null otherwise</returns>
    public static OptimizedSettingsEntry? GetEntryById(string? titleId)
    {
        if (string.IsNullOrEmpty(titleId))
        {
            Logger.Debug<OptimizedSettingsDatabase>("Title ID is null or empty");
            return null;
        }

        Logger.Debug<OptimizedSettingsDatabase>($"Searching for entry with title ID: '{titleId}'");

        string normalized = titleId.ToUpperInvariant();
        if (_databaseState.TitleIdGameMap.TryGetValue(normalized, out OptimizedSettingsEntry? result))
        {
            Logger.Debug<OptimizedSettingsDatabase>($"Found entry with title ID: '{titleId}'");
            return result;
        }

        Logger.Debug<OptimizedSettingsDatabase>($"Entry with title ID '{titleId}' not found in database");
        return null;
    }

    /// <summary>
    /// Gets the optimized settings for a game by searching the optimized settings database.
    /// First searches using the primary game TitleId, then falls back to alternative IDs if needed.
    /// If a match is found, downloads the TOML file and loads it as a ConfigFile.
    /// If no match is found, returns null.
    /// </summary>
    /// <param name="game">The game object to search for</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <returns>The ConfigFile with optimized settings if found, null otherwise</returns>
    public static async Task<ConfigFile?> GetOptimizedSettings(Game game, CancellationToken cancellationToken = default)
    {
        Logger.Debug<OptimizedSettingsDatabase>($"Searching for optimized settings for game: '{game.Title}' (ID: {game.GameId})");

        await LoadAsync(cancellationToken);

        OptimizedSettingsEntry? foundEntry = null;
        string? foundId = null;

        // First, try to find using primary TitleId
        if (!string.IsNullOrEmpty(game.GameId))
        {
            foundEntry = GetEntryById(game.GameId);
            if (foundEntry != null)
            {
                foundId = game.GameId;
                Logger.Debug<OptimizedSettingsDatabase>($"Found optimized settings entry by primary ID '{game.GameId}': {foundEntry.Title}");
            }
        }

        // If not found, try alternative IDs
        if (foundEntry == null && game.AlternativeIDs is { Count: > 0 })
        {
            Logger.Debug<OptimizedSettingsDatabase>($"Primary ID not found, searching through {game.AlternativeIDs.Count} alternative IDs");
            foreach (string altId in game.AlternativeIDs)
            {
                OptimizedSettingsEntry? match = GetEntryById(altId);
                if (match == null)
                {
                    continue;
                }
                foundEntry = match;
                foundId = altId;
                Logger.Debug<OptimizedSettingsDatabase>($"Found optimized settings entry by alternative ID '{altId}': {match.Title}");
                break;
            }
        }

        if (foundEntry == null || string.IsNullOrEmpty(foundId))
        {
            Logger.Debug<OptimizedSettingsDatabase>($"No optimized settings entry found for '{game.Title}'");
            return null;
        }

        // Construct URLs: BaseOptimizedSettingsUrl + found ID + .toml
        string[] baseUrls = Urls.BaseOptimizedSettingsUrl;
        Logger.Info<OptimizedSettingsDatabase>($"Resolved {baseUrls.Length} potential URLs for '{game.Title}' using ID '{foundId}'");

        // Try each URL in sequence until one succeeds
        foreach (string baseUrl in baseUrls)
        {
            string url = baseUrl + foundId + ".toml";
            Logger.Debug<OptimizedSettingsDatabase>($"Attempting to download optimized settings from: {url}");

            try
            {
                string tomlContent = await _client.GetAsync(url, cancellationToken, cacheKey: $"optimized_settings_{foundId}", cacheDuration: ApiCacheDuration, cacheDirectory: AppPaths.DatabaseCacheDirectory);
                Logger.Info<OptimizedSettingsDatabase>($"Successfully downloaded optimized settings TOML for '{game.Title}' from: {url}");

                ConfigFile configFile = ConfigFile.FromString(tomlContent);
                Logger.Info<OptimizedSettingsDatabase>($"Successfully loaded optimized settings as ConfigFile for '{game.Title}'");
                return configFile;
            }
            catch (Exception ex)
            {
                Logger.Warning<OptimizedSettingsDatabase>($"Failed to download or parse optimized settings from '{url}': {ex.Message}");
                Logger.LogExceptionDetails<OptimizedSettingsDatabase>(ex);
            }
        }

        Logger.Error<OptimizedSettingsDatabase>($"All {baseUrls.Length} URLs failed to provide optimized settings for '{game.Title}'");
        return null;
    }

    /// <summary>
    /// Resets all static states. Intended for test isolation only.
    /// </summary>
    public static void Reset()
    {
        _databaseState.TitleIds.Clear();
        _databaseState.TitleIdGameMap.Clear();
        _databaseState.FilteredDatabase = [];
        _databaseState.IsLoaded = false;
        Logger.Info<OptimizedSettingsDatabase>("OptimizedSettingsDatabase reset complete");
    }

    /// <summary>
    /// Forces a reload of the optimized settings database by clearing the cache and fetching fresh data.
    /// This bypasses the cached state and reloads from the API.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    public static async Task ForceReloadAsync(CancellationToken cancellationToken = default)
    {
        Logger.Info<OptimizedSettingsDatabase>("Forcing reload of optimized settings database");

        // Clear the cached database file
        string cacheFile = Path.Combine(AppPaths.DatabaseCacheDirectory, "optimized_settings_database.json");
        if (File.Exists(cacheFile))
        {
            Logger.Info<OptimizedSettingsDatabase>($"Clearing cached database file: {cacheFile}");
            File.Delete(cacheFile);
        }

        // Reset the in-memory state
        Reset();

        // Reload fresh data from API
        await LoadAsync(cancellationToken);
    }
}