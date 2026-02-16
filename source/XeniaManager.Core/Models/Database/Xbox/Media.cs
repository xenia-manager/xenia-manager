using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Database.Xbox;

/// <summary>
/// Represents media information for a game, including ID, title, edition, and region
/// </summary>
public class Media
{
    /// <summary>
    /// Gets or sets the unique identifier for the media
    /// </summary>
    [JsonPropertyName("media_id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the title of the media
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the edition of the media
    /// </summary>
    [JsonPropertyName("edition")]
    public string? Edition { get; set; }

    /// <summary>
    /// Gets or sets the region of the media
    /// </summary>
    [JsonPropertyName("region")]
    public string? Region { get; set; }
}