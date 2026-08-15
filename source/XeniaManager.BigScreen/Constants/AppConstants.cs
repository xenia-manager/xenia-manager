namespace XeniaManager.BigScreen.Constants;

/// <summary>
/// Application-level constants.
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// File name of the base Xenia Manager executable, launched on quit.
    /// </summary>
    public const string BaseAppExecutable = "XeniaManager.exe";

    /// <summary>
    /// How many games the dashboard's recent-games row shows.
    /// </summary>
    public const int RecentGamesLimit = 8;

    /// <summary>
    /// File name of the persisted dashboard settings (next to the executable).
    /// </summary>
    public const string SettingsFileName = "dashboard-settings.json";

    /// <summary>
    /// Config section holding the fullscreen toggle.
    /// </summary>
    public const string ConfigFullscreenSection = "Display";

    /// <summary>
    /// Config option forcing a game to launch fullscreen.
    /// </summary>
    public const string ConfigFullscreenOption = "fullscreen";

    /// <summary>
    /// Config section holding the auto-sign-in profile slots.
    /// </summary>
    public const string ConfigProfilesSection = "Profiles";

    /// <summary>
    /// Config option naming the profile XUID to sign in on boot (slot 0).
    /// </summary>
    public const string ConfigProfileSlotOption = "logged_profile_slot_0_xuid";
}