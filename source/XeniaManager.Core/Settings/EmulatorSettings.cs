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
    public string? NightlyVersion { get; set; }

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

    public class ProfileSettings
    {
        [JsonPropertyName("automatic_save_backup")]
        public bool AutomaticSaveBackup { get; set; } = false;

        [JsonPropertyName("profile_slot")]
        public int ProfileSlot { get; set; } = 0;
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