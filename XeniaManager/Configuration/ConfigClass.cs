using System;

// Imported
using Newtonsoft.Json;

namespace XeniaManager
{
    /// <summary>
    /// Xenia Manager settings
    /// </summary>
    public class Config
    {
        /// <summary>
        /// <para>This stores the location of Xenia VFS Dump</para>
        /// </summary>
        [JsonProperty("vfs_dump_tool_location")]
        public string? VFSDumpToolLocation { get; set; }

        /// <summary>
        /// <para>Selected theme for Xenia Manager</para>
        /// </summary>
        private string _selectedTheme;
        [JsonProperty("selected_theme")]
        public string SelectedTheme 
        { 
            get => _selectedTheme ?? "Light"; 
            set => _selectedTheme = value; 
        }

        /// <summary>
        /// <para>Stores selected option for FullscreenMode for Xenia Manager</para>
        /// </summary>
        [JsonProperty("fullscreen_mode")]
        public bool FullscreenMode { get; set; } = false; // Default to "false" if null

        /// <summary>
        /// <para>Stores selected option for automatically adding games in Xenia Manager</para>
        /// </summary>
        [JsonProperty("auto_game_adding")]
        public bool? AutoGameAdding { get; set; } = false; // Default to "false" if null

        /// <summary>
        /// Information about currently installed Xenia Manager
        /// </summary>
        [JsonProperty("manager_info")]
        public UpdateInfo? Manager { get; set; }

        /// <summary>
        /// Information about currently installed Xenia Stable
        /// </summary>
        [JsonProperty("xenia_stable_info")]
        public EmulatorInfo? XeniaStable { get; set; }

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
        public bool? UpdateAvailable { get; set; }

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
