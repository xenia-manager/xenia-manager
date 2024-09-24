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
        /// <param name="game">The game user wants to launch</param>
        /// <param name="windowedMode">Check if he wants it to be in Windowed Mode</param>
        public static async Task LaunchGame(Game game, bool windowedMode = false)
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
            //xenia.StartInfo.ArgumentList.Add(game.GameFilePath);
            //xenia.StartInfo.ArgumentList.Add("--config");
            //xenia.StartInfo.ArgumentList.Add(game.ConfigFilePath);

            // Checking if the game will be run in windowed mode
            if (windowedMode)
            {
                xenia.StartInfo.Arguments += " --fullscreen=false";
            }

            Log.Information($"Xenia Arguments: {xenia.StartInfo.Arguments}");

            // Starting the emulator
            DateTime TimeBeforeLaunch = DateTime.Now;
            xenia.Start();
            xenia.Exited += async (s, args) =>
            {
                //TimeSpan PlayTime = DateTime.Now - TimeBeforeLaunch;
                TimeSpan PlayTime = TimeSpan.FromMinutes(10.5); // For testing purposes
                Log.Information($"Current session playtime: {PlayTime.Minutes} minutes");
                if (game.Playtime != null)
                {
                    game.Playtime += PlayTime.TotalMinutes;
                }
                else
                {
                    game.Playtime = PlayTime.TotalMinutes;
                }
            };
            Log.Information("Emulator started");
            Log.Information("Waiting for emulator to be closed");
            await xenia.WaitForExitAsync(); // Waiting for emulator to close
            Log.Information("Emulator closed");
        }
    }
}
