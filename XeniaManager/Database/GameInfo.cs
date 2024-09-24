using System;

// Imported
using Newtonsoft.Json;

namespace XeniaManager
{
    /// <summary>
    /// This is used to parse games list that are stored as .JSON files
    /// </summary>
    public class GameInfo
    {
        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("alternative_id")]
        public List<string> AlternativeId { get; set; }

        [JsonProperty("title")]
        public string? Title { get; set; }

        [JsonProperty("url")]
        public string? Link { get; set; }

        [JsonProperty("artwork")]
        public Artwork? Artwork { get; set; }
    }

    /// <summary>
    /// This is used to parse the "artwork" section of .JSON file
    /// </summary>
    public class Artwork
    {
        // Universal
        [JsonProperty("boxart")]
        public string Boxart { get; set; }

        // Launchbox DB specific
        [JsonProperty("disc")]
        public string? Disc { get; set; }

        [JsonProperty("logo")]
        public string? Logo { get; set; }

        // Xbox Marketplace specific
        [JsonProperty("background")]
        public string? Background { get; set; }

        [JsonProperty("banner")]
        public string? Banner { get; set; }

        [JsonProperty("icon")]
        public string? Icon { get; set; }

        [JsonProperty("gallery")]
        public List<string>? Gallery { get; set; }
    }

    /// <summary>
    /// Xbox Marketplace game info
    /// </summary>
    public class Title
    {
        [JsonProperty("full")]
        public string Full { get; set; }

        [JsonProperty("reduced")]
        public string Reduced { get; set; }
    }

    public class Description
    {
        [JsonProperty("full")]
        public string Full { get; set; }

        [JsonProperty("short")]
        public string Short { get; set; }
    }

    public class Media
    {
        [JsonProperty("media_id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("edition")]
        public string Edition { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }
    }

    public class Parent
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }

    public class Products
    {
        [JsonProperty("parent")]
        public List<Parent> Parent { get; set; }

        [JsonProperty("related")]
        public List<object> Related { get; set; }
    }
}
