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
/// </summary>
public class XboxDatabase
{
    /// <summary>
    /// Contains the filtered games database (used for displaying games after search)
    /// This list holds the titles of games that match the current search query
    /// </summary>
    public static List<string?> FilteredDatabase { get; private set; } = [];

    /// <summary>
    /// Contains every title_id for a specific game
    /// This is used for quick lookups when searching the database
    /// </summary>
    private static readonly HashSet<string> _allTitleIds = [];

    /// <summary>
    /// Mapping of title_id to GameInfo
    /// This allows for O(1) lookup of game information by title ID
    /// Multiple IDs (main ID and alternative IDs) can map to the same GameInfo object
    /// </summary>
    private static readonly Dictionary<string, GameInfo> _titleIdGameMap = new Dictionary<string, GameInfo>();

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
    /// Attempts to fetch data from a list of URLs in sequence until one succeeds.
    /// Implements a fallback mechanism to ensure robustness against server outages.
    /// If all URLs fail, throws an AggregateException containing all exceptions.
    /// </summary>
    /// <param name="urls">Array of URLs to try in sequence</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <returns>The response string from the first successful URL request</returns>
    /// <exception cref="AggregateException">Thrown when all URLs fail to provide data</exception>
    private static async Task<string> GetWithFallbackAsync(string[] urls, CancellationToken cancellationToken = default)
    {
        Logger.Debug<XboxDatabase>($"Attempting to fetch data from {urls.Length} potential URLs");
        List<Exception> exceptions = [];

        foreach (string url in urls)
        {
            try
            {
                Logger.Debug<XboxDatabase>($"Trying URL: {url}");
                string response = await _client.GetAsync(url, cancellationToken);
                Logger.Info<XboxDatabase>($"Successfully fetched from: {url}");
                return response;
            }
            catch (Exception ex)
            {
                Logger.Warning<XboxDatabase>($"Failed to fetch from '{url}': {ex.Message}");
                exceptions.Add(ex);
            }
        }

        Logger.Error<XboxDatabase>($"All {urls.Length} URLs failed to provide data");
        throw new AggregateException($"All {urls.Length} URLs failed.", exceptions);
    }

    /// <summary>
    /// Formats URL templates with the provided argument and attempts to fetch data from them in sequence.
    /// Implements a fallback mechanism to ensure robustness against server outages.
    /// </summary>
    /// <param name="urlFormats">Array of URL templates to format with the argument</param>
    /// <param name="arg">The argument to format into the URL templates (typically a title_id)</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <returns>The response string from the first successful URL request</returns>
    /// <exception cref="AggregateException">Thrown when all URLs fail to provide data</exception>
    private static async Task<string> GetWithFallbackAsync(string[] urlFormats, string arg, CancellationToken cancellationToken = default)
    {
        Logger.Debug<XboxDatabase>($"Formatting {urlFormats.Length} URL templates with argument: {arg}");
        string[] urls = urlFormats
            .Select(fmt => string.Format(fmt, arg))
            .ToArray();

        Logger.Debug<XboxDatabase>($"Formatted URLs: [{string.Join(", ", urls)}]");
        return await GetWithFallbackAsync(urls, cancellationToken);
    }

    /// <summary>
    /// Loads the complete Xbox games database from the marketplace into memory.
    /// This method populates internal collections for fast game lookups and initializes the search functionality.
    /// The database is only loaded once; subsequent calls will be skipped if already loaded.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <exception cref="AggregateException">Thrown when all database URLs fail to provide data</exception>
    public static async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_loaded)
        {
            Logger.Debug<XboxDatabase>("Database already loaded, skipping load operation");
            return;
        }

        Logger.Info<XboxDatabase>("Loading Xbox games database");

        string response = await GetWithFallbackAsync(_databaseUrls, cancellationToken);
        Logger.Debug<XboxDatabase>("Response received, deserializing JSON data");

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

        FilteredDatabase = _titleIdGameMap.Values
            .Select(g => g.Title)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _loaded = true;
        Logger.Info<XboxDatabase>($"Database loaded: {FilteredDatabase.Count} unique titles, {_allTitleIds.Count} title IDs");
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

        if (_titleIdGameMap.TryAdd(normalized, game))
        {
            _allTitleIds.Add(normalized);
            Logger.Trace<XboxDatabase>($"Successfully added game with ID '{normalized}' to index");
        }
        else
        {
            Logger.Trace<XboxDatabase>($"Game with ID '{normalized}' already exists in index, skipping duplicate");
        }
    }

    /// <summary>
    /// Filters the database based on the provided search query.
    /// This method updates the FilteredDatabase property with titles that match the search query.
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
                FilteredDatabase = _titleIdGameMap.Values
                    .Select(g => g.Title)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                Logger.Debug<XboxDatabase>($"Reset complete, showing all {FilteredDatabase.Count} titles");
                return;
            }

            string upperQuery = searchQuery.ToUpperInvariant();

            FilteredDatabase = _allTitleIds
                .Where(id => id.Contains(upperQuery)
                             || _titleIdGameMap[id].Title!.Contains(searchQuery,
                                 StringComparison.OrdinalIgnoreCase))
                .Select(id => _titleIdGameMap[id].Title)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            Logger.Debug<XboxDatabase>($"Search completed, found {FilteredDatabase.Count} matching titles");
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

        GameInfo? result = _titleIdGameMap.Values
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
    /// </summary>
    /// <param name="titleId">The title ID of the game to fetch detailed information for</param>
    /// <param name="cancellationToken">Token to cancel the operation if needed</param>
    /// <returns>The detailed game information if found and successfully retrieved, null otherwise</returns>
    public static async Task<GameDetailedInfo?> GetFullGameInfo(string titleId, CancellationToken cancellationToken = default)
    {
        Logger.Debug<XboxDatabase>($"Initiating request for full game info with title ID: '{titleId}'");

        try
        {
            Logger.Info<XboxDatabase>($"Fetching full game info for '{titleId}'");
            string response = await GetWithFallbackAsync(_gameInfoUrls, titleId, cancellationToken);

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
        catch (AggregateException ex)
        {
            Logger.Error<XboxDatabase>($"All URLs failed when fetching game info for '{titleId}'");
            Logger.LogExceptionDetails<XboxDatabase>(ex);
            return null;
        }
        catch (Exception ex)
        {
            Logger.Error<XboxDatabase>($"Unexpected error fetching game info for '{titleId}'");
            Logger.LogExceptionDetails<XboxDatabase>(ex);
            return null;
        }
    }

    /// <summary>
    /// Resets all static state. Intended for test isolation only.
    /// This clears all cached data and resets the loaded state to allow for clean testing.
    /// </summary>
    public static void Reset()
    {
        _allTitleIds.Clear();
        _titleIdGameMap.Clear();
        FilteredDatabase = [];
        _loaded = false;
    }
}