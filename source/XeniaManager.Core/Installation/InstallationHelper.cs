using System.Diagnostics;
using Microsoft.Win32;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;

namespace XeniaManager.Core.Installation;

/// <summary>
/// Provides utility methods for installing and configuring Xenia emulator
/// </summary>
public class InstallationHelper
{
    /// <summary>
    /// Registry path where Xenia stores its configuration flags
    /// </summary>
    private const string REGISTRY_PATH = @"Software\Xenia";

    /// <summary>
    /// Name of the registry value used to suppress first-launch popups
    /// </summary>
    private const string REGISTRY_VALUE_NAME = "XEFLAGS";

    /// <summary>
    /// Value to set in the registry to suppress first-launch popups
    /// </summary>
    private const long REGISTRY_VALUE_DATA = 1;

    /// <summary>
    /// Sets up the Windows registry key to suppress the first-launch popup in Xenia
    /// This method creates or updates the XEFLAGS registry value to prevent Xenia from showing
    /// its initial configuration dialog on the first launch
    /// </summary>
    /// <remarks>
    /// This method only executes on Windows systems. On other platforms, it returns immediately.
    /// The registry key is created under HKEY_CURRENT_USER\Software\Xenia
    /// </remarks>
    public static void RegistrySetup()
    {
        if (!OperatingSystem.IsWindows())
        {
            Logger.Debug<InstallationHelper>("Skipping registry setup on non-Windows platform");
            return;
        }

        // Attempt to open the registry key for writing or create it if it doesn't exist
        using RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_PATH, writable: true)
                                ?? Registry.CurrentUser.CreateSubKey(REGISTRY_PATH, writable: true);

        if (key.GetValue(REGISTRY_VALUE_NAME) == null)
        {
            // Set the registry value to suppress the first-launch popup
            key.SetValue(REGISTRY_VALUE_NAME, REGISTRY_VALUE_DATA, RegistryValueKind.QWord);
            Logger.Info<InstallationHelper>("XEFLAGS registry value created successfully.");
        }
        else
        {
            Logger.Debug<InstallationHelper>("XEFLAGS registry value already exists.");
        }
    }

    /// <summary>
    /// Configures the emulator directory for portable mode and creates the necessary subdirectories
    /// This method ensures Xenia runs in portable mode by creating a portable.txt file
    /// and establishes the required directory structure for optimal operation
    /// </summary>
    /// <param name="emulatorLocation">The directory path where the Xenia executable is located</param>
    /// <remarks>
    /// Creates the following directory structure:
    /// - portable.txt file (enables portable mode)
    /// - config/ directory (for game-specific configurations)
    /// - content/ directory (for game content storage)
    /// - patches/ directory (for game patches)
    /// </remarks>
    public static void SetupEmulatorDirectory(string emulatorLocation)
    {
        Logger.Info<InstallationHelper>($"Setting up emulator directory at: {emulatorLocation}");

        // Create portable.txt to enable portable mode for Xenia
        string portableTxtPath = Path.Combine(emulatorLocation, "portable.txt");
        if (!File.Exists(portableTxtPath))
        {
            File.Create(portableTxtPath).Dispose(); // Dispose of the file stream immediately
            Logger.Info<InstallationHelper>($"Created portable.txt file at: {portableTxtPath}");
        }
        else
        {
            Logger.Debug<InstallationHelper>($"Portable.txt file already exists at: {portableTxtPath}");
        }

        // Create the config directory for storing game-specific configuration files
        string configDirPath = Path.Combine(emulatorLocation, "config");
        Directory.CreateDirectory(configDirPath);
        Logger.Info<InstallationHelper>($"Ensured config directory exists at: {configDirPath}");

        // Create the content directory for storing game-specific content files
        string contentDirPath = Path.Combine(emulatorLocation, "content");
        Directory.CreateDirectory(contentDirPath);
        Logger.Info<InstallationHelper>($"Ensured content directory exists at: {contentDirPath}");

        // Create the patches directory for storing game patches
        string patchesDirPath = Path.Combine(emulatorLocation, "patches");
        Directory.CreateDirectory(patchesDirPath);
        Logger.Info<InstallationHelper>($"Ensured patches directory exists at: {patchesDirPath}");

        Logger.Info<InstallationHelper>($"Emulator directory setup completed at: {emulatorLocation}");
    }
}