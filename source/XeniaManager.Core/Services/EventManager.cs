using Avalonia.Threading;

namespace XeniaManager.Core.Services;

/// <summary>
/// Centralized event manager for application-wide events.
/// Allows components to trigger actions without direct dependencies.
/// </summary>
public class EventManager
{
    // Singleton instance
    private static EventManager? _instance;
    public static EventManager Instance => _instance ??= new EventManager();

    // Window state events
    public event Action<bool>? WindowDisabled;

    // Game library events
    public event Action? GameLibraryChanged;

    /// <summary>
    /// Triggers the window disable/enable event on the UI thread.
    /// </summary>
    /// <param name="isDisabled">True to disable the window, false to enable it.</param>
    public void SetWindowDisabled(bool isDisabled)
    {
        if (WindowDisabled == null)
        {
            return;
        }

        // If we're already on the UI thread, invoke directly
        // Otherwise, marshal to the UI thread
        if (Dispatcher.UIThread.CheckAccess())
        {
            WindowDisabled.Invoke(isDisabled);
        }
        else
        {
            Dispatcher.UIThread.InvokeAsync(() => WindowDisabled.Invoke(isDisabled), DispatcherPriority.Send);
        }
    }

    /// <summary>
    /// Disables the window.
    /// </summary>
    public void DisableWindow()
    {
        SetWindowDisabled(true);
    }

    /// <summary>
    /// Enables the window.
    /// </summary>
    public void EnableWindow()
    {
        SetWindowDisabled(false);
    }

    /// <summary>
    /// Triggers the game library changed event.
    /// </summary>
    public void OnGameLibraryChanged()
    {
        GameLibraryChanged?.Invoke();
    }
}