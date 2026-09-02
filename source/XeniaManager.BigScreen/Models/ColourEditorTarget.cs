namespace XeniaManager.BigScreen.Models;

/// <summary>
/// The element of the colour row the controller is currently focused on:
/// one of the RGB sliders or the preview swatch (which opens the palette).
/// </summary>
public enum ColourEditorTarget
{
    /// <summary>
    /// The red channel slider.
    /// </summary>
    Red,

    /// <summary>
    /// The green channel slider.
    /// </summary>
    Green,

    /// <summary>
    /// The blue channel slider.
    /// </summary>
    Blue,

    /// <summary>
    /// The preview swatch; activating it opens the palette.
    /// </summary>
    Preview
}