// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Removes the game patch
        /// </summary>
        public static void RemoveGamePatch(Game game)
        {
            // Deleting the patch file
            Log.Information($"Removing patch for {game}");
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.PatchFilePath)))
            {
                File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, game.FileLocations.PatchFilePath));
            }

            game.FileLocations.PatchFilePath = null;
            Log.Information("Patch removed");
            GameManager.Save(); // Save changes to the file
        }
    }
}