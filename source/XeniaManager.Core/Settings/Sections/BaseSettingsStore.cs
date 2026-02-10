using System.Text.Json.Serialization;

namespace XeniaManager.Core.Settings.Sections;

public abstract class BaseSettingsStore
{
    [JsonPropertyName("ui")]
    public UiSettings Ui { get; set; } = new UiSettings();

    [JsonPropertyName("update_checks")]
    public UpdateCheckSettings UpdateChecks { get; set; } = new UpdateCheckSettings();
}