using System.Text.Json.Serialization;
using XeniaManager.Database.Converters;

namespace XeniaManager.Database.Models.Game;

/// <summary>
/// Netplay status breakdown for a game across different connection modes
/// </summary>
public class NetplayStatus
{
    /// <summary>
    /// Public online matchmaking status
    /// </summary>
    [JsonPropertyName("working_public")]
    [JsonConverter(typeof(NetplayStatusValueConverter))]
    public NetplayStatusValue WorkingPublic { get; set; }

    /// <summary>
    /// Locally tested status
    /// </summary>
    [JsonPropertyName("tested_locally")]
    [JsonConverter(typeof(NetplayStatusValueConverter))]
    public NetplayStatusValue TestedLocally { get; set; }

    /// <summary>
    /// Local-only (no internet) status
    /// </summary>
    [JsonPropertyName("only_local")]
    [JsonConverter(typeof(NetplayStatusValueConverter))]
    public NetplayStatusValue OnlyLocal { get; set; }

    /// <summary>
    /// System link (LAN) status
    /// </summary>
    [JsonPropertyName("systemlink")]
    [JsonConverter(typeof(NetplayStatusValueConverter))]
    public NetplayStatusValue Systemlink { get; set; }
}