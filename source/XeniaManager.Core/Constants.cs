namespace XeniaManager.Core;

/// <summary>
/// Contains all the constants used by the code
/// </summary>
public static class Constants
{
    // Global
    public static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string CacheDir = Path.Combine(BaseDir, "Cache");
    public static readonly string ConfigDir = Path.Combine(BaseDir, "Config");
    public static readonly string DownloadDir = Path.Combine(BaseDir, "Downloads");
    public static readonly string GameLibrary = Path.Combine(ConfigDir, "games.json");
    public static readonly string GamedataDir = Path.Combine(BaseDir, "GameData");

    public static class Urls
    {
        public static readonly string XboxDatabase = "https://raw.githubusercontent.com/xenia-manager/Database/main/Database/xbox_marketplace_games.json";
        public static readonly string XboxDatabaseGameInfo = "https://raw.githubusercontent.com/xenia-manager/Database/main/Database/Xbox%20Marketplace";
        public static readonly string XboxDatabaseArtworkBase = "https://raw.githubusercontent.com/xenia-manager/Assets/main/Artwork";
        public static readonly string GameCompatibility = "https://raw.githubusercontent.com/xenia-manager/Database/main/Database/game_compatibility.json";
    }
    
    // Xenia constants
    public static class Xenia
    {
        // Canary constants
        public static class Canary
        {
            public const string ExecutableName = "xenia_canary.exe";
            public const string ConfigName = "xenia-canary.config.toml";
            public static readonly string EmulatorDir = Path.Combine("Emulators", "Xenia Canary");
            public static readonly string ExecutableLocation = Path.Combine(EmulatorDir, ExecutableName);
            public static readonly string ConfigLocation = Path.Combine(EmulatorDir, "config" ,ConfigName);
            public static readonly string DefaultConfigLocation = Path.Combine(EmulatorDir, ConfigName);
        }
    }
}