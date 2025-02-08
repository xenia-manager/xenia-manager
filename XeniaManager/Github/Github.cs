// Imported
using Newtonsoft.Json.Linq;
using Serilog;

namespace XeniaManager
{
    public static partial class Github
    {
        /// <summary>
        /// Grabs the latest release from GithubAPI
        /// </summary>
        /// <param name="url">URL for the </param>
        /// <param name="releaseNumber">By default, it returns the latest release, this defines which one to be exact</param>
        /// <param name="commitish">Optional: The target branch or commit for filtering releases</param>
        /// <returns></returns>
        public static async Task<JObject> GrabRelease(string url, int releaseNumber = 0, string? commitish = null)
        {
            try
            {
                // Initialize HttpClient for interaction with GitHub API
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                // Get response from the url
                HttpResponseMessage response = await client.GetAsync(url);
                // Check if the response was successful
                if (!response.IsSuccessStatusCode)
                {
                    Log.Error($"There was an issue with the Github API, status code: {response.StatusCode}");
                    return null;
                }

                Log.Information("Got the response from the Github API");
                string json = await response.Content.ReadAsStringAsync();
                JArray releases = JArray.Parse(json);
                if (releases == null)
                {
                    Log.Error("Couldn't find latest release");
                    return null;
                }
                
                // Sorted releases by published_at
                List<JToken> sortedReleases = releases
                    .Where(r => r["published_at"] != null)
                    .OrderByDescending(r => DateTime.Parse(r["published_at"].ToString()))
                    .ToList();

                // Filter releases by target_commitish if provided
                if (!string.IsNullOrEmpty(commitish))
                {
                    foreach (var jToken in sortedReleases)
                    {
                        JObject release = (JObject)jToken;
                        if (release["target_commitish"]?.ToString() == commitish)
                        {
                            return release;
                        }
                    }

                    Log.Warning($"No release matches the target_commitish: {commitish}");
                    return null;
                }

                // Returns the latest release as JObject
                return sortedReleases[releaseNumber] as JObject;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}