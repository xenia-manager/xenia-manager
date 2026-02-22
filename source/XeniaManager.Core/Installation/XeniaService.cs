using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Settings.Sections;
using XeniaManager.Core.Utilities;
using XeniaManager.Core.Utilities.Paths;

namespace XeniaManager.Core.Installation;

/// <summary>
/// Service class responsible for setting up and configuring Xenia emulator installations
/// Handles the complete installation process including directory creation, configuration generation, and registry setup
/// </summary>
public class XeniaService
{
    /// <summary>
    /// Sets up a Xenia emulator installation with the specified version and configuration
    /// This method orchestrates the complete installation process including directory setup,
    /// configuration file generation, and registry configuration
    /// </summary>
    /// <param name="version">The specific Xenia version to install (e.g., Canary, Netplay, etc.)</param>
    /// <param name="releaseVersion">The release version string to associate with this installation</param>
    /// <param name="unifiedContentFolder">Flag indicating whether to use a unified content folder structure (currently not implemented)</param>
    /// <returns>An EmulatorInfo object containing all the necessary information about the installed emulator</returns>
    /// <exception cref="FileNotFoundException">Thrown when the generated config file cannot be found after creation</exception>
    public static EmulatorInfo SetupEmulator(XeniaVersion version, string releaseVersion, bool unifiedContentFolder = false)
    {
        // Perform the necessary registry setup for Xenia
        InstallationHelper.RegistrySetup();

        // Retrieve version-specific information for the selected Xenia version
        XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);

        // Create the emulator info object with initial configuration paths
        EmulatorInfo emulatorInfo = new EmulatorInfo
        {
            EmulatorLocation = versionInfo.EmulatorDir,
            ExecutableLocation = versionInfo.ExecutableLocation,
            ConfigLocation = versionInfo.DefaultConfigLocation,
            Version = releaseVersion
        };

        // Setup the emulator directory structure
        InstallationHelper.SetupEmulatorDirectory(AppPathResolver.GetFullPath(emulatorInfo.EmulatorLocation));

        // TODO: Unified Content File

        Logger.Info<XeniaService>($"Checking for existing profiles in content folder for Xenia version: {version}");

        // Check if there are existing profiles in the content folder
        int profileCount = AccountFile.CountProfiles(version);
        Logger.Info<XeniaService>($"Found {profileCount} existing profiles for Xenia version: {version}");

        bool shouldGenerateProfile = true; // Only generate a profile if none exist
        if (profileCount <= 0)
        {
            Logger.Info<XeniaService>($"No existing profiles found. Creating a default account profile for Xenia {version}");

            try
            {
                // Generate a default account profile
                // TODO: Replace the default gamertag with user defined username (not required)
                AccountFile.CreateAccount(version, "Canary User");
                Logger.Info<XeniaService>($"Successfully created default account profile for Xenia {version}");

                // Since we just created a profile, we don't need to generate one during config generation
                shouldGenerateProfile = false;
            }
            catch (Exception ex)
            {
                Logger.Warning<XeniaService>($"Failed to create account for Xenia {version}: {ex.Message}");
                Logger.LogExceptionDetails<XeniaService>(ex);
                Logger.Warning<XeniaService>("Falling back to manual profile generation during config generation");
                shouldGenerateProfile = true; // Try to generate profile during config generation
            }
        }
        else
        {
            Logger.Info<XeniaService>($"Profiles already exist for Xenia {version}, skipping profile creation");
            shouldGenerateProfile = false;
        }

        // Generate the initial configuration file using the executable
        Logger.Info<XeniaService>($"Generating configuration file for Xenia {version}. Generate profile during config: {shouldGenerateProfile}");
        ConfigManager.GenerateEmulatorConfigurationFile(version, shouldGenerateProfile);

        // Update the config location in the emulator info to reflect the moved file
        emulatorInfo.ConfigLocation = versionInfo.ConfigLocation;

        // Set up the working configuration file with the default emulator one
        ConfigManager.ChangeConfigurationFile(AppPathResolver.GetFullPath(emulatorInfo.ConfigLocation), version);

        return emulatorInfo;
    }

    /// <summary>
    /// Checks for updates for a specific Xenia emulator version
    /// Compares the current installed version against the latest available release
    /// </summary>
    /// <param name="releaseService">The release service used to fetch the latest build information</param>
    /// <param name="emulatorInfo">The current emulator information containing the installed version</param>
    /// <param name="releaseType">The type of release to check against (e.g., XeniaCanary, NetplayStable, etc.)</param>
    /// <param name="forceRefresh">If true, forces a refresh of the release cache before checking for updates</param>
    /// <returns>A tuple containing (isUpdateAvailable, latestVersion) - true if the update is available along with the latest version string</returns>
    public static async Task<(bool IsUpdateAvailable, string LatestVersion)> CheckForUpdatesAsync(IReleaseService releaseService, EmulatorInfo emulatorInfo,
        ReleaseType releaseType, bool forceRefresh = false)
    {
        Logger.Info<XeniaService>($"Checking for updates for release type: {releaseType}");

        try
        {
            // Force refresh if requested
            if (forceRefresh)
            {
                Logger.Info<XeniaService>("Forcing release cache refresh before checking for updates");
                await releaseService.ForceRefreshAsync();
            }

            // Fetch the latest release information
            CachedBuild? releaseBuild = await releaseService.GetCachedBuildAsync(releaseType);
            if (releaseBuild == null)
            {
                Logger.Warning<XeniaService>($"Failed to fetch release information for {releaseType}");
                return (false, string.Empty);
            }

            // Get current and latest versions
            string currentVersion = emulatorInfo.Version;
            string latestVersion = releaseBuild.TagName;

            // Check if there's a new version
            bool isUpdateAvailable = latestVersion != currentVersion;

            Logger.Info<XeniaService>(isUpdateAvailable ? $"Update available: {currentVersion} -> {latestVersion}" : "Emulator is up to date");

            return (isUpdateAvailable, latestVersion);
        }
        catch (Exception ex)
        {
            Logger.Error<XeniaService>($"Failed to check for updates: {ex.Message}");
            Logger.LogExceptionDetails<XeniaService>(ex);
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// Uninstalls a Xenia emulator installation by removing all associated files and directories
    /// This method deletes the entire emulator directory and returns null to indicate removal
    /// </summary>
    /// <param name="version">The specific Xenia version to uninstall (e.g., Canary, Netplay, etc.)</param>
    /// <returns>null to indicate that the emulator has been removed from settings</returns>
    public static EmulatorInfo? UninstallEmulator(XeniaVersion version)
    {
        Logger.Info<XeniaService>($"Starting uninstallation process for Xenia {version}");

        XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);
        Logger.Debug<XeniaService>($"Retrieved version info - Emulator directory: {versionInfo.EmulatorDir}");

        string emulatorDirectory = AppPathResolver.GetFullPath(versionInfo.EmulatorDir);

        // Check if the emulator directory exists before attempting deletion
        if (Directory.Exists(emulatorDirectory))
        {
            Logger.Info<XeniaService>($"Deleting Xenia emulator directory: {emulatorDirectory}");

            try
            {
                Directory.Delete(emulatorDirectory, true);
                Logger.Info<XeniaService>($"Successfully deleted Xenia emulator directory: {emulatorDirectory}");
            }
            catch (Exception ex)
            {
                Logger.Error<XeniaService>($"Failed to delete Xenia emulator directory: {ex.Message}");
                Logger.LogExceptionDetails<XeniaService>(ex);
                throw;
            }
        }
        else
        {
            Logger.Warning<XeniaService>($"Xenia emulator directory does not exist, skipping deletion: {emulatorDirectory}");
        }

        // Remove all games using the selected Xenia version
        Logger.Info<XeniaService>($"Removing all games using Xenia {version} from library");
        foreach (Game game in GameManager.Games.ToList().Where(game => game.XeniaVersion == version))
        {
            Logger.Info<XeniaService>($"Removing game '{game.Title}' ({game.GameId}) from library since it's using Xenia {version}");
            GameManager.RemoveGame(game);
        }

        // Log the completion of the uninstallation process
        Logger.Info<XeniaService>($"Completed uninstallation process for Xenia {version}");

        // Remove the emulator from the settings by returning null
        Logger.Debug<XeniaService>($"Returning null to indicate emulator removal from settings for Xenia {version}");
        return null;
    }
}