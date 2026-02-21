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
}