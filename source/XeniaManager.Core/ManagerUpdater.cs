using System.Text.Json;

// Imported Libraries
using Octokit;
using XeniaManager.Core.Constants;

namespace XeniaManager.Core;

public static class ManagerUpdater
{
    #region Functions

    public static async Task<bool> CheckForUpdates(string currentVersion, string repositoryOwner = "xenia-manager", string repositoryName = "xenia-manager")
    {
        // Use VersionDatabase for Xenia Manager releases
        bool isExperimental = repositoryName == "experimental-builds";
        Release latestRelease = await VersionDatabase.GetManagerRelease(isExperimental);
        Logger.Debug($"Latest Xenia Manager version: {latestRelease.TagName}");
        bool isUpdate = !string.Equals(latestRelease.TagName, currentVersion, StringComparison.OrdinalIgnoreCase);
        Logger.Info(isUpdate ? "There is an update for Xenia Manager" : "There is no update for Xenia Manager");

        return isUpdate;
    }

    public static async Task<string> GrabDownloadLink(string repositoryOwner = "xenia-manager", string repositoryName = "xenia-manager")
    {
        // Use VersionDatabase for Xenia Manager releases
        bool isExperimental = repositoryName == "experimental-builds";
        Release latestRelease = await VersionDatabase.GetManagerRelease(isExperimental);
        Logger.Debug($"Latest Xenia Manager version: {latestRelease.TagName}");
        ReleaseAsset asset = latestRelease.Assets.FirstOrDefault(asset => asset.Name == "xenia_manager.zip")
            ?? latestRelease.Assets.FirstOrDefault();
        Logger.Debug($"Latest Release version: {asset.BrowserDownloadUrl}");
        return asset.BrowserDownloadUrl;
    }

    #endregion
}