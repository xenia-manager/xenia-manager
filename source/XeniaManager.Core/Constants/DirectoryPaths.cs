namespace XeniaManager.Core.Constants;
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