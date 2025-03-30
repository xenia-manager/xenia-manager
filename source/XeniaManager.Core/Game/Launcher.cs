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
        switch (xeniaVersion)
        {
            case XeniaVersion.Canary:
                xenia.StartInfo.FileName = Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.ExecutableLocation);
                xenia.StartInfo.WorkingDirectory = Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.EmulatorDir);
                ConfigManager.ChangeConfigurationFile(Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.ConfigLocation), XeniaVersion.Canary);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(xeniaVersion), xeniaVersion, null);
        }
        
        Logger.Debug($"Xenia Executable Location: {xenia.StartInfo.FileName}");
        Logger.Debug($"Xenia Working Directory: {xenia.StartInfo.WorkingDirectory}");
        xenia.Start();
        Logger.Info($"Xenia {xeniaVersion} is running.");

        Logger.Info("Waiting for emulator to shutdown.");
        xenia.WaitForExit();
        Logger.Info($"Xenia {xeniaVersion} is closed.");
    }
}