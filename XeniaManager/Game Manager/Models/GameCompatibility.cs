using System;

// Imported
using Newtonsoft.Json;

namespace XeniaManager
{
    /// <summary>
    /// Class for parsing .JSON file containing compatibility ratings
    /// </summary>
    public class GameCompatibility
    {
        /// <summary>
        /// The unique identifier for the game
        /// </summary>
        [JsonProperty("id")]
        public string GameId { get; set; }

        /// <summary>
        /// The title of the game
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Tells the current compatibility of the game 
        /// <para>(Unknown, Unplayable, Loads, Gameplay, Playable)</para>
        /// </summary>
        [JsonProperty("state")]
        public CompatibilityRating CompatibilityRating { get; set; }

        /// <summary>
        /// The URL for more information about the game
        /// </summary>
        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
