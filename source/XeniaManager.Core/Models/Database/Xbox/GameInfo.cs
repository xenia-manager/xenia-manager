using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Database.Xbox;

/// <summary>
/// This is used to parse games list that are stored as .JSON files
/// </summary>
public class GameInfo
{
    /// <summary>
    /// Gets or sets the unique identifier for the game (optional)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the list of alternative identifiers for the game
    /// </summary>
    [JsonPropertyName("alternative_id")]
    public List<string> AlternativeId { get; set; }

    /// <summary>
    /// Gets or sets the title of the game (optional)
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the URL link for the game (optional)
    /// </summary>
    [JsonPropertyName("url")]
    public string? Link { get; set; }

    /// <summary>
    /// Gets or sets the artwork for the game (optional)
    /// </summary>
    [JsonPropertyName("artwork")]
    public Artwork? Artwork { get; set; }
}