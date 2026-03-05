using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Database.Xbox;

/// <summary>
/// Xbox Marketplace game info
/// </summary>
public class Title
{
    /// <summary>
    /// Gets or sets the full title of the game
    /// </summary>
    [JsonPropertyName("full")]
    public string? Full { get; set; }

    /// <summary>
    /// Gets or sets the reduced title of the game
    /// </summary>
    [JsonPropertyName("reduced")]
    public string? Reduced { get; set; }
}