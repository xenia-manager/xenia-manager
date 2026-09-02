namespace XeniaManager.BigScreen.Models.Settings;

/// <summary>
/// A displayable time format option for the settings dropdown.
/// </summary>
public class TimeFormatOption(TimeFormat format, string displayName)
{
    /// <summary>
    /// The time format value.
    /// </summary>
    public TimeFormat Format { get; } = format;

    /// <summary>
    /// Human-readable name shown in the dropdown.
    /// </summary>
    public string DisplayName { get; } = displayName;
}