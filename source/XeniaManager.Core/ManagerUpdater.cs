using Octokit;

namespace XeniaManager.Core;

public static class ManagerUpdater
{
    #region Variables

    private const string _repositoryOwner = "xenia-manager";
    private const string _repositoryName = "experimental-builds";

    #endregion

    #region Functions

    public static async Task<bool> CheckForUpdates(string currentVersion)
    {
        // TODO: Change this from experimental to user builds
        Release latestRelease = await Github.GetLatestRelease(_repositoryOwner, _repositoryName);
        Logger.Debug($"Latest Release version: {latestRelease.TagName}");
        if (latestRelease.TagName != currentVersion)
        {
            Logger.Info("There is a update for Xenia Manager");
            return true;
        }
        Logger.Info("There is no update for Xenia Manager");
        return false;
    }

    #endregion
}