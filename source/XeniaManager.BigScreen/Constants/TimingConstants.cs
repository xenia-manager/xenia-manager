using System;

namespace XeniaManager.BigScreen.Constants;

/// <summary>
/// Polling and update intervals.
/// </summary>
public static class TimingConstants
{
    /// <summary>
    /// How often the gamepad event queue is drained.
    /// </summary>
    public static readonly TimeSpan GamepadPollInterval = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// How often the header clock text is refreshed.
    /// </summary>
    public static readonly TimeSpan ClockUpdateInterval = TimeSpan.FromSeconds(1);
}
