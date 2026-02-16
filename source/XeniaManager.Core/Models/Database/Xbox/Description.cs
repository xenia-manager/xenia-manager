using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Database.Xbox;

/// <summary>
/// Represents the description information for a game, including both full and short descriptions
/// </summary>
public class Description
{
    /// <summary>
    /// Gets or sets the full description of the game
    /// </summary>
    [JsonPropertyName("full")]
    public string? Full { get; set; }

    /// <summary>
    /// Gets or sets the short description of the game
    /// </summary>
    [JsonPropertyName("short")]
    public string? Short { get; set; }
}