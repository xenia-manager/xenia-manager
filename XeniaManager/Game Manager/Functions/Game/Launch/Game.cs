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
            bool failedSymbolicLinking = false;
            // Checking what emulator the game uses
            switch (game.EmulatorVersion)
            {
                case EmulatorVersion.Canary:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaCanary.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation);
                    break;
                case EmulatorVersion.Mousehook:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaMousehook.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation);
                    break;
                case EmulatorVersion.Netplay:
                    xenia.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaNetplay.ExecutableLocation);
                    xenia.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation);
                    break;
                case EmulatorVersion.Custom:
                    xenia.StartInfo.FileName = game.FileLocations.EmulatorExecutableLocation;
                    xenia.StartInfo.WorkingDirectory =
                        Path.GetDirectoryName(game.FileLocations.EmulatorExecutableLocation);
                    break;
                default:
                    break;
            }

            Log.Information($"Xenia Executable Location: {xenia.StartInfo.FileName}");

            // Adding default launch arguments
            // Adding game to the launch arguments so Xenia Emulator knows what to run
            xenia.StartInfo.Arguments = $@"""{game.FileLocations.GameFilePath}""";

            // Loading configuration file
            if (game is { EmulatorVersion: EmulatorVersion.Custom, FileLocations.ConfigFilePath: not null })
            {
                // Custom version of Xenia
                xenia.StartInfo.Arguments += $@" --config ""{game.FileLocations.ConfigFilePath}""";
            }
            else if (game.EmulatorVersion != EmulatorVersion.Custom)
            {
                // Canary/Mousehook/Netplay
                failedSymbolicLinking = ChangeConfigurationFile(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.ConfigFilePath),
                    game.EmulatorVersion);
            }

            // Checking if the game will be run in windowed mode
            if (windowedMode)
            {
                xenia.StartInfo.Arguments += " --fullscreen=false";
            }

            Log.Information($"Xenia Arguments: {xenia.StartInfo.Arguments}");

            // Stores all the profiles loaded in Xenia
            List<GamerProfile> currentProfiles = new List<GamerProfile>();

            // Redirect standard output to capture console messages
            xenia.StartInfo.RedirectStandardOutput = true; // Redirecting console output into xenia.OutputDataReceived
            xenia.StartInfo.UseShellExecute = false;
            xenia.StartInfo.CreateNoWindow = true; // No Console window

            // Event handler for processing console output
            xenia.OutputDataReceived += (_, e) =>
            {
                // Checking if the console output of Xenia isn't null
                if (string.IsNullOrWhiteSpace(e.Data))
                {
                    return;
                };

                // Check if the output contains the specific line we're looking for
                // Checking for gamerProfiles
                Match gamerProfilesMatch = Regex.Match(e.Data,
                    @"Loaded\s(?<Gamertag>\w+)\s\(GUID:\s(?<GUID>[A-F0-9]+)\)\sto\sslot\s(?<Slot>[0-4])");
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
            
            // Checking if symbolic linking failed to ensure changes to configuration file are made
            if (failedSymbolicLinking)
            {
                string emulatorConfigurationFile = game.EmulatorVersion switch
                {
                    EmulatorVersion.Canary => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Emulators\Xenia Canary\xenia-canary.config.toml"),
                    EmulatorVersion.Mousehook => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Emulators\Xenia Mousehook\xenia-canary-mousehook.config.toml"),
                    EmulatorVersion.Netplay => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Emulators\Xenia Netplay\xenia-canary-netplay.config.toml"),
                    _ => null, // Handles any unexpected value
                };
                if (emulatorConfigurationFile != null && File.Exists(emulatorConfigurationFile))
                {
                    File.Copy(emulatorConfigurationFile, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.ConfigFilePath), true);
                }
            }
            
            // Checking if the automatic save backup is enabled and if it is, backup the save file
            if (currentProfiles.Count > 0 && ConfigurationManager.AppConfig.AutomaticSaveBackup == true)
            {
                foreach (GamerProfile profile in currentProfiles)
                {
                    if (profile.Slot == (ConfigurationManager.AppConfig.ProfileSlot - 1).ToString() &&
                        Directory.Exists(Path.Combine(xenia.StartInfo.WorkingDirectory, "content", profile.Xuid,
                            game.GameId, "00000001")))
                    {
                        Log.Information($"Backing up profile '{profile.Name}' ({profile.Xuid})");
                        string saveFileLocation = Path.Combine(xenia.StartInfo.WorkingDirectory, "content",
                            profile.Xuid, game.GameId, "00000001");
                        string headersLocation = Path.Combine(xenia.StartInfo.WorkingDirectory, "content", profile.Xuid,
                            game.GameId, "Headers/00000001");
                        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            $"Backup/{game.Title}"));
                        string destination = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Backup/{game.Title}",
                            $"{DateTime.Now:yyyyMMdd_HHmmss} - {game.Title} ({profile.Name} - {profile.Xuid}) Save File.zip");
                        GameManager.ExportSaveGames(game, destination, saveFileLocation, headersLocation);
                        break;
                    }
                }
            }
        }
    }
}