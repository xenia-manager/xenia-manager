using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Database.Xbox;

/// <summary>
/// Used to parse specific game details when it has been selected in the Xbox Marketplace source
/// </summary>
public class GameDetailedInfo
{
    /// <summary>
    /// Gets or sets the unique identifier for the game
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the title information for the game
    /// </summary>
    [JsonPropertyName("title")]
    public Title Title { get; set; }

    /// <summary>
    /// Gets or sets the list of genres associated with the game
    /// </summary>
    [JsonPropertyName("genre")]
    public List<string> Genres { get; set; }

    /// <summary>
    /// Gets or sets the developer of the game
    /// </summary>
    [JsonPropertyName("developer")]
    public string Developer { get; set; }

    /// <summary>
    /// Gets or sets the publisher of the game
    /// </summary>
    [JsonPropertyName("publisher")]
    public string Publisher { get; set; }

    /// <summary>
    /// Gets or sets the release date of the game
    /// </summary>
    [JsonPropertyName("release_date")]
    public string ReleaseDate { get; set; }

    /// <summary>
    /// Gets or sets the user rating of the game
    /// </summary>
    [JsonPropertyName("user_rating")]
    public string UserRating { get; set; }

    /// <summary>
    /// Gets or sets the description information for the game
    /// </summary>
    [JsonPropertyName("description")]
    public Description Description { get; set; }

    /// <summary>
    /// Gets or sets the list of media associated with the game
    /// </summary>
    [JsonPropertyName("media")]
    public List<Media> Media { get; set; }

    /// <summary>
    /// Gets or sets the artwork for the game (optional)
    /// </summary>
    [JsonPropertyName("artwork")]
    public Artwork? Artwork { get; set; }

    /// <summary>
    /// Gets or sets the product information for the game
    /// </summary>
    [JsonPropertyName("products")]
    public Products products { get; set; }
}