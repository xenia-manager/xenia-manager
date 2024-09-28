using System;

// Imported
using Newtonsoft.Json;

namespace XeniaManager
{
    /// <summary>
    /// This is used to parse a JSON file that holds all of the Game Patches for Xenia
    /// </summary>
    public class GamePatch
    {
        [JsonProperty("name")]
        public string Title { get; set; }

        [JsonProperty("sha")]
        public string Sha { get; set; }

        [JsonProperty("download_url")]
        public string Url { get; set; }
    }
}
