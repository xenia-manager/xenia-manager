﻿using System;

// Imported
using Newtonsoft.Json;
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Loads the compatibility list
        /// </summary>
        public static async Task LoadCompatibilityList()
        {
            if (GameCompatibilityList != null && GameCompatibilityList.Count > 0)
            {
                return;
            }
            string url = "https://raw.githubusercontent.com/xenia-manager/database/v2-backup/Database/game_compatibility.json";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                HttpResponseMessage response;
                try
                {
                    response = await client.GetAsync(url);
                }
                catch (HttpRequestException)
                {
                    GameCompatibilityList = new List<GameCompatibility>();
                    return;
                }

                // Check if the response was successful
                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"Failed to fetch folder contents. Status code: {response.StatusCode}");
                    return;
                }

                // Parse the response
                string json = await response.Content.ReadAsStringAsync();
                try
                {
                    GameCompatibilityList = JsonConvert.DeserializeObject<List<GameCompatibility>>(json);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message + "\nFull Error:\n" + ex);
                    return;
                }
            }
        }

        /// <summary>
        /// Updates the compatibility ratings for the games in the Xenia Manager
        /// </summary>
        /// <returns></returns>
        public static async Task UpdateCompatibilityRatings()
        {
            // Load compatibility lists
            await LoadCompatibilityList();

            // Go through every game and update compatibility ratings
            foreach (Game game in Games)
            {
                // Search for the game through gameid
                List<GameCompatibility> searchResults = GameCompatibilityList.Where(s => s.GameId == game.GameId).ToList();

                // Check if there are any searchResults and if there are not, try using alternative id's (If they exist)
                if (searchResults.Count == 0)
                {
                    Log.Warning("Searching with default gameid didn't bring any search results, using alternative game id's");
                    // Search for the game with alternative gameid's
                    foreach (string gameid in game.AlternativeIDs)
                    {
                        searchResults = GameCompatibilityList.Where(s => s.GameId == gameid).ToList();
                        if (searchResults.Count > 0)
                        {
                            break;
                        }
                    }
                }

                // Do the appropriate action
                switch (searchResults.Count)
                {
                    case 0:
                        Log.Information($"The compatibility page for {game.Title} hasn't been found found");
                        game.GameCompatibilityUrl = null;
                        game.CompatibilityRating = CompatibilityRating.Unknown;
                        break;
                    case 1:
                        Log.Information($"{game.Title} Compatibility Rating: {game.CompatibilityRating} -> {searchResults[0].CompatibilityRating}");
                        game.GameCompatibilityUrl = searchResults[0].Url;
                        game.CompatibilityRating = searchResults[0].CompatibilityRating;
                        break;
                    default:
                        Log.Information($"Multiple compatibility pages found");
                        Log.Information($"Trying to parse them");
                        foreach (GameCompatibility result in searchResults)
                        {
                            if (result.Title == game.Title)
                            {
                                Log.Information($"{game.Title} Compatibility Rating: {game.CompatibilityRating} -> {result.CompatibilityRating}");
                                game.GameCompatibilityUrl = result.Url;
                                game.CompatibilityRating = result.CompatibilityRating;
                                break;
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Grabs the URL to the compatibility page of the game
        /// </summary>
        private static async Task GetGameCompatibility(Game newGame, string gameid)
        {
            try
            {
                Log.Information($"Trying to find the compatibility page for {newGame.Title}");

                // Check if the game compatibility list is loaded, if it's not, try to load it
                await LoadCompatibilityList();

                // Search for the game through gameid
                List<GameCompatibility> searchResults = GameCompatibilityList.Where(s => s.GameId == gameid).ToList();

                switch (searchResults.Count)
                {
                    case 0:
                        Log.Information($"The compatibility page for {newGame.Title} isn't found");
                        newGame.GameCompatibilityUrl = null;
                        newGame.CompatibilityRating = CompatibilityRating.Unknown;
                        break;
                    case 1:
                        Log.Information($"Found the compatibility page for {newGame.Title}");
                        Log.Information($"URL: {searchResults[0].Url}");
                        newGame.GameCompatibilityUrl = searchResults[0].Url;
                        newGame.CompatibilityRating = searchResults[0].CompatibilityRating;
                        break;
                    default:
                        Log.Information($"Multiple compatibility pages found");
                        Log.Information($"Trying to parse them");
                        foreach (GameCompatibility result in searchResults)
                        {
                            if (result.Title == newGame.Title)
                            {
                                Log.Information($"Found the compatibility page for {newGame.Title}");
                                Log.Information($"URL: {result.Url.ToString()}");
                                newGame.GameCompatibilityUrl = result.Url;
                                newGame.CompatibilityRating = result.CompatibilityRating;
                                break;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
            }
        }
    }
}
