using System.Text.Json;
using System.Text.Json.Serialization;

namespace XeniaManager.Core.Game;

public class GameCompatibility
{
    /// <summary>
    /// The unique identifier for the game.
    /// </summary>
    [JsonPropertyName("id")]
    public string GameId { get; set; }

    /// <summary>
    /// The title of the game.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; }

    /// <summary>
    /// Tells the current compatibility of the game 
    /// <para>(Unknown, Unplayable, Loads, Gameplay, Playable)</para>
    /// </summary>
    [JsonPropertyName("state")]
    public CompatibilityRating CompatibilityRating { get; set; }

    /// <summary>
    /// The URL for more information about the game.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; }
}

public static class CompatibilityManager
{
    // Variables
    private static List<GameCompatibility> _gameCompatibilityList { get; set; }
    private static readonly HttpClientService _httpClientService = new HttpClientService();

    /// <summary>
    /// Loads the compatibility list once from the remote source.
    /// </summary>
    private static async Task LoadCompatibilityList()
    {
        if (_gameCompatibilityList != null && _gameCompatibilityList.Count > 0)
        {
            Logger.Debug("Game compatibility list is already loaded.");
            return;
        }
        Logger.Info("Loading game compatibility list.");
        string jsonData = await _httpClientService.GetAsync(Constants.Urls.GameCompatibility);
        _gameCompatibilityList = JsonSerializer.Deserialize<List<GameCompatibility>>(jsonData);
    }

    /// <summary>
    /// Returns a list of game compatibility entries matching the specified game ID.
    /// </summary>
    /// <param name="gameId">The game ID to search for.</param>
    /// <returns>A list of <see cref="GameCompatibility"/> entries matching the game ID.</returns>
    private static List<GameCompatibility> GetSearchResults(string titleId)
    {
        return _gameCompatibilityList.Where(g => g.GameId == titleId).ToList();
    }

    /// <summary>
    /// Updates the compatibility details for the specified game based on the search results.
    /// </summary>
    /// <param name="game">The game to update compatibility for.</param>
    /// <param name="searchResults">A list of search results containing compatibility data.</param>
    private static void SetGameCompatibility(Game game, List<GameCompatibility> searchResults)
    {
        switch (searchResults.Count)
        {
            case 0:
                Logger.Warning($"The compatibility page for {game.Title} isn't found.");
                game.Compatibility.Url = null;
                game.Compatibility.Rating = CompatibilityRating.Unknown;
                break;
            case 1:
                Logger.Info($"Found the compatibility page for {game.Title}.");
                Logger.Info($"URL: {searchResults[0].Url}");
                game.Compatibility = new Compatibility
                {
                    Url = searchResults[0].Url,
                    Rating = searchResults[0].CompatibilityRating
                };
                break;
            default:
                Logger.Info($"Multiple compatibility pages found for {game.Title}. Trying to select the best match.");
                // Search for an exact title match (ignoring case).
                GameCompatibility match = searchResults.FirstOrDefault(s => s.Title.Equals(game.Title, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    Logger.Info($"Exact title match found for {game.Title}: {match.Url}");
                    game.Compatibility = new Compatibility
                    {
                        Url = match.Url,
                        Rating = match.CompatibilityRating
                    };
                }
                else
                {
                    Logger.Info($"No exact title match found; using the first match for {game.Title}.");
                    game.Compatibility = new Compatibility
                    {
                        Url = searchResults[0].Url,
                        Rating = searchResults[0].CompatibilityRating
                    };
                }
                break;
        }
    }

    /// <summary>
    /// Retrieves the compatibility information for a single game based on the specified game ID.
    /// </summary>
    /// <param name="game">The game for which to retrieve compatibility data.</param>
    /// <param name="titleId">The game ID to search for in the compatibility list.</param>
    public static async Task GetCompatibility(Game game, string titleId)
    {
        await LoadCompatibilityList();
        Logger.Info($"Trying to find the compatibility page for {game.Title} using game id: {titleId}");
        List<GameCompatibility> searchResults = GetSearchResults(titleId);
        SetGameCompatibility(game, searchResults);
    }

    /// <summary>
    /// Updates the compatibility information for all games managed by the GameManager.
    /// </summary>
    public static async Task UpdateCompatibility()
    {
        await LoadCompatibilityList();

        foreach (Game game in GameManager.Games)
        {
            List<GameCompatibility> searchResults = GetSearchResults(game.GameId);

            if (searchResults.Count == 0)
            {
                Logger.Warning($"No results found for default game id for {game.Title}. Trying alternative IDs.");
                // Try alternative game IDs if available.
                foreach (string alternativeId in game.AlternativeIDs)
                {
                    searchResults = GetSearchResults(alternativeId);
                    if (searchResults.Count > 0)
                    {
                        break;
                    }
                }
            }
            Logger.Info($"Current compatibility rating: {game.Compatibility.Rating}");
            SetGameCompatibility(game, searchResults);
            Logger.Info($"New compatibility rating: {game.Compatibility.Rating}");
        }
    }
}
