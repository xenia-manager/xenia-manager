using System.Collections.Generic;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Scans every installed emulator version's screenshots folder and builds the
/// gallery items.
/// </summary>
public interface IScreenshotLibraryService
{
    /// <summary>
    /// All screenshots found in every installed emulator's screenshots folder.
    /// </summary>
    IReadOnlyList<ScreenshotItemViewModel> Screenshots { get; }

    /// <summary>
    /// Scans the screenshots folders and builds the gallery items.
    /// </summary>
    void Load();
}