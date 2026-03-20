using XeniaManager.Core.Models;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Installation;

/// <summary>
/// Service for managing Xenia Manager installation and updates
/// </summary>
public class ManagerService
{
    /// <summary>
    /// Checks if the current version is different from the latest available version
    /// </summary>
    /// <param name="releaseService">The release service for fetching version information</param>
    /// <param name="currentVersion">The current version of Xenia Manager</param>
    /// <param name="isExperimental">True to check the experimental version, false for the stable version</param>
    /// <returns>True if a newer version is available, false otherwise</returns>
    public static async Task<bool> CheckForUpdates(IReleaseService releaseService, string currentVersion, bool isExperimental)
    {
        // Get the latest version from the release service based on the channel
        ManagerBuild? latestBuild = await releaseService.GetManagerBuildAsync(
            isExperimental ? ReleaseType.XeniaManagerExperimental : ReleaseType.XeniaManagerStable);

        // If we couldn't get the latest version, assume no update available
        if (latestBuild == null)
        {
            return false;
        }

        // Compare versions - return true if they're different (update available)
        return latestBuild.Version != currentVersion;
    }
}