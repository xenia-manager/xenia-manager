using Octokit;

namespace XeniaManager.Core;

public static class ManagerUpdater
{
    #region Functions

    public static async Task<bool> CheckForUpdates(string currentVersion, string repositoryOwner = "xenia-manager", string repositoryName = "xenia-manager")
    {
        Release latestRelease = await Github.GetLatestRelease(repositoryOwner, repositoryName);
        Logger.Debug($"Latest Xenia Manager version: {latestRelease.TagName}");
        if (latestRelease.TagName != currentVersion)
        {
            Logger.Info("There is a update for Xenia Manager");
            return true;
        }
        Logger.Info("There is no update for Xenia Manager");
        return false;
    }

    public static async Task<string> GrabDownloadLink(string repositoryOwner = "xenia-manager", string repositoryName = "xenia-manager")
    {
        Release latestRelease = await Github.GetLatestRelease(repositoryOwner, repositoryName);
        Logger.Debug($"Latest Xenia Manager version: {latestRelease.TagName}");
        ReleaseAsset asset = latestRelease.Assets.FirstOrDefault(asset => asset.Name == "xenia_manager.zip");
        Logger.Debug($"Latest Release version: {asset.BrowserDownloadUrl}");
        return asset.BrowserDownloadUrl;
    }

    #endregion
}