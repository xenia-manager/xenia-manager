namespace XeniaManager.BigScreen.Models;

/// <summary>
/// The available background modes for the dashboard.
/// </summary>
public enum BackgroundMode
{
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
    RadialGradient,

    /// <summary>
    /// The currently selected game's artwork, falling back to the linear gradient.
    /// </summary>
    Dynamic,
}
