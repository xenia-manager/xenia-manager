using System.Text.Json;

// Imported Libraries
using Octokit;
using XeniaManager.Core.Constants;

namespace XeniaManager.Core;

public static class ManagerUpdater
{
    /// <summary>
    /// HttpClient used to grab the database
    /// </summary>
    private static readonly HttpClientService _client = new HttpClientService();

    #region Functions

    private static async Task<string> GetLatestVersion(bool experimental = false)
    {
        string response = await _client.GetAsync(Urls.LatestXeniaManagerVersions);
        using JsonDocument document = JsonDocument.Parse(response);
        JsonElement root = document.RootElement;
        string build = experimental ? "experimental" : "stable";
        return root.TryGetProperty(build, out JsonElement version) ? version.GetString() ?? string.Empty : string.Empty;
    }

    public static async Task<bool> CheckForUpdates(string currentVersion, string repositoryOwner = "xenia-manager", string repositoryName = "xenia-manager")
    {
        // Try without using GitHub API first
        bool isExperimental = repositoryName == "experimental-builds";
        string latestVersion = await GetLatestVersion(isExperimental);
        if (!string.IsNullOrEmpty(latestVersion))
        {
            Logger.Debug($"Latest Xenia Manager version (Cached GitHub API): {latestVersion}");
            if (!string.Equals(latestVersion, currentVersion, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Info("There is a update for Xenia Manager");
                return true;
            }
            Logger.Info("There is no update for Xenia Manager");
            return false;
        }

        // Using GitHub API
        Release latestRelease = await Github.GetLatestRelease(repositoryOwner, repositoryName);
        Logger.Debug($"Latest Xenia Manager version (GitHub API): {latestRelease.TagName}");
        bool isUpdate = !string.Equals(latestRelease.TagName, currentVersion, StringComparison.OrdinalIgnoreCase);
        Logger.Info(isUpdate ? "There is an update for Xenia Manager" : "There is no update for Xenia Manager");

        return isUpdate;
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