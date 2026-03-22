using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using XeniaManager.Core.Extensions;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Bindings;
using AvaloniaKeyEventArgs = Avalonia.Input.KeyEventArgs;
using KeyEventArgs = XeniaManager.Core.Models.InputListener.KeyEventArgs;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Provides keyboard and mouse input listening functionality using Avalonia's input system.
/// Monitors input events within the application and raise events when keys or mouse buttons are pressed.
/// Note: This only captures input within the application window, not global system-wide input.
/// </summary>
public class InputListener
{
    // State management
    private static volatile bool _isRunning;
    private static readonly Lock LockObject = new Lock();
    private static TopLevel? _currentTopLevel;

    /// <summary>
    /// Event raised when a keyboard key is pressed.
    /// </summary>
    public static event EventHandler<KeyEventArgs>? KeyPressed;

    /// <summary>
    /// Event raised when a mouse button is pressed.
    /// </summary>
    public static event EventHandler<KeyEventArgs>? MouseClicked;

    /// <summary>
    /// Starts listening for keyboard and mouse events.
    /// Attaches to the current TopLevel window's input events.
    /// </summary>
    public static void Start()
    {
        lock (LockObject)
        {
            if (_isRunning)
            {
                Logger.Warning<InputListener>("InputListener is already running");
                return;
            }

            try
            {
                // Get the current TopLevel window
                Window? mainWindow = null;

                if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    mainWindow = desktop.MainWindow;
                }

                if (mainWindow != null)
                {
                    _currentTopLevel = TopLevel.GetTopLevel(mainWindow);
                }

                if (_currentTopLevel == null)
                {
                    Logger.Warning<InputListener>("Could not get TopLevel window. InputListener requires an active window.");
                    return;
                }

                // Subscribe to keyboard events (tunneling to catch all key presses)
                _currentTopLevel.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);

                // Subscribe to mouse events
                _currentTopLevel.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);

                // Subscribe to mouse wheel events
                _currentTopLevel.AddHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);

                _isRunning = true;
                Logger.Info<InputListener>("InputListener started successfully");
            }
            catch (Exception ex)
            {
                Logger.Error<InputListener>($"Failed to start InputListener: {ex.Message}");
                Logger.LogExceptionDetails<InputListener>(ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Stops listening for keyboard and mouse events.
    /// Detaches from the TopLevel window's input events and releases resources.
    /// </summary>
    public static void Stop()
    {
        lock (LockObject)
        {
            if (!_isRunning)
            {
                Logger.Debug<InputListener>("InputListener is not running");
                return;
            }

            try
            {
                if (_currentTopLevel != null)
                {
                    // Unsubscribe from keyboard events
                    _currentTopLevel.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);

                    // Unsubscribe from mouse events
                    _currentTopLevel.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);

                    // Unsubscribe from mouse wheel events
                    _currentTopLevel.RemoveHandler(InputElement.PointerWheelChangedEvent, OnPointerWheelChanged);

                    _currentTopLevel = null;
                }

                _isRunning = false;
                Logger.Info<InputListener>("InputListener stopped successfully");
            }
            catch (Exception ex)
            {
                Logger.Error<InputListener>($"Error occurred while stopping InputListener: {ex.Message}");
                Logger.LogExceptionDetails<InputListener>(ex);
                _isRunning = false;
                throw;
            }
        }
    }

    /// <summary>
    /// Gets whether the InputListener is currently running.
    /// </summary>
    public static bool IsRunning => _isRunning;

    /// <summary>
    /// Handles key down events from Avalonia.
    /// </summary>
    private static void OnKeyDown(object? sender, AvaloniaKeyEventArgs e)
    {
        try
        {
            // Convert Avalonia Key to VirtualKeyCode
            VirtualKeyCode? keyCode = e.Key.ToVirtualKeyCode();

            if (keyCode.HasValue && keyCode.Value != VirtualKeyCode.None)
            {
                string? xeniaKey = keyCode.Value.ToXeniaKey();

                if (!string.IsNullOrEmpty(xeniaKey))
                {
                    Logger.Debug<InputListener>($"Keyboard key pressed: {xeniaKey} (Avalonia: {e.Key})");

                    // Fire event on the thread pool to avoid blocking the UI thread
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            KeyPressed?.Invoke(null, new KeyEventArgs(xeniaKey));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error<InputListener>($"Error in KeyPressed event handler for key: {xeniaKey} - {ex.Message}");
                            Logger.LogExceptionDetails<InputListener>(ex);
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error<InputListener>($"Error in keyboard event handler: {ex.Message}");
            Logger.LogExceptionDetails<InputListener>(ex);
        }
    }

    /// <summary>
    /// Handles pointer pressed events from Avalonia.
    /// </summary>
    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        try
        {
            PointerPointProperties properties = e.GetCurrentPoint(null).Properties;
            VirtualKeyCode? button = null;

            // Check which mouse button was pressed
            if (properties.IsLeftButtonPressed)
            {
                button = VirtualKeyCode.LButton;
            }
            else if (properties.IsRightButtonPressed)
            {
                button = VirtualKeyCode.RButton;
            }
            else if (properties.IsMiddleButtonPressed)
            {
                button = VirtualKeyCode.MButton;
            }
            else if (properties.IsXButton1Pressed)
            {
                button = VirtualKeyCode.Mouse4;
            }
            else if (properties.IsXButton2Pressed)
            {
                button = VirtualKeyCode.Mouse5;
            }

            if (button.HasValue)
            {
                string? xeniaKey = button.Value.ToXeniaKey();
                if (!string.IsNullOrEmpty(xeniaKey))
                {
                    Logger.Debug<InputListener>($"Mouse button pressed: {xeniaKey}");

                    // Fire event on the thread pool to avoid blocking the UI thread
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            MouseClicked?.Invoke(null, new KeyEventArgs(xeniaKey));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error<InputListener>($"Error in MouseClicked event handler for button: {xeniaKey} - {ex.Message}");
                            Logger.LogExceptionDetails<InputListener>(ex);
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error<InputListener>($"Error in mouse event handler: {ex.Message}");
            Logger.LogExceptionDetails<InputListener>(ex);
        }
    }

    /// <summary>
    /// Handles pointer wheel changed events from Avalonia.
    /// Detects mouse wheel up and wheel down scrolling.
    /// </summary>
    private static void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        try
        {
            Vector delta = e.Delta;
            VirtualKeyCode? wheelDirection = null;

            // Check wheel direction (positive Y = wheel up, negative Y = wheel down)
            if (delta.Y > 0)
            {
                wheelDirection = VirtualKeyCode.MWheelUp;
            }
            else if (delta.Y < 0)
            {
                wheelDirection = VirtualKeyCode.MWheelDown;
            }

            if (wheelDirection.HasValue)
            {
                string? xeniaKey = wheelDirection.Value.ToXeniaKey();
                if (!string.IsNullOrEmpty(xeniaKey))
                {
                    Logger.Debug<InputListener>($"Mouse wheel scrolled: {xeniaKey} (Delta: {delta.Y})");

                    // Fire event on the thread pool to avoid blocking the UI thread
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            MouseClicked?.Invoke(null, new KeyEventArgs(xeniaKey));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error<InputListener>($"Error in MouseClicked event handler for wheel: {xeniaKey} - {ex.Message}");
                            Logger.LogExceptionDetails<InputListener>(ex);
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error<InputListener>($"Error in mouse wheel event handler: {ex.Message}");
            Logger.LogExceptionDetails<InputListener>(ex);
        }
    }
}