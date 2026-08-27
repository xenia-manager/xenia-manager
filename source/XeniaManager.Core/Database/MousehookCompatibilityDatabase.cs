using System.Text.Json;
using XeniaManager.Core.Constants;
using XeniaManager.Logging;
using XeniaManager.Core.Models.Database.MousehookCompatibility;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Database;

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
    private static readonly HttpClientService _client = new HttpClientService();

    /// <summary>
    /// Fallback URLs for the Mousehook Compatibility database
    /// </summary>
    private static readonly string[] _databaseUrls = Urls.MousehookCompatibilityDatabase;

    /// <summary>
    /// Gets the filtered games database (used for displaying games after search)
    /// </summary>
    public static List<MousehookCompatibilityEntry> FilteredDatabase
    {
        get => _databaseState.FilteredDatabase;
        private set => _databaseState.FilteredDatabase = value;
    }

    /// <summary>
    /// Loads the complete mousehook compatibility database into memory.
    /// The database is only loaded once; following calls will be skipped if already loaded.
    /// Response is cached for 1 day to reduce API calls.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    public static async Task LoadAsync(CancellationToken cancellationToken = default)
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
                response = await _client.GetAsync(url, cancellationToken, cacheKey: "mousehook_compatibility_database", cacheDuration: ApiCacheDuration, cacheDirectory: AppPaths.DatabaseCacheDirectory);
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

        Logger.Info<MousehookCompatibilityDatabase>($"Database loaded: {_databaseState.FilteredDatabase.Count} unique titles, {_databaseState.TitleIds.Count} title IDs");
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
    /// Sets the mousehook compatibility for a game by searching the mousehook compatibility database.
    /// First searches using the primary game ID, then falls back to alternative IDs if needed.
    /// </summary>
    public static async Task SetMousehookCompatibility(Game game, CancellationToken cancellationToken = default)
    {
        Logger.Debug<MousehookCompatibilityDatabase>($"Setting mousehook compatibility for game: '{game.Title}' (ID: {game.GameId})");

        await LoadAsync(cancellationToken);

        List<MousehookCompatibilityEntry> matches = [];

        if (!string.IsNullOrEmpty(game.GameId))
        {
            MousehookCompatibilityEntry? match = GetGameCompatibilityById(game.GameId);
            if (match != null)
            {
                matches.Add(match);
                Logger.Debug<MousehookCompatibilityDatabase>($"Found mousehook entry by primary ID '{game.GameId}': {match.MouseSupport}");
            }
        }

        if (matches.Count == 0 && game.AlternativeIDs is { Count: > 0 })
        {
            Logger.Debug<MousehookCompatibilityDatabase>($"Primary ID not found, searching through {game.AlternativeIDs.Count} alternative IDs");
            foreach (string altId in game.AlternativeIDs)
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

        MousehookCompatibilityEntry? resultEntry = null;

        switch (matches.Count)
        {
            case 0:
                Logger.Debug<MousehookCompatibilityDatabase>($"No mousehook entry found for '{game.Title}', defaulting to Unknown");
                game.Compatibility.Mousehook.Rating = MousehookSupportRating.Unknown;
                game.Compatibility.Mousehook.Notes = string.Empty;
                break;
            case 1:
                resultEntry = matches[0];
                Logger.Debug<MousehookCompatibilityDatabase>($"Single match found for '{game.Title}': {resultEntry.MouseSupport}");
                break;
            default:
            {
                Logger.Debug<MousehookCompatibilityDatabase>($"Multiple matches ({matches.Count}) found for '{game.Title}', filtering by title");

                resultEntry = matches.FirstOrDefault(m =>
                    string.Equals(m.Title, game.Title, StringComparison.OrdinalIgnoreCase));

                if (resultEntry != null)
                {
                    Logger.Debug<MousehookCompatibilityDatabase>($"Found title match for '{game.Title}': {resultEntry.MouseSupport}");
                }
                else
                {
                    resultEntry = matches[0];
                    Logger.Debug<MousehookCompatibilityDatabase>($"No title match found, using first entry for '{game.Title}': {resultEntry.MouseSupport}");
                }
                break;
            }
        }

        if (resultEntry != null)
        {
            game.Compatibility.Mousehook.Rating = resultEntry.MouseSupport;
            game.Compatibility.Mousehook.Notes = resultEntry.Notes ?? string.Empty;
            Logger.Info<MousehookCompatibilityDatabase>($"Resolved mousehook compatibility for '{game.Title}': {resultEntry.MouseSupport}");
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
    public static async Task ForceReloadAsync(CancellationToken cancellationToken = default)
    {
        Logger.Info<MousehookCompatibilityDatabase>("Forcing reload of mousehook compatibility database");

        string cacheFile = Path.Combine(AppPaths.DatabaseCacheDirectory, "mousehook_compatibility_database.json");
        if (File.Exists(cacheFile))
        {
            Logger.Info<MousehookCompatibilityDatabase>($"Clearing cached database file: {cacheFile}");
            File.Delete(cacheFile);
        }

        Reset();
        await LoadAsync(cancellationToken);
    }
}
