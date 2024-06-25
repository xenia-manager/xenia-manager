using System;

// Imported via NuGet
using Newtonsoft.Json;

namespace Xenia_Manager.Classes
{
    /// <summary>
    /// This is used to parse JSON files that have game names and their box arts
    /// </summary>
    public class GameInfo
    {
        // This is for Andy Declari's JSON file
        [JsonProperty("Name")]
        public string? Name { get; set; }

        [JsonProperty("Front")]
        public CoverDetails? Front { get; set; }

        [JsonProperty("Back")]
        public CoverDetails? Back { get; set; }

        // This is for Wikipedia JSON file
        [JsonProperty("Title")]
        public string? Title { get; set; }

        [JsonProperty("Link")]
        public string? Link { get; set; }

        [JsonProperty("Image URL")]
        public string? ImageUrl { get; set; }
    }

    /// <summary>
    /// This is for CoverDetails (Andy Declari's JSON file)
    /// </summary>
    public class CoverDetails
    {
        [JsonProperty("Full Size")]
        public string? FullSize { get; set; }

        [JsonProperty("Thumbnail")]
        public string? Thumbnail { get; set; }
    }

    /// <summary>
    /// This is used to parse a JSON file that holds all of the Game Patches for Xenia
    /// </summary>
    public class GamePatch
    {
        [JsonProperty("name")]
        public required string gameName { get; set; }

        [JsonProperty("download_url")]
        public required string url { get; set; }
    }
}
