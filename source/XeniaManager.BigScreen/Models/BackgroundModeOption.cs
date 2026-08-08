namespace XeniaManager.BigScreen.Models;

/// <summary>
/// A displayable background mode option for the settings dropdown.
/// </summary>
public class BackgroundModeOption
{
    /// <summary>
    /// The background mode value.
    /// </summary>
    public BackgroundMode Mode { get; }

    /// <summary>
    /// Human-readable name shown in the dropdown.
    /// </summary>
    public string DisplayName { get; }

    public BackgroundModeOption(BackgroundMode mode, string displayName)
    {
        Mode = mode;
        DisplayName = displayName;
    }
}
