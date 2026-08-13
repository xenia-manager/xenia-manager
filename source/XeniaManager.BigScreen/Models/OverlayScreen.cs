namespace XeniaManager.BigScreen.Models;

/// <summary>
/// The overlay screens accessible from the dashboard option cards.
/// </summary>
public enum OverlayScreen
{
    /// <summary>
    /// No overlay is open - the dashboard is visible.
    /// </summary>
    None,

    /// <summary>
    /// The game library screen.
    /// </summary>
    Library,

    /// <summary>
    /// The media/screenshots screen.
    /// </summary>
    Media,

    /// <summary>
    /// The dashboard settings screen.
    /// </summary>
    Settings,
}