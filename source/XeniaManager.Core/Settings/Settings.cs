using System.Reflection;
using XeniaManager.Core.Settings.Sections;

namespace XeniaManager.Core.Settings;

/// <summary>
/// Manages Xenia Manager-specific application settings with loading, saving, and access functionality.
/// Includes methods for updating installed mod versions.
/// </summary>
public class Settings : AbstractSettings<Settings.SettingsStore>
{
    /// <summary>
    /// Represents the store for Xenia Manager-specific settings with common base properties.
    /// </summary>
    public class SettingsStore : BaseSettingsStore;

    /// <summary>
    /// Gets the version of the application from the assembly information. Uses InformationalVersionAttribute for experimental builds
    /// </summary>
    /// <returns>The application version in major.minor.build format, or "0.0.0" if version information is unavailable.</returns>
    public string GetVersion()
    {
        try
        {
            Assembly? assembly = Assembly.GetEntryAssembly();
            if (Settings.UpdateChecks.UseExperimentalBuild)
            {
                // Return informational version for experimental builds
                AssemblyInformationalVersionAttribute? informationalVersionAttribute = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                if (informationalVersionAttribute?.InformationalVersion != null)
                {
                    return informationalVersionAttribute.InformationalVersion.Split('+')[0];
                }
            }

            // Assembly version for stable builds
            Version? version = assembly?.GetName().Version;
            return version == null ? "0.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";
        }
        catch
        {
            return "0.0.0";
        }
    }
}