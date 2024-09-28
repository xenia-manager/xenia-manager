using System;

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
            if (gameCompatibilityList != null)
            {
                return;
            }
            string url = "https://raw.githubusercontent.com/xenia-manager/Database/refs/heads/main/Database/game_compatibility.json";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                HttpResponseMessage response = await client.GetAsync(url);

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
                    gameCompatibilityList = JsonConvert.DeserializeObject<List<GameCompatibility>>(json);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message + "\nFull Error:\n" + ex);
                    return;
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
                List<GameCompatibility> searchResults = gameCompatibilityList.Where(s => s.GameId == gameid).ToList();

                switch (searchResults.Count)
                {
                    case 0:
                        Log.Information($"The compatibility page for {newGame.Title} isn't found");
                        newGame.GameCompatibilityURL = null;
                        newGame.CompatibilityRating = CompatibilityRating.Unknown;
                        break;
                    case 1:
                        Log.Information($"Found the compatibility page for {newGame.Title}");
                        Log.Information($"URL: {searchResults[0].Url}");
                        newGame.GameCompatibilityURL = searchResults[0].Url;
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
                                newGame.GameCompatibilityURL = result.Url;
                                newGame.CompatibilityRating = result.CompatibilityRating;
                                break;
                            }
                        }
                        break;
                }

                /*
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    HttpResponseMessage response = await client.GetAsync($"https://api.github.com/search/issues?q={gameid}%20in%3Atitle%20repo%3Axenia-canary%2Fgame-compatibility");

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        JObject jsonObject = JObject.Parse(json);
                        JArray searchResults = (JArray)jsonObject["items"];
                        switch (searchResults.Count)
                        {
                            case 0:
                                Log.Information($"The compatibility page for {newGame.Title} isn't found");
                                newGame.GameCompatibilityURL = null;
                                break;
                            case 1:
                                Log.Information($"Found the compatibility page for {newGame.Title}");
                                Log.Information($"URL: {searchResults[0]["html_url"].ToString()}");
                                newGame.GameCompatibilityURL = searchResults[0]["html_url"].ToString();
                                break;
                            default:
                                Log.Information($"Multiple compatibility pages found");
                                Log.Information($"Trying to parse them");
                                foreach (JToken result in searchResults)
                                {
                                    string originalResultTitle = result["title"].ToString();
                                    string[] parts = originalResultTitle.Split(new string[] { " - " }, StringSplitOptions.None);
                                    string resultTitle = parts[1];
                                    if (resultTitle == newGame.Title)
                                    {
                                        Log.Information($"Found the compatibility page for {newGame.Title}");
                                        Log.Information($"URL: {result["html_url"].ToString()}");
                                        newGame.GameCompatibilityURL = result["html_url"].ToString();
                                        break;
                                    }
                                }
                                break;
                        }
                    }
                }
                */
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
            }
        }
    }
}
