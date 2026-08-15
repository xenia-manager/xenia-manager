namespace XeniaManager.BigScreen.Constants;

/// <summary>
/// Boot pipeline progress values, kept in sync with the splash stage order:
/// each stage reports its start value, then reports increments while its work
/// progresses (the increment is added to the stage's start value).
/// </summary>
public static class SplashStages
{
    /// <summary>
    /// Settings loading stage start (also restores the primary controller).
    /// </summary>
    public const double LoadingSettings = 0.10;

    /// <summary>
    /// Profile loading stage start.
    /// </summary>
    public const double LoadingProfile = 0.25;

    /// <summary>
    /// Dashboard (recent games) loading stage start.
    /// </summary>
    public const double LoadingDashboard = 0.35;

    /// <summary>
    /// Library loading stage start.
    /// </summary>
    public const double LoadingLibrary = 0.45;

    /// <summary>
    /// Per-game progress increment while the library loads.
    /// </summary>
    public const double LoadingLibraryIncrement = 0.17;

    /// <summary>
    /// Game data preload stage start.
    /// </summary>
    public const double LoadingGameData = 0.66;

    /// <summary>
    /// Per-game progress increment while game data preloads.
    /// </summary>
    public const double LoadingGameDataIncrement = 0.12;

    /// <summary>
    /// Gallery loading stage start.
    /// </summary>
    public const double LoadingGallery = 0.85;

    /// <summary>
    /// The final "loading done" value.
    /// </summary>
    public const double Done = 1.0;
}
