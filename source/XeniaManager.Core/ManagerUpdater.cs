using Octokit;

namespace XeniaManager.Core;

public static class ManagerUpdater
{
    #region Variables
    // TODO: Change this from experimental to user builds
    private const string _repositoryOwner = "xenia-manager";
    private const string _repositoryName = "experimental-builds";

    #endregion

    #region Functions

    public static async Task<bool> CheckForUpdates(string currentVersion)
    {
        Release latestRelease = await Github.GetLatestRelease(_repositoryOwner, _repositoryName);
        Logger.Debug($"Latest Xenia Manager version: {latestRelease.TagName}");
        if (latestRelease.TagName != currentVersion)
        {
            Logger.Info("There is a update for Xenia Manager");
            return true;
        }
        Logger.Info("There is no update for Xenia Manager");
        return false;
    }

    public static async Task<string> GrabDownloadLink()
    {
        Release latestRelease = await Github.GetLatestRelease(_repositoryOwner, _repositoryName);
        Logger.Debug($"Latest Xenia Manager version: {latestRelease.TagName}");
        ReleaseAsset asset = latestRelease.Assets.FirstOrDefault(asset => asset.Name == "xenia_manager.zip");
        Logger.Debug($"Latest Release version: {asset.BrowserDownloadUrl}");
        return asset.BrowserDownloadUrl;
    }

    #endregion
}