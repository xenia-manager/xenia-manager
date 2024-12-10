// Imported
using Newtonsoft.Json;
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// All the currently installed games
        /// </summary>
        public static List<Game> Games { get; set; }

        /// <summary>
        /// Location to the file containing info about installed games
        /// </summary>
        private static string _installedGamesFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Config\games.json");

        /// <summary>
        /// Contains all game compatibility related stuff
        /// </summary>
        private static List<GameCompatibility> GameCompatibilityList { get; set; }

        /// <summary>
        /// Contains all game compatibility related stuff
        /// </summary>
        public static List<GamePatch> GamePatchesList { get; set; }

        /// <summary>
        /// Stuff for creating shortcuts
        /// </summary>
        public static ShortcutManager Shortcut = new ShortcutManager();

        /// <summary>
        /// Initializes
        /// </summary>
        public static void InitializeNewLibrary()
        {
            Log.Information("Creating new library");
            Games = new List<Game>();
        }

        /// <summary>
        /// Loads all the games from a .JSON file
        /// </summary>
        public static void Load()
        {
            if (!File.Exists(_installedGamesFilePath))
            {
                Log.Warning("Couldn't find file that stores all of the installed games");
                InitializeNewLibrary();
                Save();
                return;
            }

            Log.Information("Loading game library");
            Games = JsonConvert.DeserializeObject<List<Game>>(File.ReadAllText(_installedGamesFilePath));
        }

        /// <summary>
        /// Saves all the games into a .JSON file
        /// </summary>
        public static void Save()
        {
            File.WriteAllText(_installedGamesFilePath,
                JsonConvert.SerializeObject(Games.OrderBy(game => game.Title), Formatting.Indented));
        }
    }
}