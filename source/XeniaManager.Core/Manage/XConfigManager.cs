using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Manage;

/// <summary>
/// Manages Xbox 360 XConfig settings for Xenia emulator, including loading and saving
/// of dashboard/console/user settings stored in XConfig files.
/// </summary>
public class XConfigManager
{
    /// <summary>
    /// Checks if an XConfig file exists for the specified Xenia version.
    /// </summary>
    /// <param name="version">The Xenia version to check.</param>
    /// <returns>True if the XConfigfile exists, false otherwise.</returns>
    public static bool XConfigExists(XeniaVersion version)
    {
        Logger.Trace<XConfigManager>($"Checking if XConfig exists for version: {version}");

        XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);
        string xconfigPath = AppPathResolver.GetFullPath(versionInfo.XConfigLocation);

        bool exists = File.Exists(xconfigPath);
        Logger.Debug<XConfigManager>($"XConfig file at '{xconfigPath}' {(exists ? "exists" : "does not exist")}");
        return exists;
    }

    /// <summary>
    /// Loads the XConfig settings from the XConfig file for the specified Xenia version.
    /// </summary>
    /// <param name="version">The Xenia version to load XConfig for.</param>
    /// <returns>The loaded XConfigFile, or null if the file does not exist.</returns>
    public static XConfigFile? LoadXConfig(XeniaVersion version)
    {
        Logger.Trace<XConfigManager>($"Starting LoadXConfig operation for version: {version}");

        XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);
        string xconfigPath = AppPathResolver.GetFullPath(versionInfo.XConfigLocation);

        if (!File.Exists(xconfigPath))
        {
            Logger.Warning<XConfigManager>($"XConfig file not found at '{xconfigPath}'");
            return null;
        }

        Logger.Info<XConfigManager>($"Loading XConfig file from '{xconfigPath}'");
        XConfigFile xconfig = XConfigFile.Load(xconfigPath);
        Logger.Info<XConfigManager>($"Successfully loaded XConfig file from '{xconfigPath}'");

        return xconfig;
    }

    /// <summary>
    /// Saves the XConfig settings to the XConfig file for the specified Xenia version.
    /// </summary>
    /// <param name="xconfig">The XConfigFile instance to save.</param>
    /// <param name="version">The Xenia version to save XConfig for.</param>
    public static void SaveXConfig(XConfigFile xconfig, XeniaVersion version)
    {
        Logger.Trace<XConfigManager>($"Starting SaveXConfig operation for version: {version}");

        XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);
        string xconfigPath = AppPathResolver.GetFullPath(versionInfo.XConfigLocation);

        Logger.Info<XConfigManager>($"Saving XConfig file to '{xconfigPath}'");
        xconfig.Save(xconfigPath);
        Logger.Info<XConfigManager>($"Successfully saved XConfig file to '{xconfigPath}'");
    }
}
