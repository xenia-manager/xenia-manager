using System.Text.Json.Serialization;

namespace XeniaManager.Core.Settings.Sections;

/// <summary>
/// Subsection for general settings
/// </summary>
public class GeneralSettings
{
    /// <summary>
    /// Whether to parse game details with Xenia emulator
    /// </summary>
    [JsonPropertyName("parse_game_details_with_xenia")]
    public bool ParseGameDetailsWithXenia { get; set; } = true;
}
