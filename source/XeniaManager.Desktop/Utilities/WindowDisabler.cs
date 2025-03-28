using System.Windows;

namespace XeniaManager.Desktop.Utilities;

public class WindowDisabler : IDisposable
{
    private readonly Window _window;
    private readonly bool _previousState;

    public WindowDisabler(FrameworkElement element)
    {
        // Get the parent window from the given element
        _window = Window.GetWindow(element) ?? throw new ArgumentNullException(nameof(element), "No parent window found.");
        // Store the previous state so it can be restored later
        _previousState = _window.IsEnabled;
        _window.IsEnabled = false;
    }

    public void Dispose()
    {
        // Restore the original state
        _window.IsEnabled = _previousState;
    }
}