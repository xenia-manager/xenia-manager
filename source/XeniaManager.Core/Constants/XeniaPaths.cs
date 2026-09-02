namespace XeniaManager.Core.Constants;

/// <summary>
/// Represents the file system paths for different Xenia emulator variants.
/// This class encapsulates all the necessary file and directory paths required
/// for managing Xenia emulators within the application.
/// </summary>
public sealed class XeniaPaths
{
    /// <summary>
    /// Name of the content folder inside every Xenia variant's directory.
    /// </summary>
    public const string ContentFolderName = "content";

    /// <summary>
    /// Name of the config folder inside every Xenia variant's directory.
    /// </summary>
    public const string ConfigFolderName = "config";

    /// <summary>
    /// Name of the patches folder inside every Xenia variant's directory.
    /// </summary>
    public const string PatchFolderName = "patches";

    /// <summary>
    /// Name of the screenshots folder inside every Xenia variant's directory.
    /// </summary>
    public const string ScreenshotsFolderName = "screenshots";

    /// <summary>
    /// Name of the SDL game controller database file inside every Xenia variant's directory.
    /// </summary>
    public const string GameControllerDatabaseName = "gamecontrollerdb.txt";

    /// <summary>
    /// Gets the display name of the Xenia variant (e.g., "Xenia Canary", "Xenia Mousehook").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the name of the executable file for this Xenia variant (e.g., "xenia_canary.exe").
    /// </summary>
    public string ExecutableName { get; }

    /// <summary>
    /// Gets the name of the configuration file for this Xenia variant (e.g., "xenia-canary.config.toml").
    /// </summary>
    public string ConfigName { get; }

    /// <summary>
    /// Gets the optional name of the bindings file (e.g., "bindings.ini").
    /// This property can be null if the variant doesn't use bindings.
    /// </summary>
    public string? BindingsName { get; }

    /// <summary>
    /// Gets the directory path where this Xenia variant is stored, relative to the application's base directory.
    /// Format: "Emulators/{Name}"
    /// </summary>
    public string EmulatorDir { get; }

    /// <summary>
    /// Gets the full path to the executable file for this Xenia variant.
    /// Format: "{EmulatorDir}/{ExecutableName}"
    /// </summary>
    public string ExecutableLocation { get; }

    /// <summary>
    /// Gets the path to the content folder for this Xenia variant.
    /// Format: "{EmulatorDir}/content"
    /// </summary>
    public string ContentFolderLocation { get; }

    /// <summary>
    /// Gets the path to the configuration folder for this Xenia variant.
    /// Format: "{EmulatorDir}/config"
    /// </summary>
    public string ConfigFolderLocation { get; }

    /// <summary>
    /// Gets the path to the patches folder for this Xenia variant.
    /// Format: "{EmulatorDir}/patches"
    /// </summary>
    public string PatchFolderLocation { get; }

    /// <summary>
    /// Gets the path to the screenshots folder for this Xenia variant.
    /// Format: "{EmulatorDir}/screenshots"
    /// </summary>
    public string ScreenshotsFolderLocation { get; }

    /// <summary>
    /// Gets the path to the log file for this Xenia variant.
    /// Format: "{EmulatorDir}/xenia.log"
    /// </summary>
    public string LogLocation { get; }

    /// <summary>
    /// Gets the path to the SDL game controller database file for this Xenia variant.
    /// Format: "{EmulatorDir}/gamecontrollerdb.txt"
    /// </summary>
    public string GameControllerDatabaseLocation { get; }

    /// <summary>
    /// Gets the full path to the configuration file for this Xenia variant.
    /// Format: "{ConfigFolderLocation}/{ConfigName}"
    /// </summary>
    public string ConfigLocation { get; }

    /// <summary>
    /// Gets the path to the default configuration file for this Xenia variant.
    /// Format: "{EmulatorDir}/{ConfigName}"
    /// </summary>
    public string DefaultConfigLocation { get; }

    /// <summary>
    /// Gets the optional full path to the bindings file for this Xenia variant.
    /// This property can be null if the variant doesn't use bindings.
    /// Format: "{EmulatorDir}/{BindingsName}" or null
    /// </summary>
    public string? BindingsLocation { get; }

    /// <summary>
    /// Gets the path to the XConfig settings file for this Xenia variant.
    /// Format: "{EmulatorDir}/xconfig.settings"
    /// </summary>
    public string XConfigLocation { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="XeniaPaths"/> class with the specified parameters.
    /// </summary>
    /// <param name="name">The display name of the Xenia variant.</param>
    /// <param name="executableName">The name of the executable file.</param>
    /// <param name="configName">The name of the configuration file.</param>
    /// <param name="bindingsName">Optional name of the bindings file (defaults to null).</param>
    private XeniaPaths(string name, string executableName, string configName, string? bindingsName = null)
    {
        Name = name;
        ExecutableName = executableName;
        ConfigName = configName;
        BindingsName = bindingsName;

        EmulatorDir = Path.Combine("Emulators", name);
        ExecutableLocation = Path.Combine(EmulatorDir, ExecutableName);

        ContentFolderLocation = Path.Combine(EmulatorDir, ContentFolderName);
        ConfigFolderLocation = Path.Combine(EmulatorDir, ConfigFolderName);
        PatchFolderLocation = Path.Combine(EmulatorDir, PatchFolderName);
        ScreenshotsFolderLocation = Path.Combine(EmulatorDir, ScreenshotsFolderName);
        LogLocation = Path.Combine(EmulatorDir, "xenia.log");
        GameControllerDatabaseLocation = Path.Combine(EmulatorDir, GameControllerDatabaseName);

        ConfigLocation = Path.Combine(ConfigFolderLocation, ConfigName);
        DefaultConfigLocation = Path.Combine(EmulatorDir, ConfigName);

        BindingsLocation = bindingsName != null
            ? Path.Combine(EmulatorDir, bindingsName)
            : null;

        XConfigLocation = Path.Combine(EmulatorDir, "xconfig.settings");
    }

    /// <summary>
    /// Represents the paths for the Xenia Canary variant.
    /// </summary>
    public static readonly XeniaPaths Canary = new XeniaPaths("Xenia Canary", "xenia_canary.exe", "xenia-canary.config.toml");

    /// <summary>
    /// Represents the paths for the Xenia Mousehook variant.
    /// </summary>
    public static readonly XeniaPaths Mousehook =
        new XeniaPaths("Xenia Mousehook", "xenia_canary_mousehook.exe", "xenia-canary-mousehook.config.toml", "bindings.ini");

    /// <summary>
    /// Represents the paths for the Xenia Netplay variant.
    /// </summary>
    public static readonly XeniaPaths Netplay = new XeniaPaths("Xenia Netplay", "xenia_canary_netplay.exe", "xenia-canary-netplay.config.toml");
}