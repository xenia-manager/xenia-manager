using XeniaManager.Core.Constants;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Utilities.Paths;

namespace XeniaManager.Core.Manage;

/// <summary>
/// Manages configuration files for different Xenia emulator versions
/// Handles copying, updating, and maintaining configuration files for various Xenia versions
/// </summary>
public class ConfigManager
{
    /// <summary>
    /// Dictionary mapping Xenia versions to their respective configuration file paths
    /// Each entry contains a tuple with:
    /// - DefaultConfigLocation: Where the configuration file needs to be for Xenia to load it
    /// - ConfigLocation: The stock/default Xenia configuration file location
    /// </summary>
    private static readonly Dictionary<XeniaVersion, (string DefaultConfigLocation, string ConfigLocation)> _configLocations = new Dictionary<XeniaVersion, (string DefaultConfigLocation, string ConfigLocation)>
    {
        {
            XeniaVersion.Canary,
            (AppPathResolver.GetFullPath(XeniaPaths.Canary.DefaultConfigLocation), AppPathResolver.GetFullPath(XeniaPaths.Canary.ConfigLocation))
        },
        {
            XeniaVersion.Mousehook,
            (AppPathResolver.GetFullPath(XeniaPaths.Mousehook.DefaultConfigLocation), AppPathResolver.GetFullPath(XeniaPaths.Mousehook.ConfigLocation))
        },
        {
            XeniaVersion.Netplay,
            (AppPathResolver.GetFullPath(XeniaPaths.Netplay.DefaultConfigLocation), AppPathResolver.GetFullPath(XeniaPaths.Netplay.ConfigLocation))
        }
    };

    /// <summary>
    /// Changes the configuration file for a specific Xenia version
    /// This method copies the provided configuration file to the appropriate location for the specified Xenia version
    /// It ensures the configuration file exists and handles the copying process with proper error handling
    /// </summary>
    /// <param name="configurationFile">The path to the source configuration file to be applied</param>
    /// <param name="xeniaVersion">The Xenia version for which to apply the configuration</param>
    /// <returns>True if the configuration file was successfully changed, false otherwise</returns>
    /// <exception cref="NotImplementedException">Thrown when the specified Xenia version is not supported</exception>
    /// <exception cref="Exception">Thrown when an error occurs during the file operations</exception>
    public static bool ChangeConfigurationFile(string configurationFile, XeniaVersion xeniaVersion)
    {
        // Attempt to retrieve the configuration paths for the specified Xenia version
        if (!_configLocations.TryGetValue(xeniaVersion, out (string DefaultConfigLocation, string ConfigLocation) configPaths))
        {
            // Throw exception if the specified Xenia version is not supported
            throw new NotImplementedException($"Unsupported emulator version: {xeniaVersion}");
        }

        try
        {
            // Delete the existing default configuration file if it exists
            if (File.Exists(configPaths.DefaultConfigLocation))
            {
                File.Delete(configPaths.DefaultConfigLocation);
            }

            // Ensure the source configuration file exists, create it from default if missing
            if (!File.Exists(configurationFile))
            {
                Logger.Warning<ConfigManager>($"Configuration file '{configurationFile}' is missing. Creating a new one from default.");
                File.Copy(configPaths.ConfigLocation, configurationFile);
            }

            // Copy the source configuration file to the default configuration location for Xenia
            File.Copy(configurationFile, configPaths.DefaultConfigLocation, true);
            return true;
        }
        catch (Exception ex)
        {
            // Wrap and re-throw any exceptions that occur during file operations
            throw new Exception($"{ex.Message}\n{ex}");
        }
    }
}