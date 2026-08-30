using System.Text.Json.Serialization;

namespace XeniaManager.Core.Models.Game;

/// <summary>
/// Netplay compatibility information for a game
/// </summary>
public class NetplayCompatibility
{
    /// <summary>
    /// Detailed netplay status across different connection modes
    /// </summary>
    [JsonPropertyName("status")]
    public NetplayStatus Status { get; set; } = new NetplayStatus();

    /// <summary>
    /// Comments about netplay support for this game
    /// </summary>
    [JsonPropertyName("comments")]
    public string Comments { get; set; } = string.Empty;
}