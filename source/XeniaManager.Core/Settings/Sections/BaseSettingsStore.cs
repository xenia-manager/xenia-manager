using System.Text.Json.Serialization;

namespace XeniaManager.Core.Settings.Sections;

public abstract class BaseSettingsStore
{
    [JsonPropertyName("ui")]
    public UiSettings Ui { get; set; } = new UiSettings();

    /// <summary>
    /// Settings related to the emulator versions
    /// </summary>
    [JsonPropertyName("emulators")]
    public EmulatorSettings Emulator { get; set; } = new EmulatorSettings();

    [JsonPropertyName("update_checks")]
    public UpdateCheckSettings UpdateChecks { get; set; } = new UpdateCheckSettings();
}