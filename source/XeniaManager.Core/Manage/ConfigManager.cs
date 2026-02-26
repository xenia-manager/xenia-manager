using System.Diagnostics;
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

    /// <summary>
    /// Generates the Xenia configuration file by launching the emulator and allowing it to create the default configuration
    /// <para>
    /// (TODO: LEGACY, WILL BE REMOVED) Optionally generates a user profile if requested
    /// </para>
    /// </summary>
    /// <param name="xeniaVersion">Xenia Version we're generating the configuration file</param>
    /// <param name="generateProfile">Whether to also generate a user profile (requires manual emulator closure)</param>
    /// <exception cref="FileNotFoundException">Thrown when the Xenia executable is not found at the specified location</exception>
    /// <exception cref="InvalidOperationException">Thrown when the Xenia process fails to start</exception>
    /// <exception cref="TimeoutException">Thrown when the configuration file generation takes too long</exception>
    /// <remarks>
    /// This method launches the Xenia emulator process, waits for it to generate the configuration file,
    /// and then either waits for manual closure (if generateProfile is true) or automatically closes the process.
    /// The method ensures the generated config file is at least 20KB in size to confirm it was properly created.
    /// </remarks>
    public static void GenerateEmulatorConfigurationFile(XeniaVersion xeniaVersion, bool generateProfile = false)
    {
        // Fetch XeniaVersionInfo
        XeniaVersionInfo info = XeniaVersionInfo.GetXeniaVersionInfo(xeniaVersion);

        // Validate that the executable exists before attempting to start the process
        if (!File.Exists(AppPathResolver.GetFullPath(info.ExecutableLocation)))
        {
            Logger.Error<ConfigManager>($"Xenia executable does not exist at: {AppPathResolver.GetFullPath(info.ExecutableLocation)}");
            throw new FileNotFoundException($"Xenia executable not found at: {AppPathResolver.GetFullPath(info.ExecutableLocation)}");
        }
        
        // Clean up current temporary configuration file to generate a fresh one
        if (File.Exists(AppPathResolver.GetFullPath(info.DefaultConfigLocation)))
        {
            File.Delete(AppPathResolver.GetFullPath(info.DefaultConfigLocation));
        }

        // Configure and start the Xenia process
        Process xenia = new Process();
        xenia.StartInfo.FileName = AppPathResolver.GetFullPath(info.ExecutableLocation);
        xenia.StartInfo.WorkingDirectory = AppPathResolver.GetFullPath(info.EmulatorDir);
        xenia.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

        Logger.Debug<ConfigManager>($"Attempting to start Xenia process from: {xenia.StartInfo.FileName} in working directory: {xenia.StartInfo.WorkingDirectory}");

        bool processStarted;
        try
        {
            processStarted = xenia.Start();
        }
        catch (Exception ex)
        {
            Logger.Error<ConfigManager>($"Failed to start Xenia process.");
            Logger.LogExceptionDetails<ConfigManager>(ex);
            throw new InvalidOperationException($"Failed to start Xenia executable at {xenia.StartInfo.FileName}");
        }

        if (!processStarted)
        {
            Logger.Error<ConfigManager>($"Failed to start Xenia process from: {xenia.StartInfo.FileName}");
            throw new InvalidOperationException("Failed to start Xenia process");
        }

        Logger.Info<ConfigManager>("Xenia launched successfully.");
        Logger.Info<ConfigManager>($"Waiting for configuration file to be generated at: {AppPathResolver.GetFullPath(info.DefaultConfigLocation)}");

        // TODO: Replace this with Log processor that can tell when the configuration file is generated instead of relying on file size
        // Wait for the configuration file to be created with sufficient size (>10KB)
        int waitCount = 0;
        const int maxWaitAttempts = 300; // 30 seconds with 100ms delay
        while (!File.Exists(AppPathResolver.GetFullPath(info.DefaultConfigLocation))
               || new FileInfo(AppPathResolver.GetFullPath(info.DefaultConfigLocation)).Length < 10 * 1024)
        {
            if (waitCount++ >= maxWaitAttempts)
            {
                Logger.Warning<ConfigManager>($"Timeout waiting for config file generation. Process ID: {xenia.Id}, IsRunning: {!xenia.HasExited}");
                xenia.Kill();
                throw new TimeoutException($"Timeout waiting for config file generation at {AppPathResolver.GetFullPath(info.DefaultConfigLocation)}");
            }

            Task.Delay(100).Wait(); // Using Wait() to ensure synchronous behavior
        }

        Logger.Info<ConfigManager>($"Configuration file generated successfully at: {AppPathResolver.GetFullPath(info.DefaultConfigLocation)}. " +
                                   $"Size: {new FileInfo(AppPathResolver.GetFullPath(info.DefaultConfigLocation)).Length} bytes");
        Logger.Info<ConfigManager>($"Waiting for emulator to close. Process ID: {xenia.Id}, Generate Profile: {generateProfile}");

        // TODO: Legacy profile generation (Should be removed)
        if (generateProfile)
        {
            Logger.Debug<ConfigManager>("Waiting for user to manually close Xenia process");
            // Wait for the user to manually close Xenia when profile generation is requested
            xenia.WaitForExit();
            Logger.Info<ConfigManager>($"Xenia process exited with code: {xenia.ExitCode}");
        }
        else
        {
            Logger.Debug<ConfigManager>("Closing Xenia process automatically");
            // Close Xenia automatically when profile generation is not requested
            // TODO: Replace this with xenia.Kill(); since we're using Hidden Window
            bool closedGracefully = false;
            if (xenia.CloseMainWindow())
            {
                Logger.Debug<ConfigManager>("Sent close message to Xenia main window");
                if (xenia.WaitForExit(5000)) // Wait up to 5 seconds for a graceful exit
                {
                    closedGracefully = true;
                    Logger.Info<ConfigManager>($"Xenia closed gracefully. Exit code: {xenia.ExitCode}");
                }
                else
                {
                    Logger.Warning<ConfigManager>("Xenia did not close gracefully within timeout, killing process");
                    xenia.Kill();
                }
            }
            else
            {
                Logger.Warning<ConfigManager>("Failed to close Xenia main window, killing process directly");
                xenia.Kill();
            }

            if (!closedGracefully)
            {
                Logger.Info<ConfigManager>("Xenia process killed successfully");
            }
        }

        Logger.Info<ConfigManager>($"Config file generation completed. Config location: {AppPathResolver.GetFullPath(info.DefaultConfigLocation)}");

        Logger.Info<ConfigManager>($"Moving generated config file to final location: {AppPathResolver.GetFullPath(info.ConfigLocation)}");
        File.Move(AppPathResolver.GetFullPath(info.DefaultConfigLocation), AppPathResolver.GetFullPath(info.ConfigLocation), true);

        Logger.Info<ConfigManager>($"Successfully generated and moved config file to final location: {AppPathResolver.GetFullPath(info.ConfigLocation)}");
    }

    /// <summary>
    /// Creates a new configuration file for a specific Xenia version by copying the default configuration file
    /// This method retrieves the default configuration file for the specified Xenia version and copies it to the specified location
    /// It ensures that a valid configuration file exists at the target location for use with the emulator
    /// </summary>
    /// <param name="configurationFile">The path where the new configuration file should be created</param>
    /// <param name="xeniaVersion">The Xenia version for which to create the configuration file</param>
    /// <returns>True if the configuration file was successfully created, false otherwise</returns>
    /// <exception cref="Exception">Thrown when the default configuration file cannot be found or when an error occurs during the file operation</exception>
    public static bool CreateConfigurationFile(string configurationFile, XeniaVersion xeniaVersion)
    {
        Logger.Trace<ConfigManager>($"Starting CreateConfigurationFile operation for Xenia version: {xeniaVersion}");
        Logger.Debug<ConfigManager>($"Target configuration file path: {configurationFile}");

        // Fetch XeniaVersionInfo
        XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(xeniaVersion);
        Logger.Debug<ConfigManager>($"Retrieved version info - Default config location: {versionInfo.ConfigLocation}");

        try
        {
            string defaultConfigPath = AppPathResolver.GetFullPath(versionInfo.ConfigLocation);

            // Check if the default configuration file exists
            if (!File.Exists(defaultConfigPath))
            {
                Logger.Error<ConfigManager>($"Default configuration file not found at {defaultConfigPath}");
                // TODO: Generate new default configuration file if missing
                GenerateEmulatorConfigurationFile(xeniaVersion);
                if (!File.Exists(defaultConfigPath))
                {
                    throw new Exception($"Couldn't find & generate default configuration file for Xenia {XeniaVersion.Canary}");
                }
            }

            Logger.Info<ConfigManager>($"Copying default configuration from {defaultConfigPath} to {configurationFile}");

            // Copy the default configuration file to the new location with a new file name
            File.Copy(defaultConfigPath, configurationFile, true);

            Logger.Info<ConfigManager>($"Successfully created configuration file at {configurationFile}");
            Logger.Debug<ConfigManager>($"CreateConfigurationFile operation completed successfully for Xenia version: {xeniaVersion}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error<ConfigManager>($"Failed to create configuration file: {ex.Message}");
            Logger.LogExceptionDetails<ConfigManager>(ex);
            // Wrap and re-throw any exceptions that occur during file operations
            throw new Exception($"{ex.Message}\n{ex}");
        }
    }

    /// <summary>
    /// Saves the current configuration file for a specific Xenia version back to its original location
    /// This method copies the active configuration file from its default location back to the user's configuration file
    /// allowing changes made during the emulator session to be preserved
    /// </summary>
    /// <param name="configurationFile">The path to the destination configuration file where changes should be saved</param>
    /// <param name="xeniaVersion">The Xenia version whose configuration needs to be saved</param>
    /// <exception cref="NotImplementedException">Thrown when the specified Xenia version is not supported</exception>
    public static void SaveConfigurationFile(string configurationFile, XeniaVersion xeniaVersion)
    {
        Logger.Debug<ConfigManager>($"Starting to save configuration file for Xenia version: {xeniaVersion}");
        Logger.Trace<ConfigManager>($"Source configuration file path: {configurationFile}");

        // Tries to grab the configuration paths from the dictionary
        if (!_configLocations.TryGetValue(xeniaVersion, out (string DefaultConfigLocation, string ConfigLocation) configPaths))
        {
            Logger.Error<ConfigManager>($"Unsupported emulator version requested: {xeniaVersion}");
            throw new NotImplementedException($"Unsupported emulator version: {xeniaVersion}");
        }

        Logger.Debug<ConfigManager>($"Configuration paths retrieved - Default: {configPaths.DefaultConfigLocation}, Config: {configPaths.ConfigLocation}");

        // Copy the current configuration file in use to its original location so changes are saved
        if (File.Exists(configPaths.DefaultConfigLocation))
        {
            Logger.Info<ConfigManager>($"Copying configuration from {configPaths.DefaultConfigLocation} to {configurationFile}");
            try
            {
                File.Copy(configPaths.DefaultConfigLocation, configurationFile, true);
                Logger.Info<ConfigManager>($"Successfully saved configuration file to {configurationFile}");
            }
            catch (Exception ex)
            {
                Logger.Error<ConfigManager>($"Failed to copy configuration file: {ex.Message}");
                Logger.LogExceptionDetails<ConfigManager>(ex);
                throw;
            }
        }
        else
        {
            Logger.Warning<ConfigManager>($"Default configuration file does not exist at {configPaths.DefaultConfigLocation}, skipping save operation");
        }

        Logger.Debug<ConfigManager>($"Finished saving configuration file for Xenia version: {xeniaVersion}");
    }
}