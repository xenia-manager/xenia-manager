﻿using System.Diagnostics;

// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
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
                case EmulatorVersion.Canary:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaCanary.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation);
                    ChangeConfigurationFile(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            ConfigurationManager.AppConfig.XeniaCanary.ConfigurationFileLocation),
                        EmulatorVersion.Canary);
                    break;
                case EmulatorVersion.Mousehook:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaMousehook.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation);
                    ChangeConfigurationFile(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            ConfigurationManager.AppConfig.XeniaMousehook.ConfigurationFileLocation),
                        EmulatorVersion.Mousehook);
                    break;
                case EmulatorVersion.Netplay:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaNetplay.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation);
                    ChangeConfigurationFile(
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            ConfigurationManager.AppConfig.XeniaNetplay.ConfigurationFileLocation),
                        EmulatorVersion.Netplay);
                    break;
                case EmulatorVersion.Custom:
                    xenia.StartInfo.FileName = game.FileLocations.EmulatorExecutableLocation;
                    xenia.StartInfo.WorkingDirectory =
                        Path.GetDirectoryName(game.FileLocations.EmulatorExecutableLocation);
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