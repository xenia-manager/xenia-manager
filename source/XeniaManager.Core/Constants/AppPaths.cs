using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Constants;

/// <summary>
/// Centralized class for managing all file paths in the application.
/// </summary>
public class AppPaths
{
    // Backup
    public static readonly string Backup = AppPathResolver.GetFullPath("Backup");

    // Cache
    public static readonly string CacheDirectory = AppPathResolver.GetFullPath("Cache");
    public static readonly string ImageCacheDirectory = Path.Combine(CacheDirectory, "Images");
    public static readonly string DatabaseCacheDirectory = Path.Combine(CacheDirectory, "Database");
    public static readonly string PatchesCacheDirectory = Path.Combine(DatabaseCacheDirectory, "Patches");
    public static readonly string X360DataBaseCacheDirectory = Path.Combine(DatabaseCacheDirectory, "x360db");

    // Config
    public static readonly string ConfigDirectory = AppPathResolver.GetFullPath("Config");
    public static readonly string ConfigFile = Path.Combine(ConfigDirectory, "config.json");
    public static readonly string ConfigFileBackup = Path.Combine(ConfigDirectory, "config.json.backup");
    public static readonly string GameLibraryPath = Path.Combine(ConfigDirectory, "games.json");

    // Downloads
    public static readonly string DownloadsDirectory = AppPathResolver.GetFullPath("Downloads");

    // Emulators
    public static readonly string EmulatorsDirectory = AppPathResolver.GetFullPath("Emulators");
    public static readonly string EmulatorsContentDirectory = Path.Combine(EmulatorsDirectory, "Content");

    // GameData
    public static readonly string GameDataDirectory = AppPathResolver.GetFullPath("GameData");

    // Logging
    public static readonly string LogsDirectory = AppPathResolver.GetFullPath("Logs");

    // Themes
    public static readonly string ThemesDirectory = AppPathResolver.GetFullPath("Themes");

    public static readonly string ManagerExecutable = AppPathResolver.GetFullPath("XeniaManager.exe");
}