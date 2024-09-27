using System;

// Imported
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XeniaManager
{
    /// <summary>
    /// Every MediaID has a list of updates
    /// </summary>
    public class XboxUnityMedia
    {
        /// <summary>
        /// Media ID
        /// </summary>
        [JsonProperty("MediaID")]
        public string? Id { get; set; }

        /// <summary>
        /// List of all of the updates the media id has
        /// </summary>
        [JsonProperty("Updates")]
        public List<XboxUnityTitleUpdate>? Updates { get; set; }
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
        public string? Id { get; set; }

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
            return $"Update {Version} ({Id})";
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
        public List<XboxUnityMedia>? MediaIds { get; set; }
    }
}
