namespace XeniaManager.BigScreen.Models.Settings;

/// <summary>
/// A displayable background mode option for the settings dropdown.
/// </summary>
public class BackgroundModeOption(BackgroundMode mode, string displayName)
{
    /// <summary>
    /// The background mode value.
    /// </summary>
    public BackgroundMode Mode { get; } = mode;

    /// <summary>
    /// Human-readable name shown in the dropdown.
    /// </summary>
    public string DisplayName { get; } = displayName;
}