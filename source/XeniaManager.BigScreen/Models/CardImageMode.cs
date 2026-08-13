namespace XeniaManager.BigScreen.Models;

/// <summary>
/// The image shown on dashboard game cards.
/// </summary>
public enum CardImageMode
{
    /// <summary>
    /// The game's box art (with the disc icon as fallback when missing).
    /// </summary>
    BoxArt,

    /// <summary>
    /// The game's disc icon.
    /// </summary>
    Icon,
}
