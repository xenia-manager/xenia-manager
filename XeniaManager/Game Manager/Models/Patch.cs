// Imported
using Newtonsoft.Json;

namespace XeniaManager
{
    /// <summary>
    /// This is used to parse a JSON file that holds all the Game Patches for Xenia
    /// </summary>
    public class GamePatch
    {
        [JsonProperty("name")] public string Title { get; set; }
        [JsonProperty("sha")] public string Sha { get; set; }
        [JsonProperty("download_url")] public string Url { get; set; }
    }

    /// <summary>
    /// Patch class used to read and edit patch files
    /// </summary>
    public class Patch
    {
        /// <summary>
        /// Name of the patch
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Tells us if the patch is enabled or disabled
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Explains what patch does, can be null
        /// </summary>
        public string? Description { get; set; }
    }
}