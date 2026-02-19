namespace XeniaManager.Core.Models.Gpd;

/// <summary>
/// Represents the namespace types used in XDBF/GPD entry tables.
/// Each namespace categorizes different types of data stored in the file.
/// </summary>
public enum EntryNamespace : ushort
{
    /// <summary>
    /// Achievement entries containing unlock status, gamerscore, and descriptions.
    /// </summary>
    Achievement = 1,

    /// <summary>
    /// Image entries containing PNG image data (icons, gamer pictures, etc.).
    /// </summary>
    Image = 2,

    /// <summary>
    /// Setting entries containing profile configuration data.
    /// </summary>
    Setting = 3,

    /// <summary>
    /// Title entries containing game-specific information.
    /// </summary>
    Title = 4,

    /// <summary>
    /// String entries containing text data (gamer card motto, etc.).
    /// </summary>
    String = 5,

    /// <summary>
    /// Achievement Security entries (created by GFWL for offline unlocked achievements).
    /// </summary>
    AchievementSecurity = 6,

    /// <summary>
    /// Avatar Award entries (Xbox 360 only, stored within PEC files).
    /// </summary>
    AvatarAward = 6
}
