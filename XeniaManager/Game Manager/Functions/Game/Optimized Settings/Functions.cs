// Imported
using Newtonsoft.Json.Linq;
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Function that tries to grab optimized settings from the url
        /// </summary>
        /// <param name="titleid">titleid we're using to look for optimized settings</param>
        /// <returns>Response as JToken if the status code is success, otherwise null</returns>
        private static async Task<JToken> FetchOptimizedSettings(string titleid)
        {
            try
            {
                string url =
                    @$"https://raw.githubusercontent.com/xenia-manager/Optimized-Settings/main/Settings/{titleid}.json";
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent",
                        "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        return JObject.Parse(await response.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        Log.Error($"Failed to find optimized settings with this titleid");
                        return null;
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message, "");
                return null;
            }
        }

        /// <summary>
        /// Searches for optimized settings in the repository
        /// </summary>
        /// <param name="titleid">Game titleid used for searching for optimized settings</param>
        public static async Task<JToken> SearchForOptimizedSettings(string titleid)
        {
            // Load the games list
            Log.Information("Loading list of games");
            string url =
                "https://raw.githubusercontent.com/xenia-manager/Database/refs/heads/main/Database/xbox_marketplace_games.json";
            JArray gamesArray;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("User-Agent",
                        "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        gamesArray = JArray.Parse(await response.Content.ReadAsStringAsync());
                    }
                    else
                    {
                        Log.Error($"Failed to load list of games ({response.StatusCode})");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message, "");
                    return null;
                }
            };
            // Do search for the game by titleid & alternativeid
            Log.Information("Doing a search by titleid and alternativeid");
            var result = gamesArray.FirstOrDefault(game =>
                (game["id"] != null && game["id"].ToString() == titleid) ||
                (game["alternative_id"] != null &&
                 ((JArray)game["alternative_id"]).Any(altId => altId.ToString() == titleid)
                ));
            if (result != null)
            {
                // Look for optimized settings configuration and then send it back
                // First do search by main titleid
                Log.Information("Found a match, looking for optimized settings");
                JToken optimizedSettings = await FetchOptimizedSettings(titleid);
                if (optimizedSettings != null)
                {
                    return optimizedSettings;
                }

                // If that fails, try to search for alternative_id
                List<string> alternativeids = result["alternative_id"].ToObject<List<string>>();
                foreach (string alternativeid in alternativeids)
                {
                    optimizedSettings = await FetchOptimizedSettings(alternativeid);
                    if (optimizedSettings != null)
                    {
                        return optimizedSettings;
                    }
                }
            }

            return null;
        }
    }
}