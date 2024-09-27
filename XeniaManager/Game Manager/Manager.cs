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
        public static List<Game> Games {  get; set; }

        /// <summary>
        /// Location
        /// </summary>
        private static string InstalledGamesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Config\games.json");

        /// <summary>
        /// Initializes
        /// </summary>
        public static void InitializeNewLibrary()
        {
            Log.Information("Creating new library");
            Games = new List<Game>();
        }

        /// <summary>
        /// Loads all of the games from a .JSON file
        /// </summary>
        public static void Load()
        {
            if (!File.Exists(InstalledGamesFilePath))
            {
                Log.Warning("Couldn't find file that stores all of the installed games");
                InitializeNewLibrary();
                Save();
                return;
            }
            Log.Information("Loading game library");
            Games = JsonConvert.DeserializeObject<List<Game>>(File.ReadAllText(InstalledGamesFilePath));
        }

        /// <summary>
        /// Saves all of the games into a .JSON file
        /// </summary>
        public static void Save()
        {
            File.WriteAllText(InstalledGamesFilePath, JsonConvert.SerializeObject(Games.OrderBy(game => game.Title), Formatting.Indented));
        }
    }
}
