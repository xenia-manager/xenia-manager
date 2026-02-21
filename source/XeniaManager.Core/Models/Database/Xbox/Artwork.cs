using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Database.Xbox;

/// <summary>
/// This class is used to parse the "artwork" section of the JSON database file.
/// Contains URLs to various visual assets such as box art, backgrounds, banners, icons, and gallery images.
/// </summary>
public class Artwork
{
    /// <summary>
    /// Gets or sets the URL to the box art image for the game.
    /// </summary>
    [JsonPropertyName("boxart")]
    public string? Boxart { get; set; }

    /// <summary>
    /// Gets or sets the URL to the background image for the game.
    /// </summary>
    [JsonPropertyName("background")]
    public string? Background { get; set; }

    /// <summary>
    /// Gets or sets the URL to the banner image for the game.
    /// </summary>
    [JsonPropertyName("banner")]
    public string? Banner { get; set; }

    /// <summary>
    /// Gets or sets the URL to the icon image for the game.
    /// </summary>
    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets a collection of gallery images for the game.
    /// </summary>
    [JsonPropertyName("gallery")]
    public List<string>? Gallery { get; set; }
}