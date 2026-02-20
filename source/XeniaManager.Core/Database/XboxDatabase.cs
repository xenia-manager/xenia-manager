using System.Text.Json;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Database.Xbox;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Database;

/// <summary>
/// Handles the loading, searching, and retrieval of Xbox game information from the marketplace database.
/// Provides functionality to load the complete game database, search for games by title or ID,
/// and fetch detailed information for specific games.
/// Implements caching for API responses with 1-day expiration.
/// </summary>
public class XboxDatabase
{
    /// <summary>
    /// Cache duration for API responses (1 day)
    /// </summary>
    private static readonly TimeSpan ApiCacheDuration = TimeSpan.FromDays(1);

    /// <summary>
    /// State for the Xbox marketplace database
    /// </summary>
    private static readonly XboxDatabaseState _databaseState = new XboxDatabaseState();

    /// <summary>
    /// HttpClient used to grab the database
    /// Reuses the same client instance for efficiency and connection pooling
    /// </summary>
    private static readonly HttpClientService _client = new HttpClientService();

    /// <summary>
    /// Fallback URLs for the Xbox Marketplace database
    /// If the primary URL fails, the system will try secondary URLs in sequence
    /// </summary>
    private static readonly string[] _databaseUrls = Urls.XboxMarketplaceDatabase;

    /// <summary>
    /// Fallback URLs for individual game info (format strings with {0} for title_id)
    /// These URLs are formatted with the specific title_id to fetch individual game details
    /// If the primary URL fails, the system will try secondary URLs in sequence
    /// </summary>
    private static readonly string[] _gameInfoUrls = Urls.XboxMarketplaceDatabaseGameInfo;

    /// <summary>
    /// Gets the filtered games database (used for displaying games after search)
    /// This list holds the GameInfo objects of games that match the current search query
    /// </summary>
    public static List<GameInfo> FilteredDatabase
    {
        get => _databaseState.FilteredDatabase;
        private set => _databaseState.FilteredDatabase = value;
    }

    /// <summary>
    /// Loads the complete Xbox games database from the marketplace into memory.
    /// This method populates internal collections for fast game lookups and initializes the search functionality.
    /// The database is only loaded once; further calls will be skipped if already loaded.
    /// Response is cached for 1 day to reduce API calls.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <exception cref="AggregateException">Thrown when all database URLs fail to provide data</exception>
    public static async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_databaseState.IsLoaded)
        {
            Logger.Debug<XboxDatabase>("Database already loaded, skipping load operation");
            return;
        }

        Logger.Info<XboxDatabase>("Loading Xbox games database");

        // Try each URL in sequence with caching
        string? response = null;
        foreach (string url in _databaseUrls)
        {
            try
            {
                response = await _client.GetAsync(url, cancellationToken, cacheKey: "xbox_database", cacheDuration: ApiCacheDuration, cacheDirectory: AppPaths.X360DataBaseCacheDirectory);
                Logger.Info<XboxDatabase>($"Successfully fetched from: {url}");
                break;
            }
            catch (Exception ex)
            {
                Logger.Warning<XboxDatabase>($"Failed to fetch from '{url}'");
                Logger.LogExceptionDetails<XboxDatabase>(ex);
            }
        }

        if (response == null)
        {
            Logger.Error<XboxDatabase>($"All {_databaseUrls.Length} URLs failed to provide data");
            return;
        }

        Logger.Debug<XboxDatabase>("Response received (from cache or fresh), deserializing JSON data");

        List<GameInfo>? allGames = JsonSerializer.Deserialize<List<GameInfo>>(response);

        if (allGames is null || allGames.Count == 0)
        {
            Logger.Warning<XboxDatabase>("Database was empty or failed to deserialize.");
            return;
        }

        Logger.Debug<XboxDatabase>($"Deserialized {allGames.Count} games from database");

        int processedGames = 0;
        foreach (GameInfo game in allGames)
        {
            if (game.Id != null)
            {
                AddGameToIndex(game, game.Id);
            }

            if (game.AlternativeId is { Count: > 0 })
            {
                foreach (string altId in game.AlternativeId)
                {
                    AddGameToIndex(game, altId);
                }
            }

            processedGames++;
            if (processedGames % 1000 == 0)
            {
                Logger.Trace<XboxDatabase>($"Processed {processedGames}/{allGames.Count} games");
            }
        }

        _databaseState.IsLoaded = true;
        _databaseState.FilteredDatabase = _databaseState.TitleIdGameMap.Values
            .DistinctBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Logger.Info<XboxDatabase>($"Database loaded: {_databaseState.FilteredDatabase.Count} unique titles, {_databaseState.TitleIds.Count} title IDs");
    }

    /// <summary>
    /// Adds a game to the internal index using the specified title ID.
    /// This enables fast lookups of game information by title ID.
    /// The title ID is normalized to uppercase for consistent comparisons.
    /// </summary>
    /// <param name="game">The GameInfo object to add to the index</param>
    /// <param name="titleId">The title ID to use as the key for indexing the game</param>
    public static void AddGameToIndex(GameInfo game, string titleId)
    {
        string normalized = titleId.ToUpperInvariant();
        Logger.Trace<XboxDatabase>($"Adding game '{game.Title}' with normalized ID '{normalized}' to index");

        if (_databaseState.TitleIdGameMap.TryAdd(normalized, game))
        {
            _databaseState.TitleIds.Add(normalized);
            Logger.Trace<XboxDatabase>($"Successfully added game with ID '{normalized}' to index");
        }
        else
        {
            Logger.Trace<XboxDatabase>($"Game with ID '{normalized}' already exists in index, skipping duplicate");
        }
    }

    /// <summary>
    /// Filters the database based on the provided search query.
    /// This method updates the FilteredDatabase property with GameInfo objects that match the search query.
    /// If the search query is empty or whitespace, the full database is restored.
    /// The search is case-insensitive and matches both title IDs and game titles.
    /// </summary>
    /// <param name="searchQuery">The query string to search for in game titles and IDs</param>
    public static Task SearchDatabase(string searchQuery)
    {
        return Task.Run(() =>
        {
            Logger.Debug<XboxDatabase>($"Searching database with query: '{searchQuery}'");

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                Logger.Debug<XboxDatabase>("Resetting to full list due to empty search query");
                _databaseState.FilteredDatabase = _databaseState.TitleIdGameMap.Values
                    .DistinctBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                Logger.Debug<XboxDatabase>($"Reset complete, showing all {_databaseState.FilteredDatabase.Count} titles");
                return;
            }

            string upperQuery = searchQuery.ToUpperInvariant();

            _databaseState.FilteredDatabase = _databaseState.TitleIds
                .Where(id => id.Contains(upperQuery)
                             || _databaseState.TitleIdGameMap[id].Title!.Contains(searchQuery,
                                 StringComparison.OrdinalIgnoreCase))
                .Select(id => _databaseState.TitleIdGameMap[id])
                .DistinctBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Logger.Debug<XboxDatabase>($"Search completed, found {_databaseState.FilteredDatabase.Count} matching titles");
        });
    }

    /// <summary>
    /// Retrieves GameInfo for a game with the specified title.
    /// Performs a case-insensitive search through all indexed games to find a match by title.
    /// </summary>
    /// <param name="gameTitle">The title of the game to search for</param>
    /// <returns>The GameInfo object if found, null otherwise</returns>
    public static GameInfo? GetShortGameInfo(string? gameTitle)
    {
        Logger.Debug<XboxDatabase>($"Searching for game with title: '{gameTitle}'");

        GameInfo? result = _databaseState.TitleIdGameMap.Values
            .FirstOrDefault(game =>
                string.Equals(game.Title, gameTitle, StringComparison.OrdinalIgnoreCase));

        Logger.Debug<XboxDatabase>(result != null
            ? $"Found game with title: '{gameTitle}'"
            : $"Game with title '{gameTitle}' not found in database");
        return result;
    }

    /// <summary>
    /// Fetches detailed game information for the specified title ID using fallback URLs.
    /// This method retrieves comprehensive game details from the online database.
    /// Implements a fallback mechanism to try multiple URLs if the primary one fails.
    /// Response is cached for 1 day to reduce API calls (cached as {titleid}.json).
    /// </summary>
    /// <param name="titleId">The title ID of the game to fetch detailed information for</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <returns>The detailed game information if found and successfully retrieved, null otherwise</returns>
    public static async Task<GameDetailedInfo?> GetFullGameInfo(string titleId, CancellationToken cancellationToken = default)
    {
        Logger.Debug<XboxDatabase>($"Initiating request for full game info with title ID: '{titleId}'");

        string cacheKey = titleId.ToUpperInvariant();
        string? response = null;

        // Try each URL in sequence with caching
        foreach (string urlFormat in _gameInfoUrls)
        {
            string url = string.Format(urlFormat, titleId);
            try
            {
                response = await _client.GetAsync(url, cancellationToken, cacheKey: cacheKey, cacheDuration: ApiCacheDuration, cacheDirectory: AppPaths.X360DataBaseCacheDirectory);
                Logger.Info<XboxDatabase>($"Successfully fetched from: {url}");
                break;
            }
            catch (Exception ex)
            {
                Logger.Warning<XboxDatabase>($"Failed to fetch from '{url}': {ex.Message}");
            }
        }

        if (response == null)
        {
            Logger.Error<XboxDatabase>($"All {_gameInfoUrls.Length} URLs failed for title ID '{titleId}'");
            return null;
        }

        GameDetailedInfo? result = JsonSerializer.Deserialize<GameDetailedInfo>(response);

        if (result != null)
        {
            Logger.Debug<XboxDatabase>($"Successfully retrieved and deserialized game info for '{titleId}'");
        }
        else
        {
            Logger.Warning<XboxDatabase>($"Deserialization returned null for game info with title ID '{titleId}'");
        }

        return result;
    }

    /// <summary>
    /// Resets all static states and clears HTTP cache. Intended for test isolation only.
    /// This clears all cached data and resets the loaded state to allow for clean testing.
    /// </summary>
    public static void Reset()
    {
        _databaseState.TitleIds.Clear();
        _databaseState.TitleIdGameMap.Clear();
        _databaseState.FilteredDatabase = [];
        _databaseState.IsLoaded = false;
        Logger.Info<XboxDatabase>("XboxDatabase reset complete");
    }
}