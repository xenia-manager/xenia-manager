using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;

namespace XeniaManager.Core.Game;

/// <summary>
/// Utility dedicated to launching Emulator standalone and games
/// </summary>
public static class Launcher
{
    /// <summary>
    /// Launches the emulator standalone
    /// </summary>
    /// <param name="xeniaVersion">Xenia Version to launch</param>
    /// <exception cref="NotImplementedException">Missing implementation</exception>
    public static void LaunchEmulator(XeniaVersion xeniaVersion)
    {
        Process xenia = new Process();
        bool changedConfig;
        switch (xeniaVersion)
        {
            case XeniaVersion.Canary:
                xenia.StartInfo.FileName = Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ExecutableLocation);
                xenia.StartInfo.WorkingDirectory = Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.EmulatorDir);
                ConfigManager.ChangeConfigurationFile(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ConfigLocation), XeniaVersion.Canary);
                changedConfig = true;
                break;
            // TODO: Add Support for Mousehook/Netplay (Executable/Emulator location) for launching the emulator
            default:
                throw new NotImplementedException($"Xenia {xeniaVersion} is not implemented");
        }

        Logger.Debug($"Xenia Executable Location: {xenia.StartInfo.FileName}");
        Logger.Debug($"Xenia Working Directory: {xenia.StartInfo.WorkingDirectory}");
        xenia.Start();
        Logger.Info($"Xenia {xeniaVersion} is running.");

        Logger.Info("Waiting for emulator to shutdown.");
        xenia.WaitForExit();
        Logger.Info($"Xenia {xeniaVersion} is closed.");

        // Saving changes done to the configuration file
        if (changedConfig)
        {
            switch (xeniaVersion)
            {
                case XeniaVersion.Canary:
                    ConfigManager.SaveConfigurationFile(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ConfigLocation), xeniaVersion);
                    break;
                // TODO: Add Support for Mousehook/Netplay (Executable/Emulator location) for saving changes after closing the emulator
                default:
                    throw new NotImplementedException($"Xenia {xeniaVersion} is not implemented");
            }
        }
    }

    /// <summary>
    /// Configures settings and launches the Xenia emulator process for the provided game.
    /// </summary>
    /// <param name="game">The game object containing the information required for launching the emulator.</param>
    /// <returns>A tuple containing the Xenia <see cref="Process"/> and a boolean indicating whether the configuration was changed.</returns>
    /// <exception cref="NotImplementedException">
    /// Thrown when the specified Xenia version is not implemented, or custom Xenia version support is not yet added.
    /// </exception>
    private static (Process Xenia, bool ChangedConfig, DateTime launchTime) ConfigureAndStartXenia(Game game)
    {
        Process xenia = new Process();
        bool changedConfig = false;

        switch (game.XeniaVersion)
        {
            case XeniaVersion.Canary:
                xenia.StartInfo.FileName = Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ExecutableLocation);
                xenia.StartInfo.WorkingDirectory = Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.EmulatorDir);
                ConfigManager.ChangeConfigurationFile(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ConfigLocation), XeniaVersion.Canary);
                break;
            // TODO: Add Support for Mousehook/Netplay (Executable/Emulator location) for launching the emulator
            default:
                throw new NotImplementedException($"Xenia {game.XeniaVersion} is not implemented");
        }

        Logger.Debug($"Xenia Executable Location: {xenia.StartInfo.FileName}");
        Logger.Debug($"Xenia Working Directory: {xenia.StartInfo.WorkingDirectory}");
        Logger.Debug($"Game Location: {game.FileLocations.Game}");
        Logger.Debug($"Game Config Location: {game.FileLocations.Config}");


        xenia.StartInfo.Arguments = $@"""{game.FileLocations.Game}""";
        if (game.XeniaVersion != XeniaVersion.Custom)
        {
            changedConfig = ConfigManager.ChangeConfigurationFile(Path.Combine(Constants.DirectoryPaths.Base, game.FileLocations.Config), game.XeniaVersion);
        }
        else
        {
            // TODO: Add support for custom version of Xenia to load it's configuration file while launching a game
            throw new NotImplementedException($"{XeniaVersion.Custom} is not implemented.");
        }

        DateTime timeBeforeLaunch = DateTime.Now;
        xenia.Start();
        Logger.Info($"Xenia {game.XeniaVersion} is running.");
        Logger.Info("Waiting for emulator to shutdown.");

        return (xenia, changedConfig, timeBeforeLaunch);
    }

    /// <summary>
    /// Launches the emulator and the game and waits for it to exit asynchronously
    /// </summary>
    /// <param name="game">Game to be launched</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public static async Task LaunchGameASync(Game game)
    {
        (Process xenia, bool changedConfig, DateTime launchTime) = ConfigureAndStartXenia(game);
        await xenia.WaitForExitAsync();
        Logger.Info($"Xenia {game.XeniaVersion} is closed.");
        if (game.Playtime != null)
        {
            game.Playtime += (DateTime.Now - launchTime).TotalMinutes;
        }
        else
        {
            game.Playtime = (DateTime.Now - launchTime).TotalMinutes;
        }
        // Saving changes done to the configuration file
        if (changedConfig)
        {
            ConfigManager.SaveConfigurationFile(Path.Combine(Constants.DirectoryPaths.Base, game.FileLocations.Config), game.XeniaVersion);
        }
    }

    /// <summary>
    /// Launches the emulator and the game
    /// </summary>
    /// <param name="game">Game to be launched</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public static void LaunchGame(Game game)
    {
        (Process xenia, bool changedConfig, DateTime launchTime) = ConfigureAndStartXenia(game);
        xenia.WaitForExit();
        Logger.Info($"Xenia {game.XeniaVersion} is closed.");
        if (game.Playtime != null)
        {
            game.Playtime += (DateTime.Now - launchTime).TotalMinutes;
        }
        else
        {
            game.Playtime = (DateTime.Now - launchTime).TotalMinutes;
        }
        // Saving changes done to the configuration file
        if (changedConfig)
        {
            ConfigManager.SaveConfigurationFile(Path.Combine(Constants.DirectoryPaths.Base, game.FileLocations.Config), game.XeniaVersion);
        }
    }
}