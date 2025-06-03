using System.Text.Json.Serialization;

namespace XeniaManager.Core.Settings;

/// <summary>
/// Subsection for update checks
/// </summary>
public class UpdateCheckSettings
{
    [JsonPropertyName("game_compatibility")]
    public DateTime CompatibilityCheck { get; set; } = DateTime.Now;
}