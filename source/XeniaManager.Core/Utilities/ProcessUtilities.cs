using System.Diagnostics;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Provides utilities for managing and checking process status.
/// </summary>
public class ProcessUtilities
{
    /// <summary>
    /// Checks if a specific Xenia emulator process is currently running.
    /// </summary>
    /// <param name="version">The Xenia version to check.</param>
    /// <returns>True if the process is running, false otherwise.</returns>
    public static bool IsXeniaRunning(XeniaVersion version)
    {
        string? processName = GetProcessName(version);

        if (string.IsNullOrEmpty(processName))
        {
            Logger.Warning<ProcessUtilities>($"Unknown Xenia version: {version}");
            return false;
        }

        Process[] processes = Process.GetProcessesByName(processName);
        bool isRunning = processes.Length > 0;

        Logger.Info<ProcessUtilities>(isRunning ? $"{processName} is currently running ({processes.Length} instance(s))" : $"{processName} is not running");

        return isRunning;
    }

    /// <summary>
    /// Gets the process name (without .exe extension) for a Xenia version.
    /// </summary>
    /// <param name="version">The Xenia version.</param>
    /// <returns>The process name, or null if the version is not recognized.</returns>
    private static string? GetProcessName(XeniaVersion version)
    {
        if (version == XeniaVersion.Custom)
        {
            return null;
        }

        try
        {
            XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);
            return Path.GetFileNameWithoutExtension(versionInfo.ExecutableName);
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }
}