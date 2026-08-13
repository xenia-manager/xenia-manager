namespace XeniaManager.BigScreen.Models.Settings;

/// <summary>
/// A displayable library view mode option for the settings dropdown.
/// </summary>
public class LibraryViewModeOption(LibraryViewMode mode, string displayName)
{
    /// <summary>
    /// The library view mode value.
    /// </summary>
    public LibraryViewMode Mode { get; } = mode;

    /// <summary>
    /// Human-readable name shown in the dropdown.
    /// </summary>
    public string DisplayName { get; } = displayName;
}