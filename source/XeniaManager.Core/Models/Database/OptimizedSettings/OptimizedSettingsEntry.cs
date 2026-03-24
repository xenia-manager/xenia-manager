using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Database.OptimizedSettings;

/// <summary>
/// Represents an entry in the optimized settings database.
/// Contains information about game-specific optimized settings configurations.
/// </summary>
public class OptimizedSettingsEntry
{
    /// <summary>
    /// The unique identifier for the game (Title ID).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The title/name of the game.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The last modified date of the settings entry.
    /// </summary>
    [JsonPropertyName("last_modified")]
    public string LastModified { get; set; } = string.Empty;
}