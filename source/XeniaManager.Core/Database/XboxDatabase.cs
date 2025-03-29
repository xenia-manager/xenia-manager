using System.Text.Json;
using System.Windows.Input;

namespace XeniaManager.Core.Database;

public static class XboxDatabase
{
    // Variables
    public static List<string> FilteredDatabase = new List<string>();
    private static HashSet<string> _allTitleIDs { get; set; }
    private static Dictionary<string, GameInfo> _titleIdGameMap { get; set; }
    private static readonly HttpClientService _client = new HttpClientService();

    // Functions
    public static async Task Load()
    {
        // Get response from the url
        string response = await _client.GetAsync(Constants.Urls.XboxDatabase);
        List<GameInfo> allGames = JsonSerializer.Deserialize<List<GameInfo>>(response);
        _allTitleIDs = new HashSet<string>();
        _titleIdGameMap = new Dictionary<string, GameInfo>();

        foreach (GameInfo game in allGames)
        {
            FilteredDatabase.Add(game.Title);
            string primaryId = game.Id.ToLower();
            if (!_titleIdGameMap.ContainsKey(primaryId))
            {
                _titleIdGameMap[primaryId] = game;
                _allTitleIDs.Add(primaryId);
            }

            if (game.AlternativeId != null)
            {
                foreach (var altId in game.AlternativeId)
                {
                    string lowerAltId = altId.ToLower();
                    if (!_titleIdGameMap.ContainsKey(lowerAltId))
                    {
                        _titleIdGameMap[lowerAltId] = game;
                        _allTitleIDs.Add(lowerAltId);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Function that searches the Xbox Marketplace list of games by both ID and Title
    /// </summary>
    /// <param name="searchQuery">Query inserted into the SearchBox, used for searching</param>
    /// <returns>
    /// A list of games that match the search criteria.
    /// </returns>
    public static Task SearchDatabase(string searchQuery)
    {
        return Task.Run(() =>
        {
            FilteredDatabase = _allTitleIDs
                .Where(id => id.Contains(searchQuery) || _titleIdGameMap[id].Title.ToLower().Contains(searchQuery.ToLower()))
                .Select(id => _titleIdGameMap[id].Title)
                .Distinct()
                .ToList();
        });
    }
}