namespace XeniaManager.BigScreen.Models;

/// <summary>
/// The kinds of rows on the settings screen, in display order. Each fixed row
/// maps to one card; <see cref="Gamepad"/> rows map to the connected-controller
/// cards beneath the Controllers section header.
/// </summary>
public enum SettingsRowKind
{
    ManageProfiles,
    LibraryView,
    CardImage,
    TimeFormat,
    QuitToggle,
    BackgroundMode,
    PrimaryColour,
    AccentColour,
    Vignette,
    BackgroundImage,
    Gamepad
}
