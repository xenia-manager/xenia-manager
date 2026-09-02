namespace XeniaManager.BigScreen.Models.Settings;

/// <summary>
/// The available background modes for the dashboard.
/// </summary>
public enum BackgroundMode
{
    /// <summary>
    /// The currently selected game's artwork, falling back to the radial gradient.
    /// </summary>
    Dynamic,

    /// <summary>
    /// A user-selected image stretched to fill the screen.
    /// </summary>
    Image,

    /// <summary>
    /// A single solid colour.
    /// </summary>
    Solid,

    /// <summary>
    /// A linear gradient.
    /// </summary>
    LinearGradient,

    /// <summary>
    /// A radial gradient.
    /// </summary>
    RadialGradient
}