using System.Text.Json;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Database;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Database;

/// <summary>
/// Handles the loading, searching, and retrieval of game compatibility information.
/// Provides functionality to load the complete compatibility database, search for games by title or ID,
/// and fetch compatibility details for specific games.
/// Implements caching for API responses with 1-day expiration.
/// </summary>
public class GameCompatibilityDatabase
{
    /// <summary>
    /// Cache duration for API responses (1 day)
    /// </summary>
    private static readonly TimeSpan ApiCacheDuration = TimeSpan.FromDays(1);

    /// <summary>
    /// Contains the filtered games database (used for displaying games after search)
    /// This list holds the GameCompatibilityEntry objects of games that match the current search query
    /// </summary>
    public static List<GameCompatibilityEntry> FilteredDatabase { get; private set; } = [];

    /// <summary>
    /// Contains every title_id for a specific game
    /// This is used for quick lookups when searching the database
    /// </summary>
    private static readonly HashSet<string> _allTitleIds = [];

    /// <summary>
    /// Mapping of title_id to GameCompatibilityEntry
    /// This allows for O(1) lookup of game compatibility information by title ID
    /// </summary>
    private static readonly Dictionary<string, GameCompatibilityEntry> _titleIdGameMap = new Dictionary<string, GameCompatibilityEntry>();

    /// <summary>
    /// HttpClient used to grab the database
    /// Reuses the same client instance for efficiency and connection pooling
    /// </summary>
    private static readonly HttpClientService _client = new HttpClientService();

    /// <summary>
    /// Indicates whether the database has already been loaded
    /// Prevents multiple loads of the same databases in memory
    /// </summary>
    private static bool _loaded;

    /// <summary>
    /// Fallback URLs for the Game Compatibility database
    /// If the primary URL fails, the system will try secondary URLs in sequence
    /// </summary>
    private static readonly string[] _databaseUrls = Urls.GameCompatibilityDatabase;

    /// <summary>
    /// Loads the complete game compatibility database into memory.
    /// This method populates internal collections for fast game lookups and initializes the search functionality.
    /// The database is only loaded once; following calls will be skipped if already loaded.
    /// Response is cached for 1 day to reduce API calls.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <exception cref="AggregateException">Thrown when all database URLs fail to provide data</exception>
    public static async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded)
        {
            Logger.Debug<GameCompatibilityDatabase>("Database already loaded, skipping load operation");
            return;
        }

        Logger.Info<GameCompatibilityDatabase>("Loading game compatibility database");

        // Try each URL in sequence with caching
        string? response = null;
        foreach (string url in _databaseUrls)
        {
            try
            {
                response = await _client.GetAsync(url, cancellationToken, cacheKey: "compatibility_database", cacheDuration: ApiCacheDuration, cacheDirectory: AppPaths.DatabaseCacheDirectory);
                Logger.Info<GameCompatibilityDatabase>($"Successfully fetched from: {url}");
                break;
            }
            catch (Exception ex)
            {
                Logger.Warning<GameCompatibilityDatabase>($"Failed to fetch from '{url}'");
                Logger.LogExceptionDetails<GameCompatibilityDatabase>(ex);
            }
        }

        if (response == null)
        {
            Logger.Error<GameCompatibilityDatabase>($"All {_databaseUrls.Length} URLs failed to provide data");
            return;
        }

        Logger.Debug<GameCompatibilityDatabase>("Response received, deserializing JSON data");

        List<GameCompatibilityEntry>? allEntries = JsonSerializer.Deserialize<List<GameCompatibilityEntry>>(response);

        if (allEntries is null || allEntries.Count == 0)
        {
            Logger.Warning<GameCompatibilityDatabase>("Database was empty or failed to deserialize.");
            return;
        }

        Logger.Debug<GameCompatibilityDatabase>($"Deserialized {allEntries.Count} games from database");

        int processedEntries = 0;
        foreach (GameCompatibilityEntry entry in allEntries)
        {
            if (entry.Id != null)
            {
                AddGameToIndex(entry, entry.Id);
            }

            processedEntries++;
            if (processedEntries % 1000 == 0)
            {
                Logger.Trace<GameCompatibilityDatabase>($"Processed {processedEntries}/{allEntries.Count} games");
            }
        }

        FilteredDatabase = _titleIdGameMap.Values
            .DistinctBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _loaded = true;
        Logger.Info<GameCompatibilityDatabase>($"Database loaded: {FilteredDatabase.Count} unique titles, {_allTitleIds.Count} title IDs");
    }

    /// <summary>
    /// Adds a game to the internal index using the specified title ID.
    /// This enables fast lookups of game compatibility information by title ID.
    /// The title ID is normalized to uppercase for consistent comparisons.
    /// </summary>
    /// <param name="entry">The GameCompatibilityEntry object to add to the index</param>
    /// <param name="titleId">The title ID to use as the key for indexing the game</param>
    public static void AddGameToIndex(GameCompatibilityEntry entry, string titleId)
    {
        string normalized = titleId.ToUpperInvariant();
        Logger.Trace<GameCompatibilityDatabase>($"Adding game '{entry.Title}' with normalized ID '{normalized}' to index");

        if (_titleIdGameMap.TryAdd(normalized, entry))
        {
            _allTitleIds.Add(normalized);
            Logger.Trace<GameCompatibilityDatabase>($"Successfully added game with ID '{normalized}' to index");
        }
        else
        {
            Logger.Trace<GameCompatibilityDatabase>($"Game with ID '{normalized}' already exists in index, skipping duplicate");
        }
    }

    /// <summary>
    /// Filters the database based on the provided search query.
    /// This method updates the FilteredDatabase property with GameCompatibilityEntry objects that match the search query.
    /// If the search query is empty or whitespace, the full database is restored.
    /// The search is case-insensitive and matches both title IDs and game titles.
    /// </summary>
    /// <param name="searchQuery">The query string to search for in game titles and IDs</param>
    public static Task SearchDatabase(string searchQuery)
    {
        return Task.Run(() =>
        {
            Logger.Debug<GameCompatibilityDatabase>($"Searching database with query: '{searchQuery}'");

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                Logger.Debug<GameCompatibilityDatabase>("Resetting to full list due to empty search query");
                FilteredDatabase = _titleIdGameMap.Values
                    .DistinctBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                Logger.Debug<GameCompatibilityDatabase>($"Reset complete, showing all {FilteredDatabase.Count} titles");
                return;
            }

            string upperQuery = searchQuery.ToUpperInvariant();

            FilteredDatabase = _allTitleIds
                .Where(id => id.Contains(upperQuery) || _titleIdGameMap[id].Title!.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                .Select(id => _titleIdGameMap[id])
                .DistinctBy(g => g.Title, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Logger.Debug<GameCompatibilityDatabase>($"Search completed, found {FilteredDatabase.Count} matching titles");
        });
    }

    /// <summary>
    /// Retrieves GameCompatibilityEntry for a game with the specified title.
    /// Performs a case-insensitive search through all indexed games to find a match by title.
    /// </summary>
    /// <param name="gameTitle">The title of the game to search for</param>
    /// <returns>The GameCompatibilityEntry object if found, null otherwise</returns>
    public static GameCompatibilityEntry? GetGameCompatibility(string? gameTitle)
    {
        Logger.Debug<GameCompatibilityDatabase>($"Searching for game with title: '{gameTitle}'");

        GameCompatibilityEntry? result = _titleIdGameMap.Values
            .FirstOrDefault(entry => string.Equals(entry.Title, gameTitle, StringComparison.OrdinalIgnoreCase));

        Logger.Debug<GameCompatibilityDatabase>(result != null
            ? $"Found game with title: '{gameTitle}'"
            : $"Game with title '{gameTitle}' not found in database");
        return result;
    }

    /// <summary>
    /// Retrieves GameCompatibilityEntry for a game with the specified title ID.
    /// Performs a direct lookup using the internal index.
    /// </summary>
    /// <param name="titleId">The title ID of the game to search for</param>
    /// <returns>The GameCompatibilityEntry object if found, null otherwise</returns>
    public static GameCompatibilityEntry? GetGameCompatibilityById(string? titleId)
    {
        if (string.IsNullOrEmpty(titleId))
        {
            Logger.Debug<GameCompatibilityDatabase>("Title ID is null or empty");
            return null;
        }

        Logger.Debug<GameCompatibilityDatabase>($"Searching for game with title ID: '{titleId}'");

        string normalized = titleId.ToUpperInvariant();
        if (_titleIdGameMap.TryGetValue(normalized, out GameCompatibilityEntry? result))
        {
            Logger.Debug<GameCompatibilityDatabase>($"Found game with title ID: '{titleId}'");
            return result;
        }

        Logger.Debug<GameCompatibilityDatabase>($"Game with title ID '{titleId}' not found in database");
        return null;
    }

    /// <summary>
    /// Sets the compatibility rating for a game by searching the compatibility database.
    /// First searches using the primary game ID, then falls back to alternative IDs if needed.
    /// If no match is found, sets the rating to CompatibilityRating.Unknown.
    /// If multiple matches are found, attempts to filter by title, otherwise uses the first match.
    /// Updates both the Rating and Url properties of the game's Compatibility object.
    /// </summary>
    /// <param name="game">The game object to update with compatibility rating</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <returns>The resolved CompatibilityRating for the game</returns>
    public static async Task SetCompatibilityRating(Game game, CancellationToken cancellationToken = default)
    {
        Logger.Debug<GameCompatibilityDatabase>($"Setting compatibility rating for game: '{game.Title}' (ID: {game.GameId})");

        // Ensure the database is loaded
        await LoadAsync(cancellationToken);

        List<GameCompatibilityEntry> matches = [];

        // First, try to find by primary game ID
        if (!string.IsNullOrEmpty(game.GameId))
        {
            GameCompatibilityEntry? match = GetGameCompatibilityById(game.GameId);
            if (match != null)
            {
                matches.Add(match);
                Logger.Debug<GameCompatibilityDatabase>($"Found compatibility entry by primary ID '{game.GameId}': {match.State}");
            }
        }

        // If no match found by primary ID, search through alternative IDs
        if (matches.Count == 0 && game.AlternativeIDs is { Count: > 0 })
        {
            Logger.Debug<GameCompatibilityDatabase>($"Primary ID not found, searching through {game.AlternativeIDs.Count} alternative IDs");
            foreach (string altId in game.AlternativeIDs)
            {
                GameCompatibilityEntry? match = GetGameCompatibilityById(altId);
                if (match == null)
                {
                    continue;
                }
                matches.Add(match);
                Logger.Debug<GameCompatibilityDatabase>($"Found compatibility entry by alternative ID '{altId}': {match.State}");
            }
        }

        // Determine the final compatibility rating based on search results
        GameCompatibilityEntry? resultEntry = null;

        switch (matches.Count)
        {
            case 0:
                // No matches found - default to Unknown
                Logger.Debug<GameCompatibilityDatabase>($"No compatibility entry found for '{game.Title}', defaulting to Unknown");
                game.Compatibility.Rating = CompatibilityRating.Unknown;
                game.Compatibility.Url = string.Empty;
                break;
            case 1:
                // Single match found - use it
                resultEntry = matches[0];
                Logger.Debug<GameCompatibilityDatabase>($"Single match found for '{game.Title}': {resultEntry.State}");
                break;
            default:
            {
                // Multiple matches found - try to filter by title
                Logger.Debug<GameCompatibilityDatabase>($"Multiple matches ({matches.Count}) found for '{game.Title}', filtering by title");

                resultEntry = matches.FirstOrDefault(m =>
                    string.Equals(m.Title, game.Title, StringComparison.OrdinalIgnoreCase));

                if (resultEntry != null)
                {
                    Logger.Debug<GameCompatibilityDatabase>($"Found title match for '{game.Title}': {resultEntry.State}");
                }
                else
                {
                    // No title match - use the first one
                    resultEntry = matches[0];
                    Logger.Debug<GameCompatibilityDatabase>($"No title match found, using first entry for '{game.Title}': {resultEntry.State}");
                }
                break;
            }
        }

        // Update the game's compatibility if we found a match
        if (resultEntry != null)
        {
            game.Compatibility.Rating = resultEntry.State;
            game.Compatibility.Url = resultEntry.Url ?? string.Empty;
            Logger.Info<GameCompatibilityDatabase>($"Resolved compatibility rating for '{game.Title}': {resultEntry.State} ({game.Compatibility.Url})");
        }
    }

    /// <summary>
    /// Resets all static states and clears HTTP cache. Intended for test isolation only.
    /// This clears all cached data and resets the loaded state to allow for clean testing.
    /// </summary>
    public static void Reset()
    {
        _allTitleIds.Clear();
        _titleIdGameMap.Clear();
        FilteredDatabase = [];
        _loaded = false;
        Logger.Info<GameCompatibilityDatabase>("GameCompatibilityDatabase reset complete");
    }
}