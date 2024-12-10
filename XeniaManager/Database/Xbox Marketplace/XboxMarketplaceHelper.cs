// Imported
using Newtonsoft.Json;
using Serilog;

namespace XeniaManager.Database
{
    public static partial class XboxMarketplace
    {
        // Xbox Marketplace
        public static List<string> FilteredGames = new List<string>();
        private static HashSet<string> _allTitleIDs; // Contains both main and alternative id's
        private static Dictionary<string, GameInfo> _titleIdGameMap; // Maps TitleID's to Game

        /// <summary>
        /// Loads Xbox Marketplace Database into Xenia Manager
        /// </summary>
        /// <param name="json"></param>
        /// <returns>true if it's successful, otherwise false</returns>
        public static bool Load(string json)
        {
            try
            {
                List<GameInfo>
                    xboxMarketplaceAllGames = JsonConvert.DeserializeObject<List<GameInfo>>(json); // Loading .JSON file

                _allTitleIDs = new HashSet<string>();
                _titleIdGameMap = new Dictionary<string, GameInfo>();

                foreach (var game in xboxMarketplaceAllGames)
                {
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

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                return false;
            }
        }

        /// <summary>
        /// Function that searches the Xbox Marketplace list of games by both ID and Title
        /// </summary>
        /// <param name="searchQuery">Query inserted into the SearchBox, used for searching</param>
        /// <returns>
        /// A list of games that match the search criteria.
        /// </returns>
        public static Task Search(string searchQuery)
        {
            return Task.Run(() =>
            {
                try
                {
                    // Perform the search operation and update the filtered games list
                    FilteredGames = _allTitleIDs
                        .Where(id =>
                            id.Contains(searchQuery) ||
                            _titleIdGameMap[id].Title.ToLower().Contains(searchQuery.ToLower()))
                        .Select(id => _titleIdGameMap[id].Title)
                        .Distinct()
                        .ToList();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message + "\nFull Error:\n" + ex);
                }
            });
        }

        /// <summary>
        /// Grabs the game info from Xbox Marketplace database
        /// </summary>
        /// <returns>GameInfo if it finds a match, otherwise null</returns>
        public static GameInfo GetGameInfo(string gameTitle)
        {
            // Go through every entry until you find the one that matches gameTitle
            foreach (GameInfo game in _titleIdGameMap.Values)
            {
                if (game.Title == gameTitle)
                {
                    return game;
                }
            }

            return null;
        }
    }
}