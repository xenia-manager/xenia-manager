namespace XeniaManager.Core.Constants.Emulators;

/// <summary>
/// Constants specific to the "Xenia Canary" emulator version.
/// </summary>
public static class XeniaCanary
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
    public static readonly string LogLocation = Path.Combine(EmulatorDir, "xenia.log");

    /// <summary>Configuration file path in the config subdirectory.</summary>
    public static readonly string ConfigLocation = Path.Combine(ConfigFolderLocation, ConfigName);

    /// <summary>Default configuration file location before it's moved to the config directory.</summary>
    public static readonly string DefaultConfigLocation = Path.Combine(EmulatorDir, ConfigName);
}