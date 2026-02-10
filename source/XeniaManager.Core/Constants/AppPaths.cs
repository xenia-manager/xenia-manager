using XeniaManager.Core.Utilities.Paths;

namespace XeniaManager.Core.Constants;

/// <summary>
/// Centralized class for managing all file paths in the application.
/// </summary>
public class AppPaths
{
    // Config
    public static readonly string ConfigDirectory = AppPathResolver.GetFullPath("Config");
    public static readonly string ConfigFile = Path.Combine(ConfigDirectory, "config.json");
    
    // Logging
    public static readonly string LogsDirectory = AppPathResolver.GetFullPath("Logs");
}