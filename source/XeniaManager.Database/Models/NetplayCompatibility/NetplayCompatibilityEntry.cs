using System.Text.Json.Serialization;
using XeniaManager.Core.Converters;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.Database.Models.NetplayCompatibility;

/// <summary>
/// Represents a single game entry in the netplay compatibility database.
/// Contains information about a game's netplay support status.
/// </summary>
public class NetplayCompatibilityEntry
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
    /// Detailed netplay status across different connection modes
    /// </summary>
    [JsonPropertyName("status")]
    public NetplayStatus Status { get; set; } = new NetplayStatus();

    /// <summary>
    /// Comments about netplay support
    /// </summary>
    [JsonPropertyName("comments")]
    public string? Comments { get; set; }

    /// <summary>
    /// Links related to netplay support (not stored per-game, only used for database display)
    /// </summary>
    [JsonPropertyName("links")]
    public List<NetplayLink>? Links { get; set; }
}

/// <summary>
/// Represents a link related to netplay support
/// </summary>
public class NetplayLink
{
    /// <summary>
    /// Display text for the link
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    /// <summary>
    /// URL of the link
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}