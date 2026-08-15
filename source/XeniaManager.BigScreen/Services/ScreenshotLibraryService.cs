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
/// Scans the Canary screenshots folder (recursively, per-game subfolders) and
/// builds the gallery items, matching each screenshot to a library game.
/// </summary>
public class ScreenshotLibraryService : IScreenshotLibraryService
{
    /// <summary>
    /// All screenshots found in the Canary screenshots folder.
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
    /// Scans the screenshots folder and builds the gallery items.
    /// </summary>
    public void Load()
    {
        string screenshotsFolder = AppPathResolver.GetFullPath(
            XeniaVersionInfo.GetXeniaVersionInfo(XeniaVersion.Canary).ScreenshotsFolderLocation);

        List<ScreenshotItemViewModel> screenshots = [];
        if (Directory.Exists(screenshotsFolder))
        {
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

        Screenshots = screenshots;
    }
}