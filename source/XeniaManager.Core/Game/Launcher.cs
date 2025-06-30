using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Text.RegularExpressions;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Constants.Emulators;

namespace XeniaManager.Core.Game;

/// <summary>
/// Utility dedicated to launching Emulator standalone and games
/// </summary>
public static class Launcher
{
    private static List<GamerProfile> _currentProfiles = [];
    public static bool XeniaUpdating = false;

    /// <summary>
    /// Launches the emulator standalone
    /// </summary>
    /// <param name="xeniaVersion">Xenia Version to launch</param>
    /// <exception cref="NotImplementedException">Missing implementation</exception>
    public static void LaunchEmulator(XeniaVersion xeniaVersion)
    {
        if (XeniaUpdating)
        {
            Logger.Warning("Xenia is currently updating, please wait until the update is finished.");
            throw new OperationCanceledException("Xenia is currently updating, please wait until the update is finished.");
        }

        Process xenia = new Process();
        bool changedConfig;
        switch (xeniaVersion)
        {
            case XeniaVersion.Canary:
                xenia.StartInfo.FileName = Path.Combine(DirectoryPaths.Base, XeniaCanary.ExecutableLocation);
                xenia.StartInfo.WorkingDirectory = Path.Combine(DirectoryPaths.Base, XeniaCanary.EmulatorDir);
                ConfigManager.ChangeConfigurationFile(Path.Combine(DirectoryPaths.Base, XeniaCanary.ConfigLocation), XeniaVersion.Canary);
                changedConfig = true;
                break;
            case XeniaVersion.Mousehook:
                xenia.StartInfo.FileName = Path.Combine(DirectoryPaths.Base, XeniaMousehook.ExecutableLocation);
                xenia.StartInfo.WorkingDirectory = Path.Combine(DirectoryPaths.Base, XeniaMousehook.EmulatorDir);
                ConfigManager.ChangeConfigurationFile(Path.Combine(DirectoryPaths.Base, XeniaMousehook.ConfigLocation), XeniaVersion.Mousehook);
                changedConfig = true;
                break;
            case XeniaVersion.Netplay:
                xenia.StartInfo.FileName = Path.Combine(DirectoryPaths.Base, XeniaNetplay.ExecutableLocation);
                xenia.StartInfo.WorkingDirectory = Path.Combine(DirectoryPaths.Base, XeniaNetplay.EmulatorDir);
                ConfigManager.ChangeConfigurationFile(Path.Combine(DirectoryPaths.Base, XeniaNetplay.ConfigLocation), XeniaVersion.Netplay);
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
                    ConfigManager.SaveConfigurationFile(Path.Combine(DirectoryPaths.Base, XeniaCanary.ConfigLocation), xeniaVersion);
                    break;
                case XeniaVersion.Mousehook:
                    ConfigManager.SaveConfigurationFile(Path.Combine(DirectoryPaths.Base, XeniaMousehook.ConfigLocation), xeniaVersion);
                    break;
                case XeniaVersion.Netplay:
                    ConfigManager.SaveConfigurationFile(Path.Combine(DirectoryPaths.Base, XeniaNetplay.ConfigLocation), xeniaVersion);
                    break;
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
        _currentProfiles = [];
        switch (game.XeniaVersion)
        {
            case XeniaVersion.Canary:
                xenia.StartInfo.FileName = Path.Combine(DirectoryPaths.Base, XeniaCanary.ExecutableLocation);
                xenia.StartInfo.WorkingDirectory = Path.Combine(DirectoryPaths.Base, XeniaCanary.EmulatorDir);
                ConfigManager.ChangeConfigurationFile(Path.Combine(DirectoryPaths.Base, XeniaCanary.ConfigLocation), XeniaVersion.Canary);
                break;
            case XeniaVersion.Mousehook:
                xenia.StartInfo.FileName = Path.Combine(DirectoryPaths.Base, XeniaMousehook.ExecutableLocation);
                xenia.StartInfo.WorkingDirectory = Path.Combine(DirectoryPaths.Base, XeniaMousehook.EmulatorDir);
                ConfigManager.ChangeConfigurationFile(Path.Combine(DirectoryPaths.Base, XeniaMousehook.ConfigLocation), XeniaVersion.Mousehook);
                changedConfig = true;
                break;
            case XeniaVersion.Netplay:
                xenia.StartInfo.FileName = Path.Combine(DirectoryPaths.Base, XeniaNetplay.ExecutableLocation);
                xenia.StartInfo.WorkingDirectory = Path.Combine(DirectoryPaths.Base, XeniaNetplay.EmulatorDir);
                ConfigManager.ChangeConfigurationFile(Path.Combine(DirectoryPaths.Base, XeniaNetplay.ConfigLocation), XeniaVersion.Netplay);
                changedConfig = true;
                break;
            case XeniaVersion.Custom:
                xenia.StartInfo.FileName = game.FileLocations.CustomEmulatorExecutable;
                xenia.StartInfo.WorkingDirectory = Path.GetDirectoryName(game.FileLocations.CustomEmulatorExecutable);
                break;
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
        xenia.StartInfo.RedirectStandardOutput = true;
        xenia.StartInfo.UseShellExecute = false;
        xenia.StartInfo.CreateNoWindow = true;
        DateTime timeBeforeLaunch = DateTime.Now;
        xenia.Start();
        xenia.BeginOutputReadLine();
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
    public static async Task LaunchGameASync(Game game, bool automaticSaveBackup, string profileSlot)
    {
        if (XeniaUpdating)
        {
            Logger.Warning("Xenia is currently updating, please wait until the update is finished.");
            throw new OperationCanceledException("Xenia is currently updating, please wait until the update is finished.");
        }

        (Process xenia, bool changedConfig, DateTime launchTime) = ConfigureAndStartXenia(game);
        xenia.OutputDataReceived += (sender, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data))
            {
                return;
            }

            if (XeniaLogProcessor.GamerProfilesRegex.Match(e.Data) is { Success: true } gamerProfileMatch)
            {
                Logger.Debug(e.Data);
                XeniaLogProcessor.ProcessLoadedGamerProfiles(gamerProfileMatch, _currentProfiles);
            }
        };
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

        if (game.XeniaVersion != XeniaVersion.Custom)
        {
            if (_currentProfiles.Count > 0 && automaticSaveBackup)
            {
                foreach (GamerProfile profile in _currentProfiles)
                {
                    if (profile.Slot == profileSlot && Directory.Exists(Path.Combine(xenia.StartInfo.WorkingDirectory, "content", profile.Xuid, game.GameId, "00000001")))
                    {
                        Logger.Info($"Backing up profile '{profile.Name}' ({profile.Xuid})");
                        string saveFileLocation = Path.Combine(xenia.StartInfo.WorkingDirectory, "content", profile.Xuid, game.GameId, "00000001");
                        Logger.Debug($"Save Location: {saveFileLocation}");
                        string headersLocation = Path.Combine(xenia.StartInfo.WorkingDirectory, "content", profile.Xuid, game.GameId, "Headers", "00000001");
                        Logger.Debug($"Headers Location: {headersLocation}");
                        Directory.CreateDirectory(Path.Combine(Constants.DirectoryPaths.Backup, game.Title, $"{profile.Name} ({profile.Xuid})"));
                        string destination = Path.Combine(Constants.DirectoryPaths.Backup, game.Title, $"{profile.Name} ({profile.Xuid})", $"{DateTime.Now:yyyyMMdd_HHmmss} Save File.zip");
                        Logger.Debug($"Backup Location: {destination}");
                        SaveManager.ExportSave(game, destination, saveFileLocation, headersLocation);
                    }
                }
            }

            // Saving changes done to the configuration file
            if (changedConfig)
            {
                ConfigManager.SaveConfigurationFile(Path.Combine(Constants.DirectoryPaths.Base, game.FileLocations.Config), game.XeniaVersion);
            }
        }
    }

    /// <summary>
    /// Launches the emulator and the game
    /// </summary>
    /// <param name="game">Game to be launched</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    public static void LaunchGame(Game game, bool automaticSaveBackup, string profileSlot)
    {
        if (XeniaUpdating)
        {
            Logger.Warning("Xenia is currently updating, please wait until the update is finished.");
            return;
        }

        (Process xenia, bool changedConfig, DateTime launchTime) = ConfigureAndStartXenia(game);
        xenia.OutputDataReceived += (sender, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data))
            {
                return;
            }

            if (XeniaLogProcessor.GamerProfilesRegex.Match(e.Data) is { Success: true } gamerProfileMatch)
            {
                Logger.Debug(e.Data);
                XeniaLogProcessor.ProcessLoadedGamerProfiles(gamerProfileMatch, _currentProfiles);
            }
        };
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

        if (game.XeniaVersion != XeniaVersion.Custom)
        {
            if (_currentProfiles.Count > 0 && automaticSaveBackup)
            {
                foreach (GamerProfile profile in _currentProfiles)
                {
                    if (profile.Slot == profileSlot && Directory.Exists(Path.Combine(xenia.StartInfo.WorkingDirectory, "content", profile.Xuid, game.GameId, "00000001")))
                    {
                        Logger.Info($"Backing up profile '{profile.Name}' ({profile.Xuid})");
                        string saveFileLocation = Path.Combine(xenia.StartInfo.WorkingDirectory, "content", profile.Xuid, game.GameId, "00000001");
                        Logger.Debug($"Save Location: {saveFileLocation}");
                        string headersLocation = Path.Combine(xenia.StartInfo.WorkingDirectory, "content", profile.Xuid, game.GameId, "Headers", "00000001");
                        Logger.Debug($"Headers Location: {headersLocation}");
                        Directory.CreateDirectory(Path.Combine(Constants.DirectoryPaths.Backup, game.Title, $"{profile.Name} ({profile.Xuid})"));
                        string destination = Path.Combine(Constants.DirectoryPaths.Backup, game.Title, $"{profile.Name} ({profile.Xuid})", $"{DateTime.Now:yyyyMMdd_HHmmss} Save File.zip");
                        Logger.Debug($"Backup Location: {destination}");
                        SaveManager.ExportSave(game, destination, saveFileLocation, headersLocation);
                    }
                }
            }

            // Saving changes done to the configuration file
            if (changedConfig)
            {
                ConfigManager.SaveConfigurationFile(Path.Combine(Constants.DirectoryPaths.Base, game.FileLocations.Config), game.XeniaVersion);
            }
        }
    }
}