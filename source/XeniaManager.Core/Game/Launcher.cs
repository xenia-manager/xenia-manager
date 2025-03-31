using System.Diagnostics;

namespace XeniaManager.Core.Game;

public static class Launcher
{
    /// <summary>
    /// Launches the emulator standalone
    /// </summary>
    /// <param name="xeniaVersion">Xenia Version to launch</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
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
                default:
                    throw new NotImplementedException($"Xenia {xeniaVersion} is not implemented");
            }
        }
    }

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
            default:
                throw new ArgumentOutOfRangeException(nameof(game.XeniaVersion), game.XeniaVersion, null);
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