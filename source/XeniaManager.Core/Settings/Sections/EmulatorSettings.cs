using System.Text.Json.Serialization;

namespace XeniaManager.Core.Settings.Sections;

/// <summary>
/// Represents the settings configuration for the Xenia emulator
/// </summary>
public class EmulatorSettings
{
    /// <summary>
    /// Gets or sets information about the Canary build of the emulator
    /// </summary>
    [JsonPropertyName("canary")]
    public EmulatorInfo? Canary { get; set; }
}

/// <summary>
/// Contains detailed information and configuration settings for a Xenia emulator instance
/// </summary>
public class EmulatorInfo
{
    /// <summary>
    /// Gets or sets the stable version of the emulator
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "v0.0.0";

    /// <summary>
    /// Gets or sets the nightly version of the emulator
    /// </summary>
    [JsonPropertyName("nightly_version")]
    public string NightlyVersion { get; set; } = "v0.0.0";

    /// <summary>
    /// Gets or sets a value indicating whether to use the nightly build of the emulator
    /// </summary>
    [JsonPropertyName("use_nightly_build")]
    public bool UseNightlyBuild { get; set; } = false;

    /// <summary>
    /// Gets the current version of the emulator based on whether nightly builds are enabled
    /// </summary>
    [JsonIgnore]
    public string CurrentVersion => UseNightlyBuild ? NightlyVersion : Version;

    /// <summary>
    /// Sets the current version of the emulator based on whether nightly builds are enabled
    /// </summary>
    /// <param name="version">The version string to set</param>
    public void SetCurrentVersion(string version)
    {
        if (UseNightlyBuild)
        {
            NightlyVersion = version;
        }
        else
        {
            Version = version;
        }
    }

    /// <summary>
    /// Gets or sets the date when the last update check was performed
    /// </summary>
    [JsonPropertyName("last_update_check_date")]
    public DateTime LastUpdateCheckDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Gets or sets a value indicating whether an update is available for the emulator
    /// </summary>
    [JsonPropertyName("update_available")]
    public bool UpdateAvailable { get; set; } = false;

    /// <summary>
    /// Gets or sets the file path location of the emulator installation
    /// </summary>
    [JsonPropertyName("emulator_location")]
    public string? EmulatorLocation { get; set; }

    /// <summary>
    /// Gets or sets the file path location of the emulator executable
    /// </summary>
    [JsonPropertyName("executable_location")]
    public string? ExecutableLocation { get; set; }

    /// <summary>
    /// Gets or sets the file path location of the emulator configuration file
    /// </summary>
    [JsonPropertyName("configuration_location")]
    public string? ConfigLocation { get; set; }
}