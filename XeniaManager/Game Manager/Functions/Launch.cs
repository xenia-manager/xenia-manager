using System;
using System.Diagnostics;

// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Launches the game
        /// </summary>
        /// <param name="game">The game user wants to launch.</param>
        /// <param name="windowedMode">Check if the game should be in Windowed Mode.</param>
        public static void LaunchGame(Game game, bool windowedMode = false)
        {
            Log.Information($"Launching {game.Title}");
            Process xenia = new Process();

            // Checking what emulator the game uses
            switch (game.EmulatorVersion)
            {
                case EmulatorVersion.Stable:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaStable.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaStable.EmulatorLocation);
                    break;
                case EmulatorVersion.Canary:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaCanary.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation);
                    break;
                case EmulatorVersion.Netplay:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaNetplay.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation);
                    break;
                case EmulatorVersion.Custom:
                    xenia.StartInfo.FileName = game.FileLocations.EmulatorExecutableLocation;
                    xenia.StartInfo.WorkingDirectory = Path.GetDirectoryName(game.FileLocations.EmulatorExecutableLocation);
                    break;
                default:
                    break;
            }

            Log.Information($"Xenia Executable Location: {xenia.StartInfo.FileName}");

            // Adding default launch arguments
            if (game.EmulatorVersion != EmulatorVersion.Custom && game.FileLocations.ConfigFilePath != null)
            {
                xenia.StartInfo.Arguments = $@"""{game.FileLocations.GameFilePath}"" --config ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.ConfigFilePath)}""";
            }
            else if (game.FileLocations.ConfigFilePath != null)
            {
                xenia.StartInfo.Arguments = $@"""{game.FileLocations.GameFilePath}"" --config ""{game.FileLocations.ConfigFilePath}""";
            }

            // Checking if the game will be run in windowed mode
            if (windowedMode)
            {
                xenia.StartInfo.Arguments += " --fullscreen=false";
            }

            Log.Information($"Xenia Arguments: {xenia.StartInfo.Arguments}");

            // Starting the emulator
            DateTime timeBeforeLaunch = DateTime.Now;
            xenia.Start();

            // Wait for the emulator to exit
            xenia.WaitForExit(); // Blocking call to wait for emulator to close

            // Calculate playtime after the emulator has closed
            TimeSpan playTime = DateTime.Now - timeBeforeLaunch;
            Log.Information($"Current session playtime: {playTime.Minutes} minutes");
            if (game.Playtime != null)
            {
                game.Playtime += playTime.TotalMinutes;
            }
            else
            {
                game.Playtime = playTime.TotalMinutes;
            }

            Log.Information("Emulator closed");
        }

        /// <summary>
        /// Launches the emulator version that the game uses
        /// </summary>
        /// <param name="game">The game whose emulator user wants to open</param>
        public static async Task LaunchEmulator(Game game)
        {
            Process xenia = new Process();

            // Checking what emulator the game uses
            switch (game.EmulatorVersion)
            {
                case EmulatorVersion.Stable:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaStable.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaStable.EmulatorLocation);
                    break;
                case EmulatorVersion.Canary:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaCanary.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation);
                    break;
                case EmulatorVersion.Netplay:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaNetplay.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation);
                    break;
                case EmulatorVersion.Custom:
                    xenia.StartInfo.FileName = game.FileLocations.EmulatorExecutableLocation;
                    xenia.StartInfo.WorkingDirectory = Path.GetDirectoryName(game.FileLocations.EmulatorExecutableLocation);
                    break;
                default:
                    break;
            }

            // Launching the emulator
            Log.Information($"Xenia Executable Location: {xenia.StartInfo.FileName}");
            xenia.Start();
            Log.Information("Emulator started");

            // Waiting for emulator to close
            Log.Information("Waiting for emulator to be closed");
            await xenia.WaitForExitAsync();
            Log.Information("Emulator closed");
        }
    }
}
