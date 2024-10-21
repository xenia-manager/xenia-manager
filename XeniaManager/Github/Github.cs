using System;

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
        /// <param name="releaseNumber">By default it returns the latest release, this defines which one to be exact</param>
        /// <param name="commitish">Optional: The target branch or commit for filtering releases</param>
        /// <returns></returns>
        public static async Task<JObject> GrabRelease(string url, int releaseNumber = 0, string? commitish = null)
        {
			try
			{
                // Initialize HttpClient for interaction with Github API
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                // Get response from the url
                HttpResponseMessage response = await client.GetAsync(url);
                // Check if the repsonse was successful
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

                // Filter releases by target_commitish if provided
                if (!string.IsNullOrEmpty(commitish))
                {
                    foreach (JObject release in releases)
                    {
                        if (release["target_commitish"]?.ToString() == commitish)
                        {
                            return release;
                        }
                    }
                    Log.Warning($"No release matches the target_commitish: {commitish}");
                    return null;
                }

                // Returns the latest release as JObject
                return releases[releaseNumber] as JObject;
            }
			catch (Exception )
			{

				return null;
			}
        }
    }
}
