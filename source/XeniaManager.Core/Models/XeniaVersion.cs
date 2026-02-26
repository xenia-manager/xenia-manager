using System.IO;
using System.Text.Json.Serialization;
using XeniaManager.Core.Constants;

namespace XeniaManager.Core.Models;

/// <summary>
/// Represents the different versions/builds of the Xenia emulator
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
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
public sealed class XeniaVersionInfo
{
    public string Name { get; }
    public string ExecutableName { get; }
    public string ConfigName { get; }
    public string? BindingsName { get; }

    public string EmulatorDir { get; }
    public string ExecutableLocation { get; }
    public string ContentFolderLocation { get; }
    public string ConfigFolderLocation { get; }
    public string PatchFolderLocation { get; }
    public string ScreenshotsFolderLocation { get; }
    public string LogLocation { get; }

    public string ConfigLocation { get; }
    public string DefaultConfigLocation { get; }
    public string? BindingsLocation { get; }

    private XeniaVersionInfo(XeniaPaths paths)
    {
        Name = paths.Name;
        ExecutableName = paths.ExecutableName;
        ConfigName = paths.ConfigName;
        BindingsName = paths.BindingsName;

        EmulatorDir = paths.EmulatorDir;
        ExecutableLocation = paths.ExecutableLocation;
        ContentFolderLocation = paths.ContentFolderLocation;
        ConfigFolderLocation = paths.ConfigFolderLocation;
        PatchFolderLocation = paths.PatchFolderLocation;
        ScreenshotsFolderLocation = paths.ScreenshotsFolderLocation;
        LogLocation = paths.LogLocation;

        ConfigLocation = paths.ConfigLocation;
        DefaultConfigLocation = paths.DefaultConfigLocation;
        BindingsLocation = paths.BindingsLocation;
    }

    public static XeniaVersionInfo GetXeniaVersionInfo(XeniaVersion version)
    {
        XeniaPaths paths = version switch
        {
            XeniaVersion.Canary => XeniaPaths.Canary,
            XeniaVersion.Mousehook => XeniaPaths.Mousehook,
            XeniaVersion.Netplay => XeniaPaths.Netplay,
            XeniaVersion.Custom => throw new NotSupportedException(
                "Custom Xenia versions must be provided explicitly."),
            _ => throw new ArgumentOutOfRangeException(nameof(version), version, null)
        };

        return new XeniaVersionInfo(paths);
    }

    public static XeniaVersionInfo GetXeniaVersionInfoFromPaths(XeniaPaths paths) => new XeniaVersionInfo(paths);
}