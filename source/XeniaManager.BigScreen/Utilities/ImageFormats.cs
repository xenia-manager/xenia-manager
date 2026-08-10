using System.Linq;

namespace XeniaManager.BigScreen.Utilities;

/// <summary>
/// Image formats recognized by the media gallery and the background image picker.
/// </summary>
public static class ImageFormats
{
    /// <summary>
    /// Common image extensions (lowercase, with leading dots) accepted as screenshots.
    /// </summary>
    public static readonly string[] ScreenshotExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".webp", ".gif"];

    /// <summary>
    /// The same formats as file-picker patterns (asterisk-prefixed).
    /// </summary>
    public static string[] FilePickerPatterns => ScreenshotExtensions.Select(e => $"*{e}").ToArray();
}
