using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Game;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MousehookSupportRating
{
    /// <summary>
    /// Mousehook support status is unknown
    /// </summary>
    Unknown,

    /// <summary>
    /// Mousehook support is poor
    /// </summary>
    Poor,

    /// <summary>
    /// Mousehook support is fair
    /// </summary>
    Fair,

    /// <summary>
    /// Mousehook support is good
    /// </summary>
    Good
}
