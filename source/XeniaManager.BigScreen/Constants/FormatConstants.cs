using XeniaManager.BigScreen.Models.Settings;

namespace XeniaManager.BigScreen.Constants;

/// <summary>
/// Display and serialization format strings.
/// </summary>
public static class FormatConstants
{
    /// <summary>
    /// Header clock format, 12-hour (e.g. "10:45 PM").
    /// </summary>
    public const string ClockFormat12h = "hh:mm tt";

    /// <summary>
    /// Header clock format, 24-hour (e.g. "22:45").
    /// </summary>
    public const string ClockFormat24h = "HH:mm";

    /// <summary>
    /// Screenshot capture date format, 12-hour (e.g. "5 Aug 2026, 2:30 PM").
    /// </summary>
    public const string CaptureDateFormat12h = "d MMM yyyy, hh:mm tt";

    /// <summary>
    /// Screenshot capture date format, 24-hour (e.g. "5 Aug 2026, 14:30").
    /// </summary>
    public const string CaptureDateFormat24h = "d MMM yyyy, HH:mm";

    /// <summary>
    /// XUID serialization format (16 uppercase hex digits).
    /// </summary>
    public const string XuidFormat = "X16";

    /// <summary>
    /// Returns the clock format for the given time format setting.
    /// </summary>
    public static string GetClockFormat(TimeFormat timeFormat) =>
        timeFormat == TimeFormat.TwentyFourHour ? ClockFormat24h : ClockFormat12h;

    /// <summary>
    /// Returns the capture date format for the given time format setting.
    /// </summary>
    public static string GetCaptureDateFormat(TimeFormat timeFormat) =>
        timeFormat == TimeFormat.TwentyFourHour ? CaptureDateFormat24h : CaptureDateFormat12h;
}