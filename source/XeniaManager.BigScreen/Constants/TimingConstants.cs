using System;

namespace XeniaManager.BigScreen.Constants;

/// <summary>
/// Polling, update and animation intervals.
/// </summary>
public static class TimingConstants
{
    /// <summary>
    /// How often the header clock text is refreshed.
    /// </summary>
    public static readonly TimeSpan ClockUpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How often the wifi connection state is re-checked.
    /// </summary>
    public static readonly TimeSpan WifiPollInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Duration of one artwork fade leg (out to the static base, or in from it).
    /// </summary>
    public static readonly TimeSpan ArtFadeDuration = TimeSpan.FromMilliseconds(300);

    /// <summary>
    /// Minimum time each splash loading stage stays visible.
    /// </summary>
    public static readonly TimeSpan StageDwell = TimeSpan.FromMilliseconds(400);

    /// <summary>
    /// Minimum time "Loading Done" stays visible before the dashboard appears.
    /// </summary>
    public static readonly TimeSpan DoneDwell = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Minimum total time the splash stays visible.
    /// </summary>
    public static readonly TimeSpan SplashMinimumShowTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// How often the library load progress is reported while cards are built
    /// (every Nth game).
    /// </summary>
    public const int ProgressReportInterval = 10;
}