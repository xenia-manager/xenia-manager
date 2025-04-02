using System.Reflection;
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

        /// <summary>
        /// Checks if the cache has been cleared
        /// </summary>
        private bool CacheCleared { get; set; } = false;
        
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

        /// <summary>
        /// Deletes all the cached artwork that is not in use
        /// </summary>
        public void ClearCache()
        {
            // Only do this once per app run
            if (CacheCleared)
            {
                return;
            }
            CacheCleared = true;
            
            Logger.Debug($"Clearing cached artwork");
            foreach (string filePath in Directory.GetFiles(Constants.CacheDir, "*", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException)
                {
                    Logger.Warning($"{Path.GetFileName(filePath)} won't get deleted since it's currently in use");
                }
                catch (Exception ex)
                {
                    Logger.Error($"{ex}\nFull Error:\n{ex.StackTrace}");
                    break;
                }
            }
        }
    }
}

/// <summary>
/// Subsection for Window Properties like size and position
/// </summary>
public class WindowProperties
{
    [JsonPropertyName("top")] 
    public double Top { get; set; } = 0;

    [JsonPropertyName("left")] 
    public double Left { get; set; } = 0;

    [JsonPropertyName("width")] 
    public double Width { get; set; } = 885;

    [JsonPropertyName("height")] 
    public double Height { get; set; } = 720;

    [JsonPropertyName("state")] 
    public WindowState State { get; set; } = WindowState.Normal;
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

    [JsonPropertyName("window")] 
    public WindowProperties Window { get; set; } = new WindowProperties();
}

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

/// <summary>
/// Subsection for Emulator Settings
/// </summary>
public class EmulatorSettings
{
    [JsonPropertyName("canary")] 
    public EmulatorInfo? Canary { get; set; }
    
    [JsonPropertyName("mousehook")] 
    public EmulatorInfo? Mousehook { get; set; }
    
    [JsonPropertyName("netplay")] 
    public EmulatorInfo? Netplay { get; set; }
}