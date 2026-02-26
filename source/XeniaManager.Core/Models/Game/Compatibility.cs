using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Game;

/// <summary>
/// Current compatibility of the emulator with the game
/// </summary>
public class Compatibility
{
    /// <summary>
    /// URL to the compatibility page
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Compatibility rating
    /// </summary>
    [JsonPropertyName("rating")]
    public CompatibilityRating Rating { get; set; }
}