using System.Text.Json.Serialization;

namespace XeniaManager.Core.Settings;

/// <summary>
/// Information about emulator
/// </summary>
public class EmulatorInfo
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("nightly_version")]
    public string? NightlyVersion { get; set; } = "v0.0.0";

    [JsonPropertyName("use_nightly_build")]
    public bool UseNightlyBuild { get; set; } = false;

    [JsonIgnore]
    public string? CurrentVersion => UseNightlyBuild ? NightlyVersion : Version;

    public void SetCurrentVersion(string? version)
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

    [JsonPropertyName("release_date")]
    public DateTime? ReleaseDate { get; set; }

    [JsonPropertyName("last_update_check_date")]
    public DateTime LastUpdateCheckDate { get; set; } = DateTime.Now;

    [JsonPropertyName("update_available")]
    public bool UpdateAvailable { get; set; } = false;

    [JsonPropertyName("emulator_location")]
    public string? EmulatorLocation { get; set; }

    [JsonPropertyName("executable_location")]
    public string? ExecutableLocation { get; set; }

    [JsonPropertyName("configuration_location")]
    public string? ConfigLocation { get; set; }
}

public class GeneralEmulatorSettings
{
    [JsonPropertyName("profile_settings")]
    public ProfileSettings Profile { get; set; } = new ProfileSettings();

    [JsonPropertyName("unified_content")]
    public bool UnifiedContentFolder { get; set; } = false;

    [JsonPropertyName("auto_update")]
    public bool AutomaticallyUpdateEmulator { get; set; } = false;

    public class ProfileSettings
    {
        [JsonPropertyName("automatic_save_backup")]
        public bool AutomaticSaveBackup { get; set; } = false;

        [JsonPropertyName("profile_slot")]
        public string ProfileSlot { get; set; } = "0";
    }
}

/// <summary>
/// Subsection for Emulator Settings
/// </summary>
public class EmulatorSettings
{
    [JsonPropertyName("settings")]
    public GeneralEmulatorSettings Settings { get; set; } = new GeneralEmulatorSettings();

    [JsonPropertyName("canary")]
    public EmulatorInfo? Canary { get; set; }

    [JsonPropertyName("mousehook")]
    public EmulatorInfo? Mousehook { get; set; }

    [JsonPropertyName("netplay")]
    public EmulatorInfo? Netplay { get; set; }
}