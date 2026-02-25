using System.Text.RegularExpressions;

namespace XeniaManager.Core.Models.Files.Stfs;

/// <summary>
/// Represents the different types of content that can be stored on Xbox 360 storage devices.
/// These values correspond to the content type field in Xbox 360 content headers.
/// </summary>
public enum ContentType : uint
{
    /// <summary>
    /// A saved game file containing player progress and game state.
    /// </summary>
    SavedGame = 0x00000001,

    /// <summary>
    /// Content downloaded from the Xbox Live Marketplace.
    /// </summary>
    MarketplaceContent = 0x00000002,

    /// <summary>
    /// Content published by a developer or publisher.
    /// </summary>
    Publisher = 0x00000003,

    /// <summary>
    /// An Xbox 360 title/game.
    /// </summary>
    Xbox360Title = 0x00010000,

    /// <summary>
    /// IPTV pause buffer content.
    /// </summary>
    IPTVPauseBuffer = 0x00020000,

    /// <summary>
    /// A game that has been installed to the console's storage device.
    /// </summary>
    InstalledGame = 0x00040000,

    /// <summary>
    /// An original Xbox game (backward-compatible titles).
    /// Note: This shares the same value as XboxTitle.
    /// </summary>
    XboxOriginalGame = 0x00050000,

    /// <summary>
    /// An original Xbox title (backward compatible).
    /// Note: This shares the same value as XboxOriginalGame.
    /// </summary>
    XboxTitle = 0x00050000,

    /// <summary>
    /// A game available for download on demand.
    /// </summary>
    GameOnDemand = 0x00070000,

    /// <summary>
    /// An avatar-related item or accessory.
    /// </summary>
    AvatarItem = 0x00090000,

    /// <summary>
    /// A user profile containing account information and settings.
    /// </summary>
    Profile = 0x00100000,

    /// <summary>
    /// A gamer picture (avatar image) associated with a profile.
    /// </summary>
    GamerPicture = 0x00200000,

    /// <summary>
    /// A theme containing visual customization elements.
    /// </summary>
    Theme = 0x00300000,

    /// <summary>
    /// A cache file used for temporary data storage.
    /// </summary>
    CacheFile = 0x00400000,

    /// <summary>
    /// Content downloaded to a storage device.
    /// </summary>
    StorageDownload = 0x00500000,

    /// <summary>
    /// A saved game from an original Xbox console.
    /// </summary>
    XboxSavedGame = 0x00600000,

    /// <summary>
    /// Downloaded content from Xbox Live for original Xbox.
    /// </summary>
    XboxDownload = 0x00700000,

    /// <summary>
    /// A demo version of a game.
    /// </summary>
    GameDemo = 0x00800000,

    /// <summary>
    /// Generic video content.
    /// </summary>
    Video = 0x00900000,

    /// <summary>
    /// A game title package.
    /// </summary>
    GameTitle = 0x00A00000,

    /// <summary>
    /// An installer package for content installation.
    /// </summary>
    Installer = 0x00B00000,

    /// <summary>
    /// A trailer for a game.
    /// </summary>
    GameTrailer = 0x00C00000,

    /// <summary>
    /// An arcade title.
    /// </summary>
    ArcadeTitle = 0x00D00000,

    /// <summary>
    /// XNA Game Studio content.
    /// </summary>
    XNA = 0x00E00000,

    /// <summary>
    /// A license store containing licensing information.
    /// </summary>
    LicenseStore = 0x00F00000,

    /// <summary>
    /// A movie content item.
    /// </summary>
    Movie = 0x01000000,

    /// <summary>
    /// Television show content.
    /// </summary>
    TV = 0x02000000,

    /// <summary>
    /// A music video content item.
    /// </summary>
    MusicVideo = 0x03000000,

    /// <summary>
    /// A video related to a game.
    /// </summary>
    GameVideo = 0x04000000,

    /// <summary>
    /// A podcast video content item.
    /// </summary>
    PodcastVideo = 0x05000000,

    /// <summary>
    /// A viral video content item.
    /// </summary>
    ViralVideo = 0x06000000,

    /// <summary>
    /// A community game or user-generated content.
    /// </summary>
    CommunityGame = 0x20000000
}

/// <summary>
/// Extension methods for the ContentType enum to provide additional functionality.
/// </summary>
public static class ContentTypeExtensions
{
    /// <summary>
    /// Converts the ContentType enum value to its hexadecimal string representation.
    /// </summary>
    /// <param name="type">The ContentType to convert.</param>
    /// <returns>The hexadecimal string representation of the ContentType value.</returns>
    public static string ToHexString(this ContentType type)
    {
        return ((uint)type).ToString("X8");
    }

    /// <summary>
    /// Converts the ContentType enum value to a display-friendly string with spaces between words.
    /// </summary>
    /// <param name="type">The ContentType to convert.</param>
    /// <returns>A display-friendly string representation of the ContentType.</returns>
    public static string ToDisplayString(this ContentType type)
    {
        string name = type.ToString();

        // Insert spaces before capital letters (except the first letter)
        return Regex.Replace(name, "(?<!^)([A-Z])", " $1");
    }
}