using System.Reflection;
using Avalonia;
using Avalonia.Controls;
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

    /// <summary>
    /// Restores the window properties (position, size, and state) from the saved settings
    /// </summary>
    /// <param name="settings">The settings object containing the saved window properties</param>
    /// <param name="window">The window to apply the restored properties to</param>
    public void RestoreWindowProperties(Settings settings, Window window)
    {
        window.Position = new PixelPoint(settings.Settings.Ui.Window.Position.X, settings.Settings.Ui.Window.Position.Y);
        window.Width = settings.Settings.Ui.Window.Width;
        window.Height = settings.Settings.Ui.Window.Height;
        window.WindowState = settings.Settings.Ui.Window.State;
    }
    
    /// <summary>
    /// Saves the current window properties (position, size, and state) to the settings
    /// </summary>
    /// <param name="settings">The settings object to save the window properties to</param>
    /// <param name="window">The window whose properties are to be saved</param>
    public void SaveWindowProperties(Settings settings, Window window)
    {
        settings.Settings.Ui.Window.Position.X = window.Position.X;
        settings.Settings.Ui.Window.Position.Y = window.Position.Y;
        settings.Settings.Ui.Window.Width = window.Width;
        settings.Settings.Ui.Window.Height = window.Height;
        settings.Settings.Ui.Window.State = window.WindowState;
        settings.SaveSettings();
    }
}