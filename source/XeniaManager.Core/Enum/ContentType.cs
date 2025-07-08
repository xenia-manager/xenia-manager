namespace XeniaManager.Core.Enum;

public enum ContentType
{
    SavedGame = 0x0000001,
    DownloadableContent = 0x0000002,
    // Publisher = 0x0000003,
    // Xbox360Title = 0x0001000,
    InstalledGame = 0x0004000,
    // XboxOriginalGame = 0x0005000,
    GameOnDemand = 0x0070000,
    // StorageDownload = 0x0050000,
    // XboxSavedGame = 0x0060000,
    // XboxDownload = 0x0070000,
    // GameDemo = 0x0080000,
    GameTitle = 0x00A0000,
    Installer = 0x00B0000,
    ArcadeTitle = 0x00D0000
}

public static class ContentTypeExtensions
{
    /// <summary>
    /// Converts the ContentType enum to a hex string representation.
    /// </summary>
    /// <param name="folderType">The ContentType to convert.</param>
    /// <returns>A string representing the hex value of the ContentType.</returns>
    public static string ToHexString(this ContentType folderType)
    {
        return ((int)folderType).ToString("X8");
    }
}