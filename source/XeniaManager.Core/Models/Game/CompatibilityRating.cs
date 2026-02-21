using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Game;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CompatibilityRating
{
    /// <summary>
    /// Compatibility status is unknown
    /// </summary>
    Unknown,
    
    /// <summary>
    /// Game is unplayable
    /// </summary>
    Unplayable,
    
    /// <summary>
    /// Game loads but doesn't reach gameplay
    /// </summary>
    Loads,
    
    /// <summary>
    /// Game reaches gameplay but has significant issues
    /// </summary>
    Gameplay,
    
    /// <summary>
    /// Game is fully playable with minor issues
    /// </summary>
    Playable
}