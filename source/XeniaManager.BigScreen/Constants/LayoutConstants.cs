namespace XeniaManager.BigScreen.Constants;

/// <summary>
/// Layout and appearance constants.
/// </summary>
public static class LayoutConstants
{
    /// <summary>
    /// Vignette opacity step per adjustment press.
    /// </summary>
    public const double VignetteStep = 0.05;

    /// <summary>
    /// Fallback width of a library carousel card before the first real layout.
    /// </summary>
    public const double LibraryCardDefaultWidth = 420;

    /// <summary>
    /// Fallback spacing between library carousel cards before the first real layout.
    /// </summary>
    public const double LibraryCardSpacing = 25;

    /// <summary>
    /// Fallback height of a library list row before the first real layout.
    /// </summary>
    public const double LibraryListRowHeight = 96;

    /// <summary>
    /// Fallback spacing between library list rows before the first real layout.
    /// </summary>
    public const double LibraryListRowSpacing = 16;

    /// <summary>
    /// How far gradient shades are mixed toward black at the mid stop.
    /// </summary>
    public const double GradientMixAmount = 0.12;

    /// <summary>
    /// How far gradient shades are mixed toward black at the end stop.
    /// </summary>
    public const double GradientEndMixAmount = 0.25;

    /// <summary>
    /// Offset of the mid gradient stop (linear gradient).
    /// </summary>
    public const double LinearMidOffset = 0.5;

    /// <summary>
    /// Offset of the mid gradient stop (radial gradient).
    /// </summary>
    public const double RadialMidOffset = 0.55;

    /// <summary>
    /// Offset where the vignette starts fading to black.
    /// </summary>
    public const double VignetteInnerStop = 0.75;

    /// <summary>
    /// Per-step tint amount used to derive the accent light/dark variants.
    /// </summary>
    public const double AccentTintStep = 0.15;

    /// <summary>
    /// Minimum header clock width in 12-hour mode (e.g. "10:45 PM").
    /// </summary>
    public const double ClockMinWidth12H = 70;

    /// <summary>
    /// Minimum header clock width in 24-hour mode (e.g. "22:45").
    /// </summary>
    public const double ClockMinWidth24H = 48;

    /// <summary>
    /// Dashboard game tile width (unselected).
    /// </summary>
    public const double DashboardCardWidth = 185;

    /// <summary>
    /// Dashboard game tile width when selected (grows; width is the primary
    /// emphasis so the carousel stays compact vertically).
    /// </summary>
    public const double DashboardCardSelectedWidth = 220;

    /// <summary>
    /// Dashboard game tile height in Icon mode (unselected, square).
    /// </summary>
    public const double DashboardCardIconHeight = 185;

    /// <summary>
    /// Dashboard game tile height in Icon mode when selected (uniform growth -
    /// stays square).
    /// </summary>
    public const double DashboardCardIconSelectedHeight = 220;

    /// <summary>
    /// Dashboard game tile height in Box Art mode (unselected). Keeps the
    /// original 1.19 width:height ratio scaled to the current tile width.
    /// </summary>
    public const double DashboardCardBoxArtHeight = 220;

    /// <summary>
    /// Dashboard game tile height in Box Art mode when selected (same 1.19 ratio).
    /// </summary>
    public const double DashboardCardBoxArtSelectedHeight = 262;

    /// <summary>
    /// Dashboard game tile art inset (2px card margin on both sides).
    /// </summary>
    public const double DashboardCardArtMargin = 4;
}