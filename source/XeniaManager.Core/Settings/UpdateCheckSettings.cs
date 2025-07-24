using System.Text.Json.Serialization;

namespace XeniaManager.Core.Settings;

/// <summary>
/// Subsection for update checks
/// </summary>
public class UpdateCheckSettings
{
    [JsonPropertyName("experimental_build")]
#if EXPERIMENTAL_BUILD
    public bool UseExperimentalBuild { get; set; } = true;
#else
    public bool UseExperimentalBuild { get; set; } = false;
#endif

    [JsonPropertyName("last_manager_update_check")]
    public DateTime LastManagerUpdateCheck { get; set; } = DateTime.Now;
    
    [JsonPropertyName("game_compatibility")]
    public DateTime CompatibilityCheck { get; set; } = DateTime.Now;
}