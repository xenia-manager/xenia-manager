using Octokit;
using XeniaManager.Core.Downloader;

namespace XeniaManager.Core.Game;

public static class PatchManager
{
    private static DownloadManager _downloadManager { get; set; } = new DownloadManager();

    public static async Task DownloadPatch(Game game, RepositoryContent selectedPatch)
    {
        string emulatorLocation = game.XeniaVersion switch
        {
            XeniaVersion.Canary => Constants.Xenia.Canary.EmulatorDir,
            _ => throw new NotImplementedException($"This version of Xenia isn't supported")
        };
        Logger.Debug($"Emulator location: {emulatorLocation}");
        Logger.Debug($"Patch URL: {selectedPatch.DownloadUrl}");
        await _downloadManager.DownloadFileAsync(selectedPatch.DownloadUrl, Path.Combine(Constants.DirectoryPaths.Base, emulatorLocation, "Patches", selectedPatch.Name));
        game.FileLocations.Patch = Path.Combine(emulatorLocation, "Patches", selectedPatch.Name);
        Logger.Info($"{game.Title} patch has been installed.");
        GameManager.SaveLibrary();
    }
}