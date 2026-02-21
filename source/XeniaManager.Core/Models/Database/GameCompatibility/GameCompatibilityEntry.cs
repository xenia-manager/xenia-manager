using System.Text.Json.Serialization;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.Core.Models.Database.GameCompatibility;

/// <summary>
/// Represents a single game entry in the game compatibility database.
/// Contains information about a game's compatibility status with the emulator.
/// </summary>
public class GameCompatibilityEntry
{
    /// <summary>
    /// The unique identifier for the game (title ID)
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// The title/name of the game
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// The compatibility rating indicating how well the game works with the emulator
    /// </summary>
    [JsonPropertyName("state")]
    public CompatibilityRating State { get; set; }

    /// <summary>
    /// URL to the game's compatibility information page
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}
