// Imported
using Newtonsoft.Json;

namespace XeniaManager.Database
{
    /// <summary>
    /// Used to parse specific game details when it has been selected in Xbox Marketplace source
    /// </summary>
    public class XboxMarketplaceGameInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public Title Title { get; set; }

        [JsonProperty("genre")]
        public List<string> Genres { get; set; }

        [JsonProperty("developer")]
        public string Developer { get; set; }

        [JsonProperty("publisher")]
        public string Publisher { get; set; }

        [JsonProperty("release_date")]
        public string ReleaseDate { get; set; }

        [JsonProperty("user_rating")]
        public string UserRating { get; set; }

        [JsonProperty("description")]
        public Description Description { get; set; }

        [JsonProperty("media")]
        public List<Media> Media { get; set; }

        [JsonProperty("artwork")]
        public Artwork? Artwork { get; set; }

        [JsonProperty("products")]
        public Products products { get; set; }
    }
}