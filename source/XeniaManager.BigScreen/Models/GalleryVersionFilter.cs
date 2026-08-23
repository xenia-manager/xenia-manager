using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Models;

namespace XeniaManager.BigScreen.Models;

/// <summary>
/// A gallery version filter option: "All" (null version) or one installed
/// emulator version with screenshots.
/// </summary>
public record GalleryVersionFilter(XeniaVersion? Version, string DisplayName)
{
    /// <summary>
    /// Whether the given screenshot passes this filter.
    /// </summary>
    public bool Matches(ScreenshotItemViewModel screenshot) => Version == null || screenshot.Version == Version;
}