using System;

// Imported
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace XeniaManager
{
    /// <summary>
    /// Used for parsing through XboxUnity API
    /// </summary>
    public partial class XboxUnity
    {
        /// <summary>
        /// Check if the response is .JSON
        /// </summary>
        /// <param name="content"></param>
        /// <returns>true if the response is in a .JSON format, otherwise false</returns>
        private static bool IsJson(string content)
        {
            try
            {
                // Try parsing the content as JSON using Newtonsoft.Json
                JToken.Parse(content);
                return true;
            }
            catch (JsonReaderException)
            {
                // If parsing fails, it's not valid JSON
                return false;
            }
        }

        /// <summary>
        /// Returns empty string or JSON as a string from the response
        /// </summary>
        /// <param name="url">URL whose response we want</param>
        private static async Task<string> GetResponse(string url)
        {
            try
            {
                // Create an instance of HttpClient
                using (HttpClient client = new HttpClient())
                {
                    // Send a GET request to the URL
                    HttpResponseMessage response = await client.GetAsync(url);

                    // Ensure the request was successful
                    response.EnsureSuccessStatusCode();

                    // Read the HTML content as a string
                    string content = await response.Content.ReadAsStringAsync();

                    if (IsJson(content))
                    {
                        return content;
                    }
                    else
                    {
                        Log.Warning("The response is not a valid JSON");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                return null;
            }
        }

        /// <summary>
        /// Returns the response as a JSON
        /// </summary>
        public static async Task<List<XboxUnityTitleUpdate>> GetTitleUpdates(string gameid, string mediaId)
        {
            try
            {
                // Url to the Title Update Info
                string url = $"http://xboxunity.net/Resources/Lib/TitleUpdateInfo.php?titleid={gameid}";

                // Checking if response returned something
                string json = await GetResponse(url);
                if (json == null)
                {
                    return null;
                }

                // Parsing HTML response
                XboxUnityAPIResponse responseAsJSON = JsonConvert.DeserializeObject<XboxUnityAPIResponse>(json);
                if (responseAsJSON.Type != 1)
                {
                    Log.Information("Unsupported response type");
                    return null;
                }

                // Check for supported Title Update's
                foreach (XboxUnityMedia media in responseAsJSON.MediaIds)
                {
                    if (media.Id == mediaId)
                    {
                        return media.Updates; // Return the list with all of the supported TitleUpdates
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                return null;
            }
        }
    }
}
