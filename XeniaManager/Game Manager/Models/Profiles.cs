using System;

// Imported
using Newtonsoft.Json;

namespace XeniaManager
{
    /// <summary>
    /// Used to store logged in gamer profiles when running the game (Useful for backing up saves)
    /// </summary>
    public class GamerProfile
    {
        /// <summary>
        /// GUID of the profile
        /// </summary>
        [JsonProperty("guid")]
        public string? GUID { get; set; }
        
        /// <summary>
        /// Name of the profile
        /// </summary>
        [JsonProperty("name")]
        public string? Name { get; set; }
        
        /// <summary>
        /// Slot where the profile is loaded
        /// </summary>
        [JsonProperty("slot")]
        public string? Slot { get; set; }
    }
}