using System.Windows;

namespace XeniaManager.Desktop.Utilities;

/// <summary>
/// Blocks UI interaction in async functions
/// </summary>
public class WindowDisabler : IDisposable
{
    // Variables
    /// <summary>
    /// Window whose UI interaction is being blocked
    /// </summary>
    private readonly Window _window;
    
    /// <summary>
    /// State of the window before it's interaction was blocked
    /// </summary>
    private readonly bool _previousState;

    // Constructor
    public WindowDisabler(FrameworkElement element)
    {
        // Get the parent window from the given element
        _window = Window.GetWindow(element) ?? throw new ArgumentNullException(nameof(element), "No parent window found.");
        // Store the previous state so it can be restored later
        _previousState = _window.IsEnabled;
        _window.IsEnabled = false;
    }

    /// <summary>
    /// Returns the window in it's previous state
    /// </summary>
    public void Dispose()
    {
        // Restore the original state
        _window.IsEnabled = _previousState;
    }
}