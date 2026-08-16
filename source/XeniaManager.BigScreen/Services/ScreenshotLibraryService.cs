using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Scans every installed emulator version's screenshots folder (recursively,
/// per-game subfolders) and builds the gallery items, matching each screenshot
/// to a library game and tagging it with its source version.
/// </summary>
public class ScreenshotLibraryService : IScreenshotLibraryService
{
    /// <summary>
    /// All screenshots found in every installed emulator's screenshots folder.
    /// </summary>
    public IReadOnlyList<ScreenshotItemViewModel> Screenshots { get; private set; } = [];

    /// <summary>
    /// Matches a screenshot's parent folder name (a game ID) to a library game title.
    /// </summary>
    private static string ResolveGameTitle(string gameId)
    {
        if (string.IsNullOrEmpty(gameId))
        {
            return LocalizationHelper.GetText("Gallery.UnknownGame");
        }

        return GameManager.Games
                   .FirstOrDefault(g => g.GameId.Equals(gameId, StringComparison.OrdinalIgnoreCase))?.Title
               ?? gameId;
    }

    /// <summary>
    /// Scans one installed version's screenshots folder and appends its items.
    /// </summary>
    private static void ScanVersion(XeniaVersion version, List<ScreenshotItemViewModel> screenshots)
    {
        string screenshotsFolder = AppPathResolver.GetFullPath(
            XeniaVersionInfo.GetXeniaVersionInfo(version).ScreenshotsFolderLocation);

        if (!Directory.Exists(screenshotsFolder))
        {
            return;
        }

        foreach (string file in Directory.EnumerateFiles(screenshotsFolder, "*", SearchOption.AllDirectories)
                     .Where(f => ImageFormats.ScreenshotExtensions.Contains(Path.GetExtension(f)
                         .ToLowerInvariant())))
        {
            try
            {
                string fileName = Path.GetFileName(file);
                string gameId = ScreenshotFileNameParser.ExtractGameId(fileName);
                if (gameId.Length == 0)
                {
                    gameId = Path.GetFileName(Path.GetDirectoryName(file) ?? string.Empty);
                }

                DateTime capturedAt = ScreenshotFileNameParser.ExtractCapturedAt(fileName)
                                      ?? File.GetLastWriteTime(file);
                screenshots.Add(new ScreenshotItemViewModel(
                    version,
                    file,
                    fileName,
                    capturedAt,
                    ResolveGameTitle(gameId),
                    new Bitmap(file)));
            }
            catch (Exception ex)
            {
                Logger.Warning<ScreenshotLibraryService>($"Failed to load screenshot '{file}', skipping");
                Logger.LogExceptionDetails<ScreenshotLibraryService>(ex);
            }
        }
    }

    /// <summary>
    /// Scans the screenshots folder of every installed emulator version and
    /// builds the gallery items (Custom is never scanned - it has no standard
    /// folder).
    /// </summary>
    public void Load()
    {
        Core.Settings.Settings desktopSettings = new();
        List<ScreenshotItemViewModel> screenshots = [];
        foreach (XeniaVersion version in desktopSettings.GetInstalledVersions(desktopSettings))
        {
            ScanVersion(version, screenshots);
        }

        Screenshots = screenshots;
    }
}