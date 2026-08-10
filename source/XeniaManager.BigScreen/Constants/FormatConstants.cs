namespace XeniaManager.BigScreen.Constants;

/// <summary>
/// Display and serialization format strings.
/// </summary>
public static class FormatConstants
{
    /// <summary>
    /// Header clock format (e.g. "10:45 PM").
    /// </summary>
    public const string ClockFormat = "hh:mm tt";

    /// <summary>
    /// Screenshot capture date format (e.g. "5 Aug 2026, 14:30").
    /// </summary>
    public const string CaptureDateFormat = "d MMM yyyy, HH:mm";

    /// <summary>
    /// XUID serialization format (16 uppercase hex digits).
    /// </summary>
    public const string XuidFormat = "X16";
}
