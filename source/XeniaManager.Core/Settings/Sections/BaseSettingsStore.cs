using System.Text.Json.Serialization;

namespace XeniaManager.Core.Settings.Sections;

public abstract class BaseSettingsStore
{
    /// <summary>
    /// Settings related to debugging and logging
    /// </summary>
    [JsonPropertyName("debug")]
    public DebugSettings Debug { get; set; } = new DebugSettings();

    /// <summary>
    /// Settings related to the user interface
    /// </summary>
    [JsonPropertyName("ui")]
    public UiSettings Ui { get; set; } = new UiSettings();

    /// <summary>
    /// Settings related to general options
    /// </summary>
    [JsonPropertyName("general")]
    public GeneralSettings General { get; set; } = new GeneralSettings();

    /// <summary>
    /// Settings related to the emulator versions
    /// </summary>
    [JsonPropertyName("emulators")]
    public EmulatorSettings Emulator { get; set; } = new EmulatorSettings();

    [JsonPropertyName("update_checks")]
    public UpdateCheckSettings UpdateChecks { get; set; } = new UpdateCheckSettings();
}