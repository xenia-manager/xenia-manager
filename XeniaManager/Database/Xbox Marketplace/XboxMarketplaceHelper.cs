using System;

// Imported
using Newtonsoft.Json;
using Serilog;

namespace XeniaManager
{
    public static partial class Database
    {
        // Xbox Marketplace
        public static List<string> XboxMarketplaceFilteredGames = new List<string>();
        private static HashSet<string> XboxMarketplaceAllTitleIDs; // Contains both main and alterantive id's
        private static Dictionary<string, GameInfo> XboxMarketplaceIDGame; // Maps TitleID's to Game

        /// <summary>
        /// Loads Xbox Marketplace Database into Xenia Manager
        /// </summary>
        /// <param name="json"></param>
        /// <returns>true if it's successful, otherwise false</returns>
        public static bool ReadXboxMarketplaceDatabase(string json)
        {
            try
            {
                List<GameInfo> XboxMarketplaceAllGames = JsonConvert.DeserializeObject<List<GameInfo>>(json); // Loading .JSON file

                XboxMarketplaceAllTitleIDs = new HashSet<string>();
                XboxMarketplaceIDGame = new Dictionary<string, GameInfo>();

                foreach (var game in XboxMarketplaceAllGames)
                {
                    string primaryId = game.Id.ToLower();
                    if (!XboxMarketplaceIDGame.ContainsKey(primaryId))
                    {
                        XboxMarketplaceIDGame[primaryId] = game;
                        XboxMarketplaceAllTitleIDs.Add(primaryId);
                    }

                    if (game.AlternativeId != null)
                    {
                        foreach (var altId in game.AlternativeId)
                        {
                            string lowerAltId = altId.ToLower();
                            if (!XboxMarketplaceIDGame.ContainsKey(lowerAltId))
                            {
                                XboxMarketplaceIDGame[lowerAltId] = game;
                                XboxMarketplaceAllTitleIDs.Add(lowerAltId);
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
        public static Task SearchXboxMarketplace(string searchQuery)
        {
            return Task.Run(() =>
            {
                try
                {
                    // Perform the search operation and update the filtered games list
                    XboxMarketplaceFilteredGames = XboxMarketplaceAllTitleIDs
                        .Where(id => id.Contains(searchQuery) || XboxMarketplaceIDGame[id].Title.ToLower().Contains(searchQuery.ToLower()))
                        .Select(id => XboxMarketplaceIDGame[id].Title)
                        .Distinct()
                        .ToList();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message + "\nFull Error:\n" + ex);
                }
            });
        }
    }
}
