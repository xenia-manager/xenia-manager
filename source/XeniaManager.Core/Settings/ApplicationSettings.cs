using System.Reflection;
using System.Text.Json.Serialization;

// Imported Libraries
using XeniaManager.Core.Enum;
using XeniaManager.Core.Mousehook;

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
        public UpdateCheckSettings UpdateChecks { get; set; } = new UpdateCheckSettings();

        /// <summary>
        /// Checks if the cache has been cleared
        /// </summary>
        private bool CacheCleared { get; set; } = false;

        [JsonIgnore]
        public List<GameKeyMapping> MousehookBindings { get; set; }

        // Functions
        /// <summary>
        /// Gets the current application version based on experimental build setting
        /// </summary>
        /// <returns>Informational version if using experimental build, otherwise assembly version</returns>
        public string GetManagerVersion()
        {
            try
            {
                Assembly? assembly = Assembly.GetExecutingAssembly();

                if (UpdateChecks.UseExperimentalBuild)
                {
                    // Return informational version for experimental builds
                    var informationalVersionAttribute = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                    if (informationalVersionAttribute?.InformationalVersion != null)
                    {
                        return informationalVersionAttribute.InformationalVersion.Split('+')[0];
                    }
                }

                // Return assembly version for stable builds or as fallback
                Version? version = assembly?.GetName().Version;
                if (version == null)
                {
                    return "0.0.0";
                }

                // Get first three components, using 0 for missing parts
                int build = version.Build >= 0 ? version.Build : 0;
                return $"{version.Major}.{version.Minor}.{version.Build}";
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

        public XeniaVersion SelectVersion(Func<XeniaVersion?> showSelectionDialog)
        {
            List<XeniaVersion> installedVersions = GetInstalledVersions();
            if (installedVersions == null || installedVersions.Count == 0)
            {
                throw new InvalidOperationException("No Xenia version installed. Install Xenia before continuing.");
            }
            ;

            if (installedVersions.Count == 1)
            {
                return installedVersions[0];
            }

            var selected = showSelectionDialog?.Invoke();
            if (selected != null)
            {
                return selected.Value;
            }

            throw new OperationCanceledException("Xenia version selection was canceled by the user.");
        }
    }
}