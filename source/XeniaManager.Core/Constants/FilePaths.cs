namespace XeniaManager.Core.Constants;
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