﻿using System.Reflection;
using System.Text.Json.Serialization;
using System.Windows;

namespace XeniaManager.Core.Settings;

/// <summary>
/// Main settings class
/// </summary>
public class ApplicationSettings() : AbstractSettings<ApplicationSettings.ApplicationSettingsStore>("config.json")
{
    public class ApplicationSettingsStore
    {
        // Settings
        /// <summary>
        /// Settings related to the UI.
        /// </summary>
        [JsonPropertyName("ui")]
        public UiSettings Ui { get; set; } = new UiSettings();

        /// <summary>
        /// Settings related to the emulator versions
        /// </summary>
        [JsonPropertyName("emulators")]
        public EmulatorSettings Emulator { get; set; } = new EmulatorSettings();
        
        // Functions
        /// <summary>
        /// Gets the current application version as a string
        /// </summary>
        public string GetCurrentVersion()
        {
            try
            {
                Assembly assembly = Assembly.GetEntryAssembly();
                Version version = assembly?.GetName().Version;

                if (version == null)
                {
                    return "0.0.0";
                }

                // Get first three components, using 0 for missing parts
                int build = version.Build >= 0 ? version.Build : 0;
                return $"{version.Major}.{version.Minor}.{version.Revision}";
            }
            catch
            {
                return "0.0.0";
            }
        }
    }
}

/// <summary>
/// Subsection for Window Properties like size and position
/// </summary>
public class WindowProperties
{
    [JsonPropertyName("top")] public double Top { get; set; } = 0;

    [JsonPropertyName("left")] public double Left { get; set; } = 0;

    [JsonPropertyName("width")] public double Width { get; set; } = 885;

    [JsonPropertyName("height")] public double Height { get; set; } = 720;

    [JsonPropertyName("state")] public WindowState State { get; set; } = WindowState.Normal;
}

/// <summary>
/// Subsection for UI settings
/// </summary>
public class UiSettings
{
    /// <summary>
    /// <para>Language used by Xenia Manager UI</para>
    /// Default Language = English
    /// </summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";

    /// <summary>
    /// <para>Theme used by Xenia Manager UI</para>
    /// Default Theme = Light
    /// </summary>
    [JsonPropertyName("theme")]
    public Theme Theme { get; set; } = Theme.Light;

    [JsonPropertyName("window")] public WindowProperties Window { get; set; } = new WindowProperties();
}

/// <summary>
/// Information about emulator
/// </summary>
public class EmulatorInfo
{
    [JsonPropertyName("version")] public string? Version { get; set; }

    [JsonPropertyName("nightly_version")] public string? NightlyVersion { get; set; }

    [JsonPropertyName("release_date")] public string? ReleaseDate { get; set; }

    [JsonPropertyName("emulator_location")]
    public string? EmulatorLocation { get; set; }

    [JsonPropertyName("executable_location")]
    public string? ExecutableLocation { get; set; }

    [JsonPropertyName("configuration_location")]
    public string? ConfigLocation { get; set; }
}

/// <summary>
/// Subsection for Emulator Settings
/// </summary>
public class EmulatorSettings
{
    [JsonPropertyName("canary")] public EmulatorInfo? Canary { get; set; }
}