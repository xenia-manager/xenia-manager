using System;

namespace XeniaManager.BigScreen.Utilities;

/// <summary>
/// Shared card sizing for the screenshot grids (gallery and game screenshots pane).
/// </summary>
public static class ScreenshotGridLayout
{
    /// <summary>
    /// How many cards fit on one row.
    /// </summary>
    public const int CardsPerRow = 4;

    /// <summary>
    /// The widest a card may get (16:9). Cards shrink to fit <see cref="CardsPerRow"/> per row.
    /// </summary>
    public const double MaxCardWidth = 384;

    /// <summary>
    /// The gap between cards and rows (matches the WrapPanel spacing).
    /// </summary>
    public const double ItemSpacing = 16;

    /// <summary>
    /// The card's 16:9 aspect ratio (height / width).
    /// </summary>
    public const double AspectRatio = 9.0 / 16.0;

    /// <summary>
    /// Fits a card width so exactly <see cref="CardsPerRow"/> fit per row,
    /// capped at <see cref="MaxCardWidth"/>.
    /// </summary>
    public static double FitCardWidth(double viewportWidth)
    {
        return Math.Min((viewportWidth - ItemSpacing * (CardsPerRow - 1)) / CardsPerRow, MaxCardWidth);
    }

    /// <summary>
    /// The card height for the given width (16:9).
    /// </summary>
    public static double CardHeight(double width) => width * AspectRatio;
}