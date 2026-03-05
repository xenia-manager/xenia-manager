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

    /// <summary>
    /// Triggers the window disable/enable event.
    /// </summary>
    /// <param name="isDisabled">True to disable the window, false to enable it.</param>
    public void SetWindowDisabled(bool isDisabled)
    {
        WindowDisabled?.Invoke(isDisabled);
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
}
