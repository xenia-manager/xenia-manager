using System.Text.Json.Serialization;

namespace XeniaManager.Database.Models.Game;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NetplayStatusValue
{
    /// <summary>
    /// Status is unknown or untested
    /// </summary>
    Unknown,

    /// <summary>
    /// Works correctly
    /// </summary>
    Ok,

    /// <summary>
    /// Partially works with issues
    /// </summary>
    Partial,

    /// <summary>
    /// Does not work
    /// </summary>
    Fail
}