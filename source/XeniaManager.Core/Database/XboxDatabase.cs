using System.Text.Json;
using XeniaManager.Core.Constants;

namespace XeniaManager.Core.Database;

/// <summary>
/// Library used to interact with the XboxDatabase from Xenia Manager's Database repository
/// </summary>
public static class XboxDatabase
{
    // Variables
    /// <summary>
    /// Contains filtered games database (used for displaying games after search)
    /// </summary>
    public static List<string> FilteredDatabase = new List<string>();
    
    /// <summary>
    /// Contains every title_id for a specific game
    /// </summary>
    private static HashSet<string> _allTitleIDs { get; set; } = new HashSet<string>();
    
    /// <summary>
    /// Mapping of title_id and GameInfo that has that specific title_id
    /// </summary>
    private static Dictionary<string, GameInfo> _titleIdGameMap { get; set; } = new Dictionary<string, GameInfo>();
    
    /// <summary>
    /// HttpClient used to grab the database
    /// </summary>
    private static readonly HttpClientService _client = new HttpClientService();
    private static bool _loaded { get; set; }

    // Functions
    /// <summary>
    /// Loads the games database into Xenia Manager
    /// </summary>
    public static async Task Load()
    {
        if (_loaded)
        {
            return;
        }
        Logger.Info("Loading Xbox games database");
        // Get response from the url
        string response = await _client.GetAsync(Urls.XboxDatabase);
        List<GameInfo> allGames = JsonSerializer.Deserialize<List<GameInfo>>(response);

        foreach (GameInfo game in allGames)
        {
            FilteredDatabase.Add(game.Title);
            string primaryId = game.Id.ToUpper();
            
            if (_titleIdGameMap.TryAdd(primaryId, game))
            {
                _allTitleIDs.Add(primaryId);
            }

            if (game.AlternativeId is { Count: > 0 })
            {
                foreach (string altId in game.AlternativeId)
                {
                    string lowerAltId = altId.ToUpper();
                    if (_titleIdGameMap.TryAdd(lowerAltId, game))
                    {
                        _allTitleIDs.Add(lowerAltId);
                    }
                }
            }
        }
        _loaded = true;
    }

    /// <summary>
    /// Searches the Xbox Marketplace list of games by both ID and Title
    /// </summary>
    /// <param name="searchQuery">Query inserted into the SearchBox used for filtering games in the database</param>
    /// <returns>
    /// A list of games that match the search criteria.
    /// </returns>
    public static Task SearchDatabase(string searchQuery)
    {
        return Task.Run(() =>
        {
            FilteredDatabase = _allTitleIDs
                .Where(id => id.Contains(searchQuery.ToUpper()) || _titleIdGameMap[id].Title.ToLower().Contains(searchQuery.ToLower()))
                .Select(id => _titleIdGameMap[id].Title)
                .Distinct()
                .ToList();
        });
    }

    /// <summary>
    /// Searches through database to find gameinfo about selected game
    /// </summary>
    /// <param name="gameTitle">Game title to look GameInfo for</param>
    /// <returns>GameInfo about selected game;otherwise null</returns>
    public static GameInfo GetShortGameInfo(string gameTitle)
    {
        return _titleIdGameMap.Values.FirstOrDefault(game => game.Title == gameTitle);
    }

    /// <summary>
    /// Fetches full game info from GitHub repository
    /// </summary>
    /// <param name="titleId">Game title id</param>
    /// <returns>Full game info about a game; otherwise null</returns>
    public static async Task<XboxDatabaseGameInfo> GetFullGameInfo(string titleId)
    {
        string url = string.Format(Constants.Urls.XboxDatabaseGameInfo, titleId);
        try
        {
            Logger.Info("Trying to fetch game info");
            return JsonSerializer.Deserialize<XboxDatabaseGameInfo>(await _client.GetAsync(url));
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return null;
        }
    }
}