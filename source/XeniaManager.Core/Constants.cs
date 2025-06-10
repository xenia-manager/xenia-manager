namespace XeniaManager.Core;

/// <summary>
/// Provides a collection of constant values used globally throughout the application,
/// including directory paths, file paths, URLs, and settings related to Xenia emulators.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Contains application directory paths used throughout the application.
    /// </summary>
    public static class DirectoryPaths
    {
        /// <summary>Base directory of the application.</summary>
        public static readonly string Base = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string Backup = Path.Combine(Base, "Backup");

        /// <summary>Directory for application cache files.</summary>
        public static readonly string Cache = Path.Combine(Base, "Cache");

        /// <summary>Directory for application configuration files.</summary>
        public static readonly string Config = Path.Combine(Base, "Config");

        /// <summary>User's desktop directory path.</summary>
        public static readonly string Desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        /// <summary>Directory for downloaded files.</summary>
        public static readonly string Downloads = Path.Combine(Base, "Downloads");
        public static readonly string Emulators = Path.Combine(Base, "Emulators");
        public static readonly string EmulatorContent = Path.Combine(Emulators, "Content");

        /// <summary>Directory for game-related data, such as artwork and assets.</summary>
        public static readonly string GameData = Path.Combine(Base, "GameData");

        /// <summary>User's desktop directory path.</summary>
        public static readonly string Logs = Path.Combine(Base, "Logs");
    }

    /// <summary>
    /// Contains file paths used throughout the application.
    /// </summary>
    public static class FilePaths
    {
        /// <summary>Path to the game library JSON file.</summary>
        public static readonly string GameLibrary = Path.Combine(DirectoryPaths.Config, "games.json");

        /// <summary>Path to the application's main executable.</summary>
        public static readonly string ManagerExecutable = Path.Combine(DirectoryPaths.Base, "XeniaManager.exe");
    }

    /// <summary>
    /// Provides URLs for accessing Xbox database services, game compatibility data,
    /// and related resources.
    /// </summary>
    public static class Urls
    {
        /// <summary>URL for the Xbox Marketplace games database in JSON format.</summary>
        public static readonly string XboxDatabase = "https://raw.githubusercontent.com/xenia-manager/Database/main/Database/xbox_marketplace_games.json";

        /// <summary>URL for detailed Xbox game information.</summary>
        public static readonly string XboxDatabaseGameInfo = "https://raw.githubusercontent.com/xenia-manager/Database/main/Database/Xbox%20Marketplace";

        /// <summary>Base URL for Xbox database artwork assets.</summary>
        public static readonly string XboxDatabaseArtworkBase = "https://raw.githubusercontent.com/xenia-manager/Assets/main/Artwork";

        /// <summary>URL to the game compatibility JSON database.</summary>
        public static readonly string GameCompatibility = "https://raw.githubusercontent.com/xenia-manager/Database/main/Database/game_compatibility.json";
    }

    /// <summary>
    /// Contains constants related to the Xenia Emulator.
    /// </summary>
    public static class Xenia
    {
        /// <summary>
        /// Constants specific to the "Xenia Canary" emulator version.
        /// </summary>
        public static class Canary
        {
            /// <summary>Executable file name for Xenia Canary.</summary>
            public const string ExecutableName = "xenia_canary.exe";

            /// <summary>Configuration file name for Xenia Canary.</summary>
            public const string ConfigName = "xenia-canary.config.toml";

            /// <summary>Base directory for Xenia Canary emulator.</summary>
            public static readonly string EmulatorDir = Path.Combine("Emulators", "Xenia Canary");

            /// <summary>Full path to the Xenia Canary executable.</summary>
            public static readonly string ExecutableLocation = Path.Combine(EmulatorDir, ExecutableName);
            public static readonly string ContentFolderLocation = Path.Combine(EmulatorDir, "content");
            public static readonly string ConfigFolderLocation = Path.Combine(EmulatorDir, "config");
            public static readonly string PatchFolderLocation = Path.Combine(EmulatorDir, "patches");
            public static readonly string ScreenshotsFolderLocation = Path.Combine(EmulatorDir, "screenshots");

            /// <summary>Configuration file path in the config subdirectory.</summary>
            public static readonly string ConfigLocation = Path.Combine(ConfigFolderLocation, ConfigName);

            /// <summary>Default configuration file location before it's moved to the config directory.</summary>
            public static readonly string DefaultConfigLocation = Path.Combine(EmulatorDir, ConfigName);
        }
    }
}