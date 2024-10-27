﻿using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

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
                case EmulatorVersion.Canary:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaCanary.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation);
                    break;
                case EmulatorVersion.Mousehook:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaMousehook.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation);
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
            // Adding game to the launch arguments so Xenia Emulator knows what to run
            xenia.StartInfo.Arguments = $@"""{game.FileLocations.GameFilePath}""";
            
            // Loading configuration file
            if (game.EmulatorVersion == EmulatorVersion.Custom && game.FileLocations.ConfigFilePath != null)
            {
                // Custom version of Xenia
                xenia.StartInfo.Arguments += $@" --config ""{game.FileLocations.ConfigFilePath}""";
            }
            else if (game.EmulatorVersion != EmulatorVersion.Custom)
            {
                // Canary/Mousehook/Netplay
                ChangeConfigurationFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.ConfigFilePath), game.EmulatorVersion);
            }

            // Checking if the game will be run in windowed mode
            if (windowedMode)
            {
                xenia.StartInfo.Arguments += " --fullscreen=false";
            }

            Log.Information($"Xenia Arguments: {xenia.StartInfo.Arguments}");
            
            // Stores all of the profiles loaded in Xenia
            List<GamerProfile> currentProfiles = new List<GamerProfile>();
            
            // Redirect standard output to capture console messages
            xenia.StartInfo.RedirectStandardOutput = true; // Redirecting console output into xenia.OutputDataReceived
            xenia.StartInfo.UseShellExecute = false;
            xenia.StartInfo.CreateNoWindow = true; // No Console window
            
            // Event handler for processing console output
            xenia.OutputDataReceived += (sender, e) =>
            {
                // Checking if the console output of Xenia isn't null
                if (string.IsNullOrWhiteSpace(e.Data))
                {
                    return;
                };
                
                // Check if the output contains the specific line we're looking for
                // Checking for gamerProfiles
                Match gamerProfilesMatch = Regex.Match(e.Data, @"Loaded\s(?<Gamertag>\w+)\s\(GUID:\s(?<GUID>[A-F0-9]+)\)\sto\sslot\s(?<Slot>[0-4])");
                if (gamerProfilesMatch.Success)
                {
                    GamerProfilesProcess(gamerProfilesMatch, currentProfiles);
                }
            };

            // Starting the emulator
            DateTime timeBeforeLaunch = DateTime.Now;
            xenia.Start();
            
            // Begin reading the console output asynchronously
            xenia.BeginOutputReadLine();

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
            
            foreach (GamerProfile profile in currentProfiles)
            {
                Log.Information($"Detected profile '{profile.Name}' with GUID '{profile.GUID}' in slot {profile.Slot}");
            }
        }
    }
}