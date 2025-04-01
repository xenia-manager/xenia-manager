using System.Text.Json;

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

    // Functions
    /// <summary>
    /// Loads the games database into Xenia Manager
    /// </summary>
    public static async Task Load()
    {
        // Get response from the url
        string response = await _client.GetAsync(Constants.Urls.XboxDatabase);
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
}