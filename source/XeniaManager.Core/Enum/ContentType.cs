namespace XeniaManager.Core.Enum;

public enum ContentType
{
    SavedGame = 0x00000001,
    DownloadableContent = 0x00000002,
    // Publisher = 0x00000003,
    // Xbox360Title = 0x00001000,
    InstalledGame = 0x00040000,
    // XboxOriginalGame = 0x00050000,
    GameOnDemand = 0x00070000,
    // AvatarItem = 0x00090000,
    // Profile = 0x00100000,
    // GamerPicture = 0x00200000,
    // Theme = 0x00300000,
    // StorageDownload = 0x00500000,
    // XboxSavedGame = 0x00600000,
    // XboxDownload = 0x00700000,
    // GameDemo = 0x00800000,
    GameTitle = 0x00A00000,
    Installer = 0x00B00000,
    ArcadeTitle = 0x00D00000
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