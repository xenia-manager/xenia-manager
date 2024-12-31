// Imported
using Newtonsoft.Json;

namespace XeniaManager
{
    /// <summary>
    /// Xenia Manager settings
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// <para>This stores the location of Xenia VFS Dump</para>
        /// </summary>
        [JsonProperty("vfs_dump_tool_location")]
        public string? VfsDumpToolLocation { get; set; }

        /// <summary>
        /// <para>Selected theme for Xenia Manager</para>
        /// </summary>
        private string selectedTheme;

        [JsonProperty("selected_theme")]
        public string SelectedTheme
        {
            get => selectedTheme ?? "Light";
            set => selectedTheme = value;
        }

        /// <summary>
        /// <para>Stores selected option for FullscreenMode for Xenia Manager</para>
        /// </summary>
        [JsonProperty("fullscreen_mode")]
        public bool FullscreenMode { get; set; } = false; // Default to "false" if null

        /// <summary>
        /// <para>Stores selected option for parsing game details without Xenia</para>
        /// </summary>
        [JsonProperty("auto_game_parsing_selection")]
        public bool? AutomaticGameParsingSelection { get; set; } = false; // Default to "false" if null

        /// <summary>
        /// <para>Stores the selected option for automatically backing up saves in Xenia Manager</para>
        /// </summary>
        [JsonProperty("auto_save_backup")]
        public bool? AutomaticSaveBackup { get; set; } = false; // Default to "false" if null

        /// <summary>
        /// Stores the option for enabling/disabling compatibility icons
        /// </summary>
        [JsonProperty("compatibility_icons")] 
        public bool? CompatibilityIcons { get; set; } = true; // Default to "true" if null

        /// <summary>
        /// <para>Stores the selected option for what profile slot the user wants to have automatically backing up saves done in Xenia Manager</para>
        /// </summary>
        [JsonProperty("profile_slot")]
        public int ProfileSlot { get; set; } = 1; // Default to Slot 0 if null

        /// <summary>
        /// Information about currently installed Xenia Manager
        /// </summary>
        [JsonProperty("manager_info")]
        public UpdateInfo? Manager { get; set; }

        /// <summary>
        /// Information about currently installed Xenia Canary
        /// </summary>
        [JsonProperty("xenia_canary_info")]
        public EmulatorInfo? XeniaCanary { get; set; }

        /// <summary>
        /// Information about currently installed Xenia Netplay
        /// </summary>
        [JsonProperty("xenia_netplay_info")]
        public EmulatorInfo? XeniaNetplay { get; set; }

        /// <summary>
        /// Information about currently installed Xenia Netplay
        /// </summary>
        [JsonProperty("xenia_mousehook_info")]
        public EmulatorInfo? XeniaMousehook { get; set; }

        /// <summary>
        /// Checks if any of Xenia versions are installed
        /// </summary>
        /// <returns>Returns true if there are any Xenia Versions installed, otherwise returns false</returns>
        public bool IsXeniaInstalled()
        {
            return XeniaCanary != null || XeniaNetplay != null || XeniaMousehook != null;
        }
    }

    /// <summary>
    /// Information about currently installed Xenia Manager
    /// </summary>
    public class UpdateInfo
    {
        /// <summary>
        /// <para>"id" property from this JSON file</para>
        /// <para>Used to update the emulator</para>
        /// </summary>
        [JsonProperty("version")]
        public string? Version { get; set; }

        /// <summary>
        /// <para>"id" property from this JSON file</para>
        /// <para>Used to update the emulator</para>
        /// </summary>
        [JsonProperty("update_available")]
        public bool? UpdateAvailable { get; set; } = false; // Default to "false" if null

        /// <summary>
        /// <para>Date of publishing of the installed build</para>
        /// </summary>
        [JsonProperty("release_date")]
        public DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// <para>Date when the last check for updates was</para>
        /// </summary>
        [JsonProperty("last_update_check_date")]
        public DateTime LastUpdateCheckDate { get; set; } = DateTime.Now; // Defaults to the current date if null

        /// <summary>
        /// <para>Date when the last check for compatibility rating was</para>
        /// </summary>
        [JsonProperty("last_compatibility_rating_update_check_date")]
        public DateTime? LastCompatiblityRatingUpdateCheckDate { get; set; }
    }

    /// <summary>
    /// Information about installed emulator
    /// </summary>
    public class EmulatorInfo
    {
        /// <summary>
        /// <para>This stores the location where the emulator is installed</para>
        /// </summary>
        [JsonProperty("emulator_location")]
        public string? EmulatorLocation { get; set; }

        /// <summary>
        /// <para>This stores the location where the emulator executable is</para>
        /// </summary>
        [JsonProperty("executable_location")]
        public string? ExecutableLocation { get; set; }

        /// <summary>
        /// <para>This stores the location where the emulator configuration file is</para>
        /// </summary>
        [JsonProperty("configurationfile_location")]
        public string? ConfigurationFileLocation { get; set; }

        /// <summary>
        /// <para>"id" property from this JSON file</para>
        /// <para>Used to update the emulator</para>
        /// </summary>
        [JsonProperty("version")]
        public string? Version { get; set; }

        /// <summary>
        /// <para>Date of publishing of the installed build</para>
        /// </summary>
        [JsonProperty("release_date")]
        public DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// <para>Date when the last check for updates was</para>
        /// </summary>
        [JsonProperty("last_update_check_date")]
        public DateTime? LastUpdateCheckDate { get; set; }
    }
}