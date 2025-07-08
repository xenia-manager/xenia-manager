using System.Text.Json.Serialization;

namespace XeniaManager.Core.Enum;
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CompatibilityRating
{
    Unknown,
    Unplayable,
    Loads,
    Gameplay,
    Playable
}