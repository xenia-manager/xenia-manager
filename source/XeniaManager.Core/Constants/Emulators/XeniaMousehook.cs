namespace XeniaManager.Core.Constants.Emulators;
public static class XeniaMousehook
{
    /// <summary>Executable file name for Xenia Mousehook.</summary>
    public const string ExecutableName = "xenia_canary_mousehook.exe";

    /// <summary>Configuration file name for Xenia Mousehook.</summary>
    public const string ConfigName = "xenia-canary-mousehook.config.toml";

    public const string BindingsName = "bindings.ini";

    /// <summary>Base directory for Xenia Mousehook emulator.</summary>
    public static readonly string EmulatorDir = Path.Combine("Emulators", "Xenia Mousehook");

    /// <summary>Full path to the Xenia Mousehook executable.</summary>
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

    public static readonly string BindingsLocation = Path.Combine(EmulatorDir, BindingsName);
}