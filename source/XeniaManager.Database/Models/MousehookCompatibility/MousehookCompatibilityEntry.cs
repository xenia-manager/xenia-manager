using System.Text.Json.Serialization;
using XeniaManager.Database.Converters;
using XeniaManager.Database.Models.Game;

namespace XeniaManager.Database.Models.MousehookCompatibility;

/// <summary>
/// Represents a single game entry in the mousehook compatibility database.
/// Contains information about a game's mousehook support status.
/// </summary>
public class MousehookCompatibilityEntry
{
    /// <summary>
    /// The unique identifier(s) for the game (title ID).
    /// Can be a single string or an array of strings in the source JSON.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonConverter(typeof(StringOrArrayJsonConverter))]
    public List<string> Ids { get; set; } = [];

    /// <summary>
    /// The title/name of the game
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// The mousehook support rating
    /// </summary>
    [JsonPropertyName("mouse_support")]
    public MousehookSupportRating MouseSupport { get; set; }

    /// <summary>
    /// The supported versions for mousehook
    /// </summary>
    [JsonPropertyName("supported_versions")]
    public string? SupportedVersions { get; set; }

    /// <summary>
    /// Additional notes about mousehook support
    /// </summary>
    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}