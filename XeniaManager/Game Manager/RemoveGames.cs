using System;
using System.Windows;

// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Removes the game from Xenia Manager
        /// </summary>
        /// <param name="game">Game that we want to remove</param>
        public static void RemoveGame(Game game)
        {
            Log.Information($"Removing {game.Title}");

            // Remove game patch
            if (game.FileLocations.PatchFilePath != null && File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.PatchFilePath)))
            {
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.PatchFilePath));
                Log.Information($"Deleted patch: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.PatchFilePath)}");
            };

            // Remove game configuration file
            if (game.FileLocations.ConfigFilePath != null && File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.ConfigFilePath)))
            {
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.ConfigFilePath));
                Log.Information($"Deleted configuration file: {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.ConfigFilePath)}");
            };

            // Remove game boxart
            if (game.Artwork != null && Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"Artwork\{game.Title}")))
            {
                Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"Artwork\{game.Title}"), true);
                Log.Information($"Delted artwork folder: {Path.GetDirectoryName(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @$"Artwork\{game.Title}"))}");
            }
            
            // Remove game from Xenia Manager
            Log.Information($"Removing {game.Title} from the Library");
            Games.Remove(game);
        }
    }
}
