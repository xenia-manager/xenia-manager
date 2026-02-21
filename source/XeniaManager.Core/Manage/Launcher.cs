using System.Diagnostics;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities.Paths;

namespace XeniaManager.Core.Manage;

/// <summary>
/// Handles launching the Xenia emulator with the appropriate configuration
/// Manages the process lifecycle and configuration file operations for different Xenia versions
/// </summary>
public class Launcher
{
    /// <summary>
    /// Launches the specified Xenia emulator version with its associated configuration
    /// This method handles applying the appropriate configuration file before launching,
    /// starting the emulator process, waiting for it to exit, and then saving any changes
    /// made to the configuration during the session
    /// </summary>
    /// <param name="xeniaVersion">The Xenia version to launch</param>
    public static void LaunchEmulator(XeniaVersion xeniaVersion)
    {
        Logger.Info<Launcher>($"Launching Xenia emulator for version: {xeniaVersion}");

        try
        {
            XeniaVersionInfo info = XeniaVersionInfo.GetXeniaVersionInfo(xeniaVersion);
            Logger.Debug<Launcher>($"Retrieved Xenia version info - Executable: {info.ExecutableLocation}, Emulator Dir: {info.EmulatorDir}, Config: {info.ConfigLocation}");

            bool changedConfig;
            Process xenia = new Process();
            xenia.StartInfo.FileName = AppPathResolver.GetFullPath(info.ExecutableLocation);
            xenia.StartInfo.WorkingDirectory = AppPathResolver.GetFullPath(info.EmulatorDir);

            Logger.Trace<Launcher>($"Setting up process - Executable: {xenia.StartInfo.FileName}, Working Directory: {xenia.StartInfo.WorkingDirectory}");

            changedConfig = ConfigManager.ChangeConfigurationFile(AppPathResolver.GetFullPath(info.ConfigLocation), xeniaVersion);
            Logger.Info<Launcher>($"Configuration file change status: {changedConfig}");

            Logger.Info<Launcher>($"Starting Xenia process for version: {xeniaVersion}");
            xenia.Start();
            Logger.Info<Launcher>($"Xenia process started successfully with PID: {xenia.Id}");

            Logger.Debug<Launcher>($"Waiting for Xenia process to exit...");
            xenia.WaitForExit();
            Logger.Info<Launcher>($"Xenia process has exited with code: {xenia.ExitCode}");

            if (changedConfig)
            {
                Logger.Info<Launcher>($"Configuration was changed, saving updated configuration file for {xeniaVersion}");
                // Save a newly modified configuration file
                ConfigManager.SaveConfigurationFile(AppPathResolver.GetFullPath(info.ConfigLocation), xeniaVersion);
                Logger.Info<Launcher>($"Configuration file saved successfully for {xeniaVersion}");
            }
            else
            {
                Logger.Debug<Launcher>($"No configuration changes detected, skipping save operation for {xeniaVersion}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error<Launcher>($"Error occurred while launching Xenia emulator: {ex.Message}");
            Logger.LogExceptionDetails<Launcher>(ex);
            throw;
        }

        Logger.Info<Launcher>($"Finished launching Xenia emulator for version: {xeniaVersion}");
    }

    /// <summary>
    /// Launches a game asynchronously using the specified Xenia emulator version
    /// This method handles the complete game launch lifecycle including:
    /// - Resolving the appropriate Xenia version configuration
    /// - Setting up the emulator process with the game file
    /// - Managing configuration file changes for non-custom versions
    /// - Waiting for the game session to complete
    /// - Persisting any configuration changes made during the session
    /// </summary>
    /// <param name="game">The game object containing file locations and Xenia version information</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public static async Task LaunchGameASync(Game game)
    {
        Logger.Info<Launcher>($"Launching game: {game.Title} using Xenia version: {game.XeniaVersion}");

        Process xenia = new Process();
        XeniaVersionInfo xeniaVersionInfo = XeniaVersionInfo.GetXeniaVersionInfo(game.XeniaVersion);
        
        Logger.Debug<Launcher>($"Retrieved Xenia version info - Executable: {xeniaVersionInfo.ExecutableLocation}, Emulator Dir: {xeniaVersionInfo.EmulatorDir}, Config: {xeniaVersionInfo.ConfigLocation}");

        bool changedConfig = false;
        
        // Configure the Xenia emulator process with the executable path and working directory
        xenia.StartInfo.FileName = AppPathResolver.GetFullPath(xeniaVersionInfo.ExecutableLocation);
        xenia.StartInfo.WorkingDirectory = AppPathResolver.GetFullPath(xeniaVersionInfo.EmulatorDir);
        
        // Set the game file path as a command-line argument
        xenia.StartInfo.Arguments = $@"""{game.FileLocations.Game}""";
        
        Logger.Trace<Launcher>($"Process configuration - Executable: {xenia.StartInfo.FileName}, Working Directory: {xenia.StartInfo.WorkingDirectory}, Arguments: {xenia.StartInfo.Arguments}");

        // For non-custom Xenia versions, apply the appropriate configuration file before launch
        if (game.XeniaVersion != XeniaVersion.Custom)
        {
            Logger.Debug<Launcher>($"Non-custom Xenia version detected, applying configuration file for {game.XeniaVersion}");
            changedConfig = ConfigManager.ChangeConfigurationFile(AppPathResolver.GetFullPath(game.FileLocations.Config!), game.XeniaVersion);
            Logger.Info<Launcher>($"Configuration file change status: {changedConfig}");
        }
        else
        {
            Logger.Debug<Launcher>($"Custom Xenia version detected, skipping configuration file change");
        }

        Logger.Info<Launcher>($"Starting Xenia process for game: {game.Title}");
        xenia.Start();
        Logger.Info<Launcher>($"Xenia process started successfully with PID: {xenia.Id}");

        Logger.Debug<Launcher>($"Waiting for Xenia process to exit...");
        await xenia.WaitForExitAsync();
        Logger.Info<Launcher>($"Xenia process has exited with code: {xenia.ExitCode}");

        // Save configuration changes if the configuration was modified during the session
        if (changedConfig)
        {
            Logger.Info<Launcher>($"Configuration was changed, saving updated configuration file for {game.XeniaVersion}");
            ConfigManager.SaveConfigurationFile(AppPathResolver.GetFullPath(game.FileLocations.Config!), game.XeniaVersion);
            Logger.Info<Launcher>($"Configuration file saved successfully for {game.XeniaVersion}");
        }
        else
        {
            Logger.Debug<Launcher>($"No configuration changes detected, skipping save operation for {game.XeniaVersion}");
        }

        Logger.Info<Launcher>($"Finished launching game: {game.Title}");
    }
}