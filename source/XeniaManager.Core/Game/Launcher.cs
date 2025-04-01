using System.Diagnostics;

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
        bool changedConfig = false;
        switch (xeniaVersion)
        {
            case XeniaVersion.Canary:
                xenia.StartInfo.FileName = Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.ExecutableLocation);
                xenia.StartInfo.WorkingDirectory = Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.EmulatorDir);
                ConfigManager.ChangeConfigurationFile(Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.ConfigLocation), XeniaVersion.Canary);
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
                    ConfigManager.SaveConfigurationFile(Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.ConfigLocation), xeniaVersion);
                    break;
                // TODO: Add Support for Mousehook/Netplay (Executable/Emulator location) for saving changes after closing the emulator
                default:
                    throw new NotImplementedException($"Xenia {xeniaVersion} is not implemented");
            }
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
        Process xenia = new Process();
        bool changedConfig = false;
        switch (game.XeniaVersion)
        {
            case XeniaVersion.Canary:
                xenia.StartInfo.FileName = Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.ExecutableLocation);
                xenia.StartInfo.WorkingDirectory = Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.EmulatorDir);
                ConfigManager.ChangeConfigurationFile(Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.ConfigLocation), XeniaVersion.Canary);
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
            changedConfig = ConfigManager.ChangeConfigurationFile(Path.Combine(Constants.BaseDir, game.FileLocations.Config), game.XeniaVersion);
        }
        else
        {
            // TODO: Add support for custom version of Xenia to load it's configuration file while launching a game
            throw new NotImplementedException($"{XeniaVersion.Custom} is not implemented.");
        }
        xenia.Start();
        Logger.Info($"Xenia {game.XeniaVersion} is running.");

        Logger.Info("Waiting for emulator to shutdown.");
        xenia.WaitForExit();
        Logger.Info($"Xenia {game.XeniaVersion} is closed.");
        
        // Saving changes done to the configuration file
        if (changedConfig)
        {
            ConfigManager.SaveConfigurationFile(Path.Combine(Constants.BaseDir, game.FileLocations.Config), game.XeniaVersion);
        }
    }
}