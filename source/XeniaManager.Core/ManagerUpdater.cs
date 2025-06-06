using Octokit;

namespace XeniaManager.Core;

public static class ManagerUpdater
{
    public static async Task<bool> CheckForUpdates(string currentVersion)
    {
        // TODO: Change this from experimental to user builds
        Release latestRelease = await Github.GetLatestRelease("xenia-manager", "experimental-builds");
        Logger.Debug($"Latest Release version: {latestRelease.TagName}");
        if (latestRelease.TagName != currentVersion)
        {
            Logger.Info("There is a update for Xenia Manager");
            return true;
        }
        else
        {
            Logger.Info("There is no update for Xenia Manager");
            return false;
        }
    }
}