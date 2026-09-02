using System.Text.Json.Serialization;
using XeniaManager.Database.Models.Game;

namespace XeniaManager.Core.Models.Game;

/// <summary>
/// Mousehook compatibility information for a game
/// </summary>
public class MousehookCompatibility
{
    /// <summary>
    /// Mousehook support rating
    /// </summary>
    [JsonPropertyName("rating")]
    public MousehookSupportRating Rating { get; set; }

    /// <summary>
    /// Notes about mousehook support for this game
    /// </summary>
    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}