using System.Collections.Generic;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Scans the Canary screenshots folder and builds the gallery items.
/// </summary>
public interface IScreenshotLibraryService
{
    /// <summary>
    /// All screenshots found in the Canary screenshots folder.
    /// </summary>
    IReadOnlyList<ScreenshotItemViewModel> Screenshots { get; }

    /// <summary>
    /// Scans the screenshots folder and builds the gallery items.
    /// </summary>
    void Load();
}