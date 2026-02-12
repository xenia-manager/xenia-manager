using System.IO;
using XeniaManager.Core.Constants;

namespace XeniaManager.Core.Models;

/// <summary>
/// Represents the different versions/builds of the Xenia emulator
/// </summary>
public enum XeniaVersion
{
    /// <summary>
    /// The main development branch of Xenia with the latest features and improvements
    /// </summary>
    Canary,

    /// <summary>
    /// Xenia Canary fork with keyboard and mouse support
    /// </summary>
    Mousehook,

    /// <summary>
    /// Xenia Canary fork with multiplayer support
    /// </summary>
    Netplay,

    /// <summary>
    /// User-defined custom installation of Xenia
    /// </summary>
    Custom
}

/// <summary>
/// Contains path information for a specific Xenia emulator version
/// </summary>
public class XeniaVersionInfo
{
    /// <summary>
    /// The directory where the Xenia emulator is installed
    /// </summary>
    public string EmulatorDir { get; set; }

    /// <summary>
    /// The full path to the Xenia executable file
    /// </summary>
    public string ExecutableLocation { get; set; }

    /// <summary>
    /// The path to the default configuration file
    /// </summary>
    public string DefaultConfigLocation { get; set; }

    /// <summary>
    /// The path to the active configuration file
    /// </summary>
    public string ConfigLocation { get; set; }

    /// <summary>
    /// The path to the content folder for game files
    /// </summary>
    public string ContentFolderLocation { get; set; }

    /// <summary>
    /// Initializes a new instance of the XeniaVersionInfo class with the specified parameters
    /// </summary>
    /// <param name="emulatorDir">The directory where the Xenia emulator is installed</param>
    /// <param name="executableLocation">The full path to the Xenia executable file</param>
    /// <param name="defaultConfigLocation">The path to the default configuration file</param>
    /// <param name="configLocation">The path to the active configuration file</param>
    /// <param name="contentFolderLocation">The path to the content folder for game files</param>
    public XeniaVersionInfo(string emulatorDir, string executableLocation, string defaultConfigLocation, string configLocation, string contentFolderLocation)
    {
        EmulatorDir = emulatorDir;
        ExecutableLocation = executableLocation;
        DefaultConfigLocation = defaultConfigLocation;
        ConfigLocation = configLocation;
        ContentFolderLocation = contentFolderLocation;
    }

    /// <summary>
    /// Retrieves version-specific information for a given Xenia emulator version
    /// This method returns a XeniaVersionInfo object containing all the necessary paths
    /// and configuration details for the specified Xenia version
    /// </summary>
    /// <param name="version">The Xenia version to get information for (e.g., Canary, Netplay, Mousehook)</param>
    /// <returns>XeniaVersionInfo object containing paths for the emulator directory, executable, config locations, and content directory</returns>
    /// <exception cref="NotImplementedException">Thrown when this method does not yet support the specified Xenia version</exception>
    /// <remarks>
    /// Currently only supports Xenia Canary version. Support for Mousehook and Netplay versions is planned but not yet implemented.
    /// The method uses pattern matching to return the appropriate path configuration based on the version parameter.
    /// </remarks>
    public static XeniaVersionInfo GetXeniaVersionInfo(XeniaVersion version) => version switch
    {
        XeniaVersion.Canary => new XeniaVersionInfo(
            XeniaPaths.Canary.EmulatorDir,
            XeniaPaths.Canary.ExecutableLocation,
            XeniaPaths.Canary.DefaultConfigLocation,
            XeniaPaths.Canary.ConfigLocation,
            Path.Combine(XeniaPaths.Canary.EmulatorDir, "content")
        ),
        // TODO: Mousehook & Netplay
        _ => throw new NotImplementedException($"Xenia {version} is not supported.")
    };
}