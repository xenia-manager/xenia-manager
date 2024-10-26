using System;

// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Checks if there is already a configuration file in the new emulator location and if it's not, moves it
        /// </summary>
        /// <param name="game">Game that we're moving</param>
        /// <param name="oldEmulatorLocation">Old emulator location</param>
        /// <param name="newEmulatorLocation">New emulator location</param>
        /// <param name="defaultConfigFileLocation">Location to wards the default configuration file</param>
        private static void TransferConfigurationFile(Game game, string oldEmulatorLocation, string newEmulatorLocation, string defaultConfigFileLocation)
        {
            // Check if the game has a configuration file
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, newEmulatorLocation, $@"config\{game.Title}.config.toml")))
            {
                Log.Information("Game configuration file not found");
                Log.Information("Creating a new configuration file from the default one");
                File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, defaultConfigFileLocation), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, newEmulatorLocation, $@"config\{game.Title}.config.toml"), true);
            }
            game.FileLocations.ConfigFilePath = Path.Combine(newEmulatorLocation, $@"config\{game.Title}.config.toml");
        }

        /// <summary>
        /// Checks if there are any patch files and transfers them to the new folder
        /// </summary>
        /// <param name="game">Game that we're moving</param>
        /// <param name="oldVersion">Xenia version that the game used</param>
        /// <param name="newVersion">New Xenia version that we want the game to be using</param>
        /// <param name="newEmulatorLocation">New emulator location</param>
        private static void TransferPatchFile(Game game, EmulatorVersion oldVersion, EmulatorVersion newVersion, string newEmulatorLocation)
        {
            // Check if the patches folder exists, if it doesn't, create it
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, newEmulatorLocation, @$"patches"));

            // Moving patch file
            File.Move(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.PatchFilePath), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, newEmulatorLocation, @$"patches\{Path.GetFileName(game.FileLocations.PatchFilePath)}"));

            game.FileLocations.PatchFilePath = Path.Combine(newEmulatorLocation, @$"patches\{Path.GetFileName(game.FileLocations.PatchFilePath)}");
        }

        /// <summary>
        /// Transfers the content of the game to a new version
        /// </summary>
        /// <param name="game">Game that we're moving</param>
        /// <param name="oldEmulatorLocation">Old emulator location</param>
        /// <param name="newEmulatorLocation">New emulator location</param>
        private static void TransferContent(Game game, string oldEmulatorLocation, string newEmulatorLocation)
        {
            // Create all of the necessary directories for content copy
            foreach (string dirPath in Directory.GetDirectories(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"{oldEmulatorLocation}content\{game.GameId}"), "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"{oldEmulatorLocation}content\{game.GameId}"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"{newEmulatorLocation}content\{game.GameId}")));
            }

            // Copy all the files
            foreach (string newPath in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"{oldEmulatorLocation}content\{game.GameId}"), "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"{oldEmulatorLocation}content\{game.GameId}"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"{newEmulatorLocation}content\{game.GameId}")), true);
            }
        }

        /// <summary>
        /// Makes the game use different Xenia version
        /// </summary>
        /// <param name="game">Game that we're moving</param>
        /// <param name="oldVersion">Xenia version that the game used</param>
        /// <param name="newVersion">New Xenia version that we want the game to be using</param>
        /// <param name="oldEmulatorLocation">Old emulator location</param>
        /// <param name="newEmulatorLocation">New emulator location</param>
        /// <param name="defaultConfigFileLocation">Location to wards the default configuration file</param>
        public static void SwitchXeniaVersion(Game game, EmulatorVersion oldVersion, EmulatorVersion newVersion, string oldEmulatorLocation, string newEmulatorLocation, string defaultConfigFileLocation)
        {
            // Check if the old version is Custom and if it is, reset the "EmulatorExecutableLocation" to null
            if (oldVersion == EmulatorVersion.Custom)
            {
                game.FileLocations.EmulatorExecutableLocation = null;
            }

            Log.Information($"Moving the game to Xenia {newVersion}");
            game.EmulatorVersion = newVersion; // Setting the new emulator version
            TransferConfigurationFile(game, oldEmulatorLocation, newEmulatorLocation, defaultConfigFileLocation); // Moving configuration file
            // Making sure that transfer is between Canary and Mousehook to do the patch transfer
            if (((oldVersion == EmulatorVersion.Canary || oldVersion == EmulatorVersion.Mousehook) && (newVersion == EmulatorVersion.Canary || newVersion == EmulatorVersion.Mousehook)) && game.FileLocations.PatchFilePath != null)
            {
                TransferPatchFile(game, oldVersion, newVersion, newEmulatorLocation);
            }
            // Checking if there is some content installed that should be copied over
            if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"{oldEmulatorLocation}content\{game.GameId}")))
            {
                Log.Information($"Copying all of the installed content and saves from Xenia {oldVersion} to Xenia {newVersion}");
                TransferContent(game, oldEmulatorLocation, newEmulatorLocation);
            }
            else
            {
                Log.Information("No installed content or saves found");
            }
            GameManager.Save(); // Saves the changes to a folder
        }
    }
}