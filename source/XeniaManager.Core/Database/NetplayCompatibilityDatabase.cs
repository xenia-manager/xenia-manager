using System.Text.Json;
using XeniaManager.Core.Constants;
using XeniaManager.Logging;
using XeniaManager.Core.Models.Database.NetplayCompatibility;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Database;

/// <summary>
/// Handles the loading, searching, and retrieval of netplay compatibility information.
/// Provides functionality to load the complete netplay compatibility database, search for games by title or ID,
/// and fetch netplay compatibility details for specific games.
/// Implements caching for API responses with 1-day expiration.
/// </summary>
public class NetplayCompatibilityDatabase
{
    /// <summary>
    /// Cache duration for API responses (1 day)
    /// </summary>
    private static readonly TimeSpan ApiCacheDuration = TimeSpan.FromDays(1);

    /// <summary>
    /// State for the netplay compatibility database
    /// </summary>
    private static readonly NetplayCompatibilityDatabaseState _databaseState = new NetplayCompatibilityDatabaseState();

    /// <summary>
    /// HttpClient used to fetch the database
    /// </summary>
    private static readonly HttpClientService _client = new HttpClientService();

    /// <summary>
    /// Fallback URLs for the Netplay Compatibility database
    /// </summary>
    private static readonly string[] _databaseUrls = Urls.NetplayCompatibilityDatabase;

    /// <summary>
    /// Gets the filtered games database (used for displaying games after search)
    /// </summary>
    public static List<NetplayCompatibilityEntry> FilteredDatabase
    {
        get => _databaseState.FilteredDatabase;
        private set => _databaseState.FilteredDatabase = value;
    }

    /// <summary>
    /// Loads the complete netplay compatibility database into memory.
    /// The database is only loaded once; following calls will be skipped if already loaded.
    /// Response is cached for 1 day to reduce API calls.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    public static async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_databaseState.IsLoaded)
        {
            Logger.Debug<NetplayCompatibilityDatabase>("Database already loaded, skipping load operation");
            return;
        }

        Logger.Info<NetplayCompatibilityDatabase>("Loading netplay compatibility database");

        string? response = null;

        foreach (string url in _databaseUrls)
        {
            try
            {
                response = await _client.GetAsync(url, cancellationToken, cacheKey: "netplay_compatibility_database", cacheDuration: ApiCacheDuration, cacheDirectory: AppPaths.DatabaseCacheDirectory);
                Logger.Info<NetplayCompatibilityDatabase>($"Successfully fetched from: {url}");
                break;
            }
            catch (Exception ex)
            {
                Logger.Warning<NetplayCompatibilityDatabase>($"Failed to fetch from '{url}'");
                Logger.LogExceptionDetails<NetplayCompatibilityDatabase>(ex);
            }
        }

        if (response == null)
        {
            Logger.Error<NetplayCompatibilityDatabase>($"All {_databaseUrls.Length} URLs failed to provide data");
            return;
        }

        Logger.Debug<NetplayCompatibilityDatabase>("Deserializing JSON data");

        List<NetplayCompatibilityEntry>? allEntries = JsonSerializer.Deserialize<List<NetplayCompatibilityEntry>>(response);

        if (allEntries is null || allEntries.Count == 0)
        {
            Logger.Warning<NetplayCompatibilityDatabase>("Database was empty or failed to deserialize.");
            return;
        }

        Logger.Debug<NetplayCompatibilityDatabase>($"Deserialized {allEntries.Count} games from database");

        int processedEntries = 0;
        foreach (NetplayCompatibilityEntry entry in allEntries)
        {
            foreach (string id in entry.Ids)
            {
                AddGameToIndex(entry, id);
            }

            processedEntries++;
            if (processedEntries % 1000 == 0)
            {
                Logger.Trace<NetplayCompatibilityDatabase>($"Processed {processedEntries}/{allEntries.Count} games");
            }
        }

        _databaseState.IsLoaded = true;
        _databaseState.FilteredDatabase = _databaseState.TitleIdGameMap.Values
            .DistinctBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Logger.Info<NetplayCompatibilityDatabase>($"Database loaded: {_databaseState.FilteredDatabase.Count} unique titles, {_databaseState.TitleIds.Count} title IDs");
    }

    /// <summary>
    /// Adds a game to the internal index using the specified title ID.
    /// </summary>
    public static void AddGameToIndex(NetplayCompatibilityEntry entry, string titleId)
    {
        if (entry.Title == null)
        {
            Logger.Warning<NetplayCompatibilityDatabase>("Attempted to add game with null title to index");
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
            Logger.Debug<NetplayCompatibilityDatabase>($"Searching database with query: '{searchQuery}'");

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                _databaseState.FilteredDatabase = _databaseState.TitleIdGameMap.Values
                    .DistinctBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                Logger.Debug<NetplayCompatibilityDatabase>($"Reset complete, showing all {_databaseState.FilteredDatabase.Count} titles");
                return;
            }

            string upperQuery = searchQuery.ToUpperInvariant();

            _databaseState.FilteredDatabase = _databaseState.TitleIds
                .Where(id => id.Contains(upperQuery) || _databaseState.TitleIdGameMap[id].Title!.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                .Select(id => _databaseState.TitleIdGameMap[id])
                .DistinctBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Logger.Debug<NetplayCompatibilityDatabase>($"Search completed, found {_databaseState.FilteredDatabase.Count} matching titles");
        });
    }

    /// <summary>
    /// Retrieves NetplayCompatibilityEntry for a game with the specified title.
    /// </summary>
    public static NetplayCompatibilityEntry? GetGameCompatibility(string? gameTitle)
    {
        Logger.Debug<NetplayCompatibilityDatabase>($"Searching for game with title: '{gameTitle}'");

        NetplayCompatibilityEntry? result = _databaseState.TitleIdGameMap.Values
            .FirstOrDefault(entry => string.Equals(entry.Title, gameTitle, StringComparison.OrdinalIgnoreCase));

        Logger.Debug<NetplayCompatibilityDatabase>(result != null
            ? $"Found game with title: '{gameTitle}'"
            : $"Game with title '{gameTitle}' not found in database");
        return result;
    }

    /// <summary>
    /// Retrieves NetplayCompatibilityEntry for a game with the specified title ID.
    /// </summary>
    public static NetplayCompatibilityEntry? GetGameCompatibilityById(string? titleId)
    {
        if (string.IsNullOrEmpty(titleId))
        {
            Logger.Debug<NetplayCompatibilityDatabase>("Title ID is null or empty");
            return null;
        }

        Logger.Debug<NetplayCompatibilityDatabase>($"Searching for game with title ID: '{titleId}'");

        string normalized = titleId.ToUpperInvariant();
        if (_databaseState.TitleIdGameMap.TryGetValue(normalized, out NetplayCompatibilityEntry? result))
        {
            Logger.Debug<NetplayCompatibilityDatabase>($"Found game with title ID: '{titleId}'");
            return result;
        }

        Logger.Debug<NetplayCompatibilityDatabase>($"Game with title ID '{titleId}' not found in database");
        return null;
    }

    /// <summary>
    /// Sets the netplay compatibility for a game by searching the netplay compatibility database.
    /// First searches using the primary game ID, then falls back to alternative IDs if needed.
    /// </summary>
    public static async Task SetNetplayCompatibility(Game game, CancellationToken cancellationToken = default)
    {
        Logger.Debug<NetplayCompatibilityDatabase>($"Setting netplay compatibility for game: '{game.Title}' (ID: {game.GameId})");

        await LoadAsync(cancellationToken);

        List<NetplayCompatibilityEntry> matches = [];

        if (!string.IsNullOrEmpty(game.GameId))
        {
            NetplayCompatibilityEntry? match = GetGameCompatibilityById(game.GameId);
            if (match != null)
            {
                matches.Add(match);
                Logger.Debug<NetplayCompatibilityDatabase>($"Found netplay entry by primary ID '{game.GameId}'");
            }
        }

        if (matches.Count == 0 && game.AlternativeIDs is { Count: > 0 })
        {
            Logger.Debug<NetplayCompatibilityDatabase>($"Primary ID not found, searching through {game.AlternativeIDs.Count} alternative IDs");
            foreach (string altId in game.AlternativeIDs)
            {
                NetplayCompatibilityEntry? match = GetGameCompatibilityById(altId);
                if (match == null)
                {
                    continue;
                }
                matches.Add(match);
                Logger.Debug<NetplayCompatibilityDatabase>($"Found netplay entry by alternative ID '{altId}'");
            }
        }

        NetplayCompatibilityEntry? resultEntry = null;

        switch (matches.Count)
        {
            case 0:
                Logger.Debug<NetplayCompatibilityDatabase>($"No netplay entry found for '{game.Title}', defaulting to empty");
                game.Compatibility.Netplay.Status = new NetplayStatus();
                game.Compatibility.Netplay.Comments = string.Empty;
                break;
            case 1:
                resultEntry = matches[0];
                Logger.Debug<NetplayCompatibilityDatabase>($"Single match found for '{game.Title}'");
                break;
            default:
            {
                Logger.Debug<NetplayCompatibilityDatabase>($"Multiple matches ({matches.Count}) found for '{game.Title}', filtering by title");

                resultEntry = matches.FirstOrDefault(m =>
                    string.Equals(m.Title, game.Title, StringComparison.OrdinalIgnoreCase));

                if (resultEntry != null)
                {
                    Logger.Debug<NetplayCompatibilityDatabase>($"Found title match for '{game.Title}'");
                }
                else
                {
                    resultEntry = matches[0];
                    Logger.Debug<NetplayCompatibilityDatabase>($"No title match found, using first entry for '{game.Title}'");
                }
                break;
            }
        }

        if (resultEntry != null)
        {
            game.Compatibility.Netplay.Status = resultEntry.Status;
            game.Compatibility.Netplay.Comments = resultEntry.Comments ?? string.Empty;
            Logger.Info<NetplayCompatibilityDatabase>($"Resolved netplay compatibility for '{game.Title}'");
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
        Logger.Info<NetplayCompatibilityDatabase>("NetplayCompatibilityDatabase reset complete");
    }

    /// <summary>
    /// Forces a reload of the netplay compatibility database by clearing the cache and fetching fresh data.
    /// </summary>
    public static async Task ForceReloadAsync(CancellationToken cancellationToken = default)
    {
        Logger.Info<NetplayCompatibilityDatabase>("Forcing reload of netplay compatibility database");

        string cacheFile = Path.Combine(AppPaths.DatabaseCacheDirectory, "netplay_compatibility_database.json");
        if (File.Exists(cacheFile))
        {
            Logger.Info<NetplayCompatibilityDatabase>($"Clearing cached database file: {cacheFile}");
            File.Delete(cacheFile);
        }

        Reset();
        await LoadAsync(cancellationToken);
    }
}
