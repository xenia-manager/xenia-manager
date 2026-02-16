using XeniaManager.Core.Utilities.Paths;

namespace XeniaManager.Core.Constants;

/// <summary>
/// Centralized class for managing all file paths in the application.
/// </summary>
public class AppPaths
{
    // Cache
    public static readonly string CacheDirectory = AppPathResolver.GetFullPath("Cache");

    // Config
    public static readonly string ConfigDirectory = AppPathResolver.GetFullPath("Config");
    public static readonly string ConfigFile = Path.Combine(ConfigDirectory, "config.json");
    public static readonly string GameLibraryPath = Path.Combine(ConfigDirectory, "games.json");

    // Downloads
    public static readonly string DownloadsDirectory = AppPathResolver.GetFullPath("Downloads");
    
    // GameData
    public static readonly string GameDataDirectory = AppPathResolver.GetFullPath("GameData");
    
    // Logging
    public static readonly string LogsDirectory = AppPathResolver.GetFullPath("Logs");
}