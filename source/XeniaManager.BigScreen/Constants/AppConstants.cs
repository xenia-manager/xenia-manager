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
}