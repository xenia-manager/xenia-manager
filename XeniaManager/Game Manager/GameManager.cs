using System;

// Imported
using Newtonsoft.Json;
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// All of the currently installed games
        /// </summary>
        public static List<Game> InstalledGames {  get; set; }

        /// <summary>
        /// Location
        /// </summary>
        private static string InstalledGamesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "installedGames.json");

        /// <summary>
        /// Initializes
        /// </summary>
        public static void InitializeNewLibrary()
        {
            Log.Information("Creating new library");
            InstalledGames = new List<Game>();
        }

        /// <summary>
        /// Loads all of the games from a .JSON file
        /// </summary>
        public static void LoadGames()
        {
            if (!File.Exists(InstalledGamesFilePath))
            {
                Log.Warning("Couldn't find file that stores all of the installed games");
                InitializeNewLibrary();
                SaveGames();
                return;
            }
            InstalledGames = JsonConvert.DeserializeObject<List<Game>>(File.ReadAllText(InstalledGamesFilePath));
        }

        /// <summary>
        /// Saves all of the games into a .JSON file
        /// </summary>
        public static void SaveGames()
        {
            File.WriteAllText(InstalledGamesFilePath, JsonConvert.SerializeObject(InstalledGames, Formatting.Indented));
        }
    }
}
