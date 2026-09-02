using System.Text.Json;
using XeniaManager.Database.Models.MousehookCompatibility;
using XeniaManager.Database.Constants;
using XeniaManager.Database.Utilities;
using XeniaManager.Logging;

namespace XeniaManager.Database;

/// <summary>
/// Handles the loading, searching, and retrieval of mousehook compatibility information.
/// Provides functionality to load the complete mousehook compatibility database, search for games by title or ID,
/// and fetch mousehook compatibility details for specific games.
/// Implements caching for API responses with 1-day expiration.
/// </summary>
public class MousehookCompatibilityDatabase
{
    /// <summary>
    /// Cache duration for API responses (1 day)
    /// </summary>
    private static readonly TimeSpan ApiCacheDuration = TimeSpan.FromDays(1);

    /// <summary>
    /// State for the mousehook compatibility database
    /// </summary>
    private static readonly MousehookCompatibilityDatabaseState _databaseState = new MousehookCompatibilityDatabaseState();

    /// <summary>
    /// HttpClient used to fetch the database
    /// </summary>
    private static readonly DatabaseHttpClient _client = new DatabaseHttpClient();

    /// <summary>
    /// Fallback URLs for the Mousehook Compatibility database
    /// </summary>
    private static readonly string[] _databaseUrls = DatabaseUrls.MousehookCompatibilityDatabase;

    /// <summary>
    /// Gets the filtered games database (used for displaying games after search)
    /// </summary>
    public static List<MousehookCompatibilityEntry> FilteredDatabase
    {
        get
        {
            return _databaseState.FilteredDatabase;
        }
        private set
        {
            _databaseState.FilteredDatabase = value;
        }
    }

    /// <summary>
    /// Loads the complete mousehook compatibility database into memory.
    /// The database is only loaded once; following calls will be skipped if already loaded.
    /// Response is cached for 1 day to reduce API calls.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <param name="cacheDirectory">Optional cache directory override</param>
    public static async Task LoadAsync(CancellationToken cancellationToken = default, string? cacheDirectory = null)
    {
        if (_databaseState.IsLoaded)
        {
            Logger.Debug<MousehookCompatibilityDatabase>("Database already loaded, skipping load operation");
            return;
        }

        Logger.Info<MousehookCompatibilityDatabase>("Loading mousehook compatibility database");

        string? response = null;

        foreach (string url in _databaseUrls)
        {
            try
            {
                response = await _client.GetAsync(url, cancellationToken, "mousehook_compatibility_database", ApiCacheDuration, cacheDirectory);
                Logger.Info<MousehookCompatibilityDatabase>($"Successfully fetched from: {url}");
                break;
            }
            catch (Exception ex)
            {
                Logger.Warning<MousehookCompatibilityDatabase>($"Failed to fetch from '{url}'");
                Logger.LogExceptionDetails<MousehookCompatibilityDatabase>(ex);
            }
        }

        if (response == null)
        {
            Logger.Error<MousehookCompatibilityDatabase>($"All {_databaseUrls.Length} URLs failed to provide data");
            return;
        }

        Logger.Debug<MousehookCompatibilityDatabase>("Deserializing JSON data");

        List<MousehookCompatibilityEntry>? allEntries = JsonSerializer.Deserialize<List<MousehookCompatibilityEntry>>(response);

        if (allEntries is null || allEntries.Count == 0)
        {
            Logger.Warning<MousehookCompatibilityDatabase>("Database was empty or failed to deserialize.");
            return;
        }

        Logger.Debug<MousehookCompatibilityDatabase>($"Deserialized {allEntries.Count} games from database");

        int processedEntries = 0;
        foreach (MousehookCompatibilityEntry entry in allEntries)
        {
            foreach (string id in entry.Ids)
            {
                AddGameToIndex(entry, id);
            }

            processedEntries++;
            if (processedEntries % 1000 == 0)
            {
                Logger.Trace<MousehookCompatibilityDatabase>($"Processed {processedEntries}/{allEntries.Count} games");
            }
        }

        _databaseState.IsLoaded = true;
        _databaseState.FilteredDatabase = _databaseState.TitleIdGameMap.Values
            .DistinctBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Logger.Info<MousehookCompatibilityDatabase>(
            $"Database loaded: {_databaseState.FilteredDatabase.Count} unique titles, {_databaseState.TitleIds.Count} title IDs");
    }

    /// <summary>
    /// Adds a game to the internal index using the specified title ID.
    /// </summary>
    public static void AddGameToIndex(MousehookCompatibilityEntry entry, string titleId)
    {
        if (entry.Title == null)
        {
            Logger.Warning<MousehookCompatibilityDatabase>("Attempted to add game with null title to index");
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
    /// </summary>
    public static Task SearchDatabase(string searchQuery)
    {
        return Task.Run(() =>
        {
            Logger.Debug<MousehookCompatibilityDatabase>($"Searching database with query: '{searchQuery}'");

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                _databaseState.FilteredDatabase = _databaseState.TitleIdGameMap.Values
                    .DistinctBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                Logger.Debug<MousehookCompatibilityDatabase>($"Reset complete, showing all {_databaseState.FilteredDatabase.Count} titles");
                return;
            }

            string upperQuery = searchQuery.ToUpperInvariant();

            _databaseState.FilteredDatabase = _databaseState.TitleIds
                .Where(id => id.Contains(upperQuery) || _databaseState.TitleIdGameMap[id].Title!.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                .Select(id => _databaseState.TitleIdGameMap[id])
                .DistinctBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Logger.Debug<MousehookCompatibilityDatabase>($"Search completed, found {_databaseState.FilteredDatabase.Count} matching titles");
        });
    }

    /// <summary>
    /// Retrieves MousehookCompatibilityEntry for a game with the specified title.
    /// </summary>
    public static MousehookCompatibilityEntry? GetGameCompatibility(string? gameTitle)
    {
        Logger.Debug<MousehookCompatibilityDatabase>($"Searching for game with title: '{gameTitle}'");

        MousehookCompatibilityEntry? result = _databaseState.TitleIdGameMap.Values
            .FirstOrDefault(entry => string.Equals(entry.Title, gameTitle, StringComparison.OrdinalIgnoreCase));

        Logger.Debug<MousehookCompatibilityDatabase>(result != null
            ? $"Found game with title: '{gameTitle}'"
            : $"Game with title '{gameTitle}' not found in database");
        return result;
    }

    /// <summary>
    /// Retrieves MousehookCompatibilityEntry for a game with the specified title ID.
    /// </summary>
    public static MousehookCompatibilityEntry? GetGameCompatibilityById(string? titleId)
    {
        if (string.IsNullOrEmpty(titleId))
        {
            Logger.Debug<MousehookCompatibilityDatabase>("Title ID is null or empty");
            return null;
        }

        Logger.Debug<MousehookCompatibilityDatabase>($"Searching for game with title ID: '{titleId}'");

        string normalized = titleId.ToUpperInvariant();
        if (_databaseState.TitleIdGameMap.TryGetValue(normalized, out MousehookCompatibilityEntry? result))
        {
            Logger.Debug<MousehookCompatibilityDatabase>($"Found game with title ID: '{titleId}'");
            return result;
        }

        Logger.Debug<MousehookCompatibilityDatabase>($"Game with title ID '{titleId}' not found in database");
        return null;
    }

    /// <summary>
    /// Resolves mousehook compatibility entry for given ids/title. Returns null if not found.
    /// First searches using the primary game ID, then falls back to alternative IDs if needed.
    /// </summary>
    /// <param name="gameId">The primary game ID to search for</param>
    /// <param name="alternativeIds">Alternative IDs to search if primary not found</param>
    /// <param name="title">The title of the game to search for</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <param name="cacheDirectory">Optional cache directory override</param>
    /// <returns>The matching MousehookCompatibilityEntry if found, null otherwise</returns>
    public static async Task<MousehookCompatibilityEntry?> ResolveAsync(string? gameId, IReadOnlyList<string>? alternativeIds, string? title,
        CancellationToken cancellationToken = default, string? cacheDirectory = null)
    {
        Logger.Debug<MousehookCompatibilityDatabase>($"Resolving mousehook for '{title}' (ID: {gameId})");
        await LoadAsync(cancellationToken, cacheDirectory);

        List<MousehookCompatibilityEntry> matches = [];

        if (!string.IsNullOrEmpty(gameId))
        {
            MousehookCompatibilityEntry? match = GetGameCompatibilityById(gameId);
            if (match != null)
            {
                matches.Add(match);
                Logger.Debug<MousehookCompatibilityDatabase>($"Found mousehook entry by primary ID '{gameId}': {match.MouseSupport}");
            }
        }

        if (matches.Count == 0 && alternativeIds is { Count: > 0 })
        {
            Logger.Debug<MousehookCompatibilityDatabase>($"Primary ID not found, searching through {alternativeIds.Count} alternative IDs");
            foreach (string altId in alternativeIds)
            {
                MousehookCompatibilityEntry? match = GetGameCompatibilityById(altId);
                if (match == null)
                {
                    continue;
                }

                matches.Add(match);
                Logger.Debug<MousehookCompatibilityDatabase>($"Found mousehook entry by alternative ID '{altId}': {match.MouseSupport}");
            }
        }

        switch (matches.Count)
        {
            case 0:
                Logger.Debug<MousehookCompatibilityDatabase>($"No mousehook entry found for '{title}'");
                return null;
            case 1:
                Logger.Debug<MousehookCompatibilityDatabase>($"Single match found for '{title}': {matches[0].MouseSupport}");
                return matches[0];
            default:
                Logger.Debug<MousehookCompatibilityDatabase>($"Multiple matches ({matches.Count}) found for '{title}', filtering by title");
                MousehookCompatibilityEntry? resultEntry = matches.FirstOrDefault(m => string.Equals(m.Title, title, StringComparison.OrdinalIgnoreCase));
                if (resultEntry != null)
                {
                    Logger.Debug<MousehookCompatibilityDatabase>($"Found title match for '{title}': {resultEntry.MouseSupport}");
                    return resultEntry;
                }

                resultEntry = matches[0];
                Logger.Debug<MousehookCompatibilityDatabase>($"No title match found, using first entry for '{title}': {resultEntry.MouseSupport}");
                return resultEntry;
        }
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
        Logger.Info<MousehookCompatibilityDatabase>("MousehookCompatibilityDatabase reset complete");
    }

    /// <summary>
    /// Forces a reload of the mousehook compatibility database by clearing the cache and fetching fresh data.
    /// </summary>
    public static async Task ForceReloadAsync(CancellationToken cancellationToken = default, string? cacheDirectory = null)
    {
        Logger.Info<MousehookCompatibilityDatabase>("Forcing reload of mousehook compatibility database");
        if (!string.IsNullOrEmpty(cacheDirectory))
        {
            string cacheFile = Path.Combine(cacheDirectory, "mousehook_compatibility_database.json");
            if (File.Exists(cacheFile))
            {
                File.Delete(cacheFile);
            }
        }
        else
        {
            string defaultCache = Path.Combine(AppContext.BaseDirectory, "Cache", "Database", "mousehook_compatibility_database.json");
            if (File.Exists(defaultCache))
            {
                File.Delete(defaultCache);
            }
        }

        Reset();
        await LoadAsync(cancellationToken, cacheDirectory);
    }
}