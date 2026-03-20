using System.Text.Json.Serialization;

namespace XeniaManager.Core.Settings.Sections;

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

    [JsonPropertyName("manager_update_available")]
    public bool ManagerUpdateAvailable { get; set; } = false;

    [JsonPropertyName("last_manager_update_check")]
    public DateTime LastManagerUpdateCheck { get; set; } = DateTime.Now;

    [JsonPropertyName("check_for_updates_on_startup")]
    public bool CheckForUpdatesOnStartup { get; set; } = true;
}