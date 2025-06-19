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

        [JsonPropertyName("notifications")]
        public NotificationSettings Notification { get; set; } = new NotificationSettings();

        /// <summary>
        /// Settings related to the emulator versions
        /// </summary>
        [JsonPropertyName("emulators")]
        public EmulatorSettings Emulator { get; set; } = new EmulatorSettings();

        [JsonPropertyName("update_checks")]
        public UpdateCheckSettings UpdateCheckChecks { get; set; } = new UpdateCheckSettings();

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
        /// Gets the current application informational version including commit SHA
        /// </summary>
        public string GetInformationalVersion()
        {
            try
            {
                Assembly assembly = Assembly.GetEntryAssembly();
                var informationalVersionAttribute = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        
                if (informationalVersionAttribute?.InformationalVersion != null)
                {
                    return informationalVersionAttribute.InformationalVersion.Split('+')[0];
                }

                // Fallback to regular version if informational version is not available
                return GetCurrentVersion();
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
            if (CacheCleared || !Directory.Exists(Constants.DirectoryPaths.Cache))
            {
                return;
            }
            CacheCleared = true;

            Logger.Debug($"Clearing cached artwork");
            foreach (string filePath in Directory.GetFiles(Constants.DirectoryPaths.Cache, "*", SearchOption.AllDirectories))
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

        public bool IsXeniaInstalled(XeniaVersion version)
        {
            return version switch
            {
                XeniaVersion.Canary => this.Emulator.Canary != null,
                XeniaVersion.Mousehook => this.Emulator.Mousehook != null,
                XeniaVersion.Netplay => this.Emulator.Netplay != null,
                _ => false
            };
        }

        /// <summary>
        /// Gets all the installed versions of Xenia
        /// </summary>
        /// <returns></returns>
        public List<XeniaVersion> GetInstalledVersions() =>
            new[]
                {
                    (IsInstalled: this.Emulator.Canary != null, Version: XeniaVersion.Canary),
                    (IsInstalled: this.Emulator.Mousehook != null, Version: XeniaVersion.Mousehook),
                    (IsInstalled: this.Emulator.Netplay != null, Version: XeniaVersion.Netplay)
                }
                .Where(item => item.IsInstalled)
                .Select(item => item.Version)
                .ToList();
    }
}