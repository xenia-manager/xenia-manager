using XeniaManager.Core;

namespace XeniaManager.Desktop.Utilities;

public static class EventManager
{
    /// <summary>
    /// Event that is triggered when the game library needs to be refreshed
    /// </summary>
    public static event EventHandler LibraryUIiRefresh;

    /// <summary>
    /// Request a refresh of the game library UI
    /// </summary>
    public static void RequestLibraryUiRefresh()
    {
        Logger.Info("Library refresh requested");
        LibraryUIiRefresh?.Invoke(null, EventArgs.Empty);
    }
}