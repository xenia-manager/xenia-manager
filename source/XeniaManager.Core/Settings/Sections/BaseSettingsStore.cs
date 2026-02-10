using System.Text.Json.Serialization;

namespace XeniaManager.Core.Settings.Sections;

public abstract class BaseSettingsStore
{
    [JsonPropertyName("update_checks")]
    public UpdateCheckSettings UpdateChecks { get; set; } = new UpdateCheckSettings();
}