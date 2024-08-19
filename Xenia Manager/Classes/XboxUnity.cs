using System;
using System.Net.Http;
using System.Security.Policy;
using System.Windows;

// Imported
using Newtonsoft.Json;
using Serilog;

namespace Xenia_Manager.Classes
{
    /// <summary>
    /// Every MediaID has a list of updates
    /// </summary>
    public class XboxUnityMediaID
    {
        /// <summary>
        /// Media ID
        /// </summary>
        [JsonProperty("MediaID")]
        public string? id { get; set; }

        /// <summary>
        /// List of all of the updates the media id has
        /// </summary>
        [JsonProperty("Updates")]
        public List<XboxUnityTitleUpdate>? updates { get; set; }
    }

    /// <summary>
    /// Information about the update
    /// </summary>
    public class XboxUnityTitleUpdate
    {
        /// <summary>
        /// Title Update ID that is needed for downloading the TU
        /// </summary>
        [JsonProperty("TitleUpdateID")]
        public string? id { get; set; }

        /// <summary>
        /// Version of the update
        /// </summary>
        [JsonProperty("Version")]
        public string? Version { get; set; }

        /// <summary>
        /// Version of the update
        /// </summary>
        [JsonProperty("Name")]
        public string? Name { get; set; }

        /// <summary>
        /// Returns a string representation of the object, which will be displayed in the ListBox.
        /// </summary>
        public override string ToString()
        {
            return $"Update {Version} ({id})";
        }
    }

    /// <summary>
    /// Response from XboxUnity
    /// </summary>
    public class XboxUnityAPIResponse
    {
        /// <summary>
        /// Type 1 - Has TU's
        /// Type 2 - No TU's = Skip
        /// </summary>
        [JsonProperty("Type")]
        public int Type { get; set; }

        /// <summary>
        /// All of the grabbed media ID's
        /// We need to match this before downloading updates
        /// </summary>
        [JsonProperty("MediaIDS")]
        public List<XboxUnityMediaID>? MediaIds { get; set; }
    }

    /// <summary>
    /// Holds everything for grabbing XboxUnity TU's for a certain game
    /// </summary>
    public class XboxUnity
    {
        /// <summary>
        /// Game ID
        /// </summary>
        public string gameid { get; set; }

        /// <summary>
        /// Media ID
        /// </summary>
        public string mediaid { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="gameid">Game ID</param>
        /// <param name="mediaid">Media ID</param>
        public XboxUnity(string gameid, string mediaid)
        {
            this.gameid = gameid;
            this.mediaid = mediaid;
        }

        /// <summary>
        /// Returns empty string or JSON as a string from the response
        /// </summary>
        /// <param name="url">URL whose response we want</param>
        private async Task<string> GetResponse(string url)
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
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                return "";
            }
        }

        /// <summary>
        /// Returns the response as a JSON
        /// </summary>
        public async Task<List<XboxUnityTitleUpdate>> GetTitleUpdates()
        {
            try
            {
                // Url to the Title Update Info
                string url = $"http://xboxunity.net/Resources/Lib/TitleUpdateInfo.php?titleid={gameid}";

                // Checking if response returned something
                string json = await GetResponse(url);
                if (json == "")
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
                foreach (XboxUnityMediaID mediaid in responseAsJSON.MediaIds)
                {
                    if (mediaid.id == this.mediaid)
                    {
                        return mediaid.updates;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return null;
            }
        }
    }
}
