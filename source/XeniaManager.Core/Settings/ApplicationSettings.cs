using System.Text.Json.Serialization;

namespace XeniaManager.Core.Settings;

/// <summary>
/// Main settings class
/// </summary>
public class ApplicationSettings : AbstractSettings<ApplicationSettings.ApplicationSettingsStore>
{
    public class ApplicationSettingsStore
    {
        /// <summary>
        /// Settings related to the UI.
        /// </summary>
        [JsonPropertyName("UI")]
        public UiSettings UI { get; set; } = new UiSettings();
    }

    public ApplicationSettings() : base("config.json") { }
}

/// <summary>
/// Sub sections for settings
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
}