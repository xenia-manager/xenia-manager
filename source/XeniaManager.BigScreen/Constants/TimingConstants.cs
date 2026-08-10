using System;

namespace XeniaManager.BigScreen.Constants;

/// <summary>
/// Polling, update and animation intervals.
/// </summary>
public static class TimingConstants
{
    /// <summary>
    /// How often the gamepad event queue is drained.
    /// </summary>
    public static readonly TimeSpan GamepadPollInterval = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// How often the gamepad battery state is queried.
    /// </summary>
    public static readonly TimeSpan BatteryPollInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How often the header clock text is refreshed.
    /// </summary>
    public static readonly TimeSpan ClockUpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How often the wifi connection state is re-checked.
    /// </summary>
    public static readonly TimeSpan WifiPollInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Duration of one background fade leg (out to black, or in from black).
    /// </summary>
    public static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(180);

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
    public static readonly TimeSpan SplashMinimumShowTime = TimeSpan.FromSeconds(3);
}
