using XeniaManager.Core;

namespace XeniaManager.Desktop.Utilities;

public static class EventManager
{
    /// <summary>
    /// Event that is triggered when the game library needs to be refreshed
    /// </summary>
    public static event EventHandler LibraryUIiRefresh;

    /// <summary>
    /// Event that is triggered when Xenia settings need to be refreshed
    /// </summary>
    public static event EventHandler XeniaSettingsRefresh;

    /// <summary>
    /// Request a refresh of the game library UI
    /// </summary>
    public static void RequestLibraryUiRefresh()
    {
        Logger.Info("Library refresh requested");
        LibraryUIiRefresh?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Request a refresh of the Xenia settings UI
    /// </summary>
    public static void RequestXeniaSettingsRefresh()
    {
        Logger.Info("Xenia settings refresh requested");
        XeniaSettingsRefresh?.Invoke(null, EventArgs.Empty);
    }
}