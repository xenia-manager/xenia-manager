/*using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Runtime.CompilerServices.RuntimeHelpers;

namespace XeniaManager.Core.Mousehook;
public static partial class InputListener
{
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    /// <summary>
    /// Identifier for low-level keyboard hook
    /// </summary>
    private const int WH_KEYBOARD_LL = 13;

    /// <summary>
    /// Identifier for low-level mouse hook
    /// </summary>
    private const int WH_MOUSE_LL = 14;

    /// <summary>
    /// Windows Message code for key press
    /// </summary>
    private const int WM_KEYDOWN = 0x0100;

    /// <summary>
    /// Windows Message code for "Left Button" press
    /// </summary>
    private const int WM_LBUTTONDOWN = 0x0201;

    /// <summary>
    /// Windows Message code for "Right Button" press
    /// </summary>
    private const int WM_RBUTTONDOWN = 0x0204;

    /// <summary>
    /// Windows Message code for "Middle Button" press
    /// </summary>
    private const int WM_MBUTTONDOWN = 0x0207;

    /// <summary>
    /// Windows Message code for "Side Buttons" press
    /// </summary>
    private const int WM_XBUTTONDOWN = 0x020B;
    /// <summary>
    /// Delegate for handling keyboard hook events
    /// </summary>
    private static LowLevelKeyboardProc _keyboardProc = KeyboardHookCallback;

    /// <summary>
    /// Delegate for handling mouse hook events
    /// </summary>
    private static LowLevelMouseProc _mouseProc = MouseHookCallback;

    /// <summary>
    /// Storage for hook id used to install/uninstall the keyboard hook
    /// </summary>
    private static IntPtr _keyboardHookId = IntPtr.Zero;

    /// <summary>
    /// Storage for hook id used to install/uninstall the mouse hook
    /// </summary>
    private static IntPtr _mouseHookId = IntPtr.Zero;

    /// <summary>
    /// Allows other parts of the app to wait for the key press event
    /// </summary>
    public static event EventHandler<KeyEventArgs> KeyPressed;

    /// <summary>
    /// Allows other parts of the app to wait for the mouse press event
    /// </summary>
    public static event EventHandler<KeyEventArgs> MouseClicked;
    /// <summary>
    /// This is a class used to provide more info about the key/mouse involved in each press
    /// </summary>
    public class KeyEventArgs : EventArgs
    {
        public string Key { get; }

        public KeyEventArgs(string key)
        {
            Key = key;
        }
    }

    /// <summary>
    /// Delegate for low-level keyboard events
    /// </summary>
    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Delegate for low-level mouse events
    /// </summary>
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Creates a hook for mouse input
    /// </summary>
    /// <param name="proc">Delegate for handling keyboard hook events</param>
    private static IntPtr SetKeyboardHook(LowLevelKeyboardProc proc)
    {
        // Grabs the current process
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    /// <summary>
    /// Callback function for keyboard hook
    /// </summary>
    private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // Checking if the key is actually pressed
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            // Converting the lParam into VirtualKeyCode
            int vkCode = Marshal.ReadInt32(lParam);
            VirtualKeyCode keyCode = (VirtualKeyCode)vkCode;
            string key = keyCode.ToXeniaKey();
            if (!string.IsNullOrEmpty(key))
            {
                Log.Information($"Key Pressed: {key}");
                // Invoking the keyPressed event so the other part of this app can trigger
                KeyPressed?.Invoke(null, new KeyEventArgs(key));
            }
        }
        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    /// <summary>
    /// Creates a hook for mouse input
    /// </summary>
    /// <param name="proc">Delegate for handling mouse hook events</param>
    private static IntPtr SetMouseHook(LowLevelMouseProc proc)
    {
        // Grabs the current process
        using (var curProcess = Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    /// <summary>
    /// Callback function for mouse hook
    /// </summary>
    private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // Checking if there was actually some click
        if (nCode >= 0)
        {
            // Looking up what mouse button was pressed
            VirtualKeyCode button;
            switch ((int)wParam)
            {
                case WM_LBUTTONDOWN:
                    button = VirtualKeyCode.LMouse;
                    break;
                case WM_RBUTTONDOWN:
                    button = VirtualKeyCode.RMouse;
                    break;
                case WM_MBUTTONDOWN:
                    button = VirtualKeyCode.MMouse;
                    break;
                case WM_XBUTTONDOWN:
                    // Since there are multiple side buttons (2 Supported), have to detect which one is pressed exactly
                    uint mouseData = (uint)Marshal.ReadInt32(lParam + 8);
                    button = (mouseData >> 16) == 1 ? VirtualKeyCode.Mouse4 : VirtualKeyCode.Mouse5;
                    break;
                default:
                    return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
            }
            string key = button.ToXeniaKey();
            if (!string.IsNullOrEmpty(button.ToXeniaKey()))
            {
                Log.Information($"Key Pressed: {key}");
                // Invoking the keyPressed event so the other part of this app can trigger
                MouseClicked?.Invoke(null, new KeyEventArgs(key));
            }
        }

        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    /// <summary>
    /// Starts listening for keyboard and mouse events
    /// </summary>
    public static void Start()
    {
        _keyboardHookId = SetKeyboardHook(_keyboardProc);
        _mouseHookId = SetMouseHook(_mouseProc);
    }

    /// <summary>
    /// Stops listening for keyboard and mouse events
    /// </summary>
    public static void Stop()
    {
        UnhookWindowsHookEx(_keyboardHookId);
        UnhookWindowsHookEx(_mouseHookId);
    }
}*/

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace XeniaManager.Core.Mousehook;

public static partial class InputListener
{
    #region P/Invoke Declarations

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    #endregion

    #region Constants

    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_XBUTTONDOWN = 0x020B;
    private const uint XBUTTON1 = 1;
    private const uint XBUTTON2 = 2;
    private const int MOUSE_DATA_OFFSET = 8;
    private const int HIGH_WORD_SHIFT = 16;

    #endregion

    #region Delegates

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    #endregion

    #region Static Fields

    private static readonly LowLevelKeyboardProc KeyboardProc = KeyboardHookCallback;
    private static readonly LowLevelMouseProc MouseProc = MouseHookCallback;

    private static IntPtr _keyboardHookId = IntPtr.Zero;
    private static IntPtr _mouseHookId = IntPtr.Zero;
    private static IntPtr _moduleHandle = IntPtr.Zero;

    private static volatile bool _isRunning = false;
    private static readonly object _lockObject = new();

    // Pre-cached mouse button mappings for performance
    private static readonly Dictionary<int, VirtualKeyCode> MouseButtonMap = new()
    {
        { WM_LBUTTONDOWN, VirtualKeyCode.LMouse },
        { WM_RBUTTONDOWN, VirtualKeyCode.RMouse },
        { WM_MBUTTONDOWN, VirtualKeyCode.MMouse }
    };

    #endregion

    #region Events

    public static event EventHandler<KeyEventArgs>? KeyPressed;
    public static event EventHandler<KeyEventArgs>? MouseClicked;

    #endregion

    #region Event Args

    public sealed class KeyEventArgs : EventArgs
    {
        public string Key { get; }

        public KeyEventArgs(string key)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Starts listening for keyboard and mouse events
    /// </summary>
    public static void Start()
    {
        lock (_lockObject)
        {
            if (_isRunning)
            {
                Logger.Warning("InputListener is already running");
                return;
            }

            try
            {
                InitializeModuleHandle();

                _keyboardHookId = SetKeyboardHook(KeyboardProc);
                if (_keyboardHookId == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    Logger.Error($"Failed to install keyboard hook. Error code: {error}");
                    throw new InvalidOperationException($"Failed to install keyboard hook. Error code: {error}");
                }

                _mouseHookId = SetMouseHook(MouseProc);
                if (_mouseHookId == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    Logger.Error($"Failed to install mouse hook. Error code: {error}");

                    // Clean up keyboard hook if mouse hook failed
                    UnhookWindowsHookEx(_keyboardHookId);
                    _keyboardHookId = IntPtr.Zero;

                    throw new InvalidOperationException($"Failed to install mouse hook. Error code: {error}");
                }

                _isRunning = true;
                Logger.Info("InputListener started successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to start InputListener");
                throw;
            }
        }
    }

    /// <summary>
    /// Stops listening for keyboard and mouse events
    /// </summary>
    public static void Stop()
    {
        lock (_lockObject)
        {
            if (!_isRunning)
            {
                Logger.Debug("InputListener is not running");
                return;
            }

            try
            {
                bool keyboardUnhooked = true;
                bool mouseUnhooked = true;

                if (_keyboardHookId != IntPtr.Zero)
                {
                    keyboardUnhooked = UnhookWindowsHookEx(_keyboardHookId);
                    if (!keyboardUnhooked)
                    {
                        var error = Marshal.GetLastWin32Error();
                        Logger.Warning($"Failed to unhook keyboard. Error code: {error}");
                    }
                    _keyboardHookId = IntPtr.Zero;
                }

                if (_mouseHookId != IntPtr.Zero)
                {
                    mouseUnhooked = UnhookWindowsHookEx(_mouseHookId);
                    if (!mouseUnhooked)
                    {
                        var error = Marshal.GetLastWin32Error();
                        Logger.Warning($"Failed to unhook mouse. Error code: {error}");
                    }
                    _mouseHookId = IntPtr.Zero;
                }

                _isRunning = false;

                if (keyboardUnhooked && mouseUnhooked)
                {
                    Logger.Info("InputListener stopped successfully");
                }
                else
                {
                    Logger.Warning("InputListener stopped with warnings");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error occurred while stopping InputListener");
                _isRunning = false;
                throw;
            }
        }
    }

    /// <summary>
    /// Gets whether the InputListener is currently running
    /// </summary>
    public static bool IsRunning => _isRunning;

    #endregion

    #region Private Methods

    private static void InitializeModuleHandle()
    {
        if (_moduleHandle == IntPtr.Zero)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            _moduleHandle = GetModuleHandle(curModule?.ModuleName);

            if (_moduleHandle == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                Logger.Error($"Failed to get module handle. Error code: {error}");
                throw new InvalidOperationException($"Failed to get module handle. Error code: {error}");
            }
        }
    }

    private static IntPtr SetKeyboardHook(LowLevelKeyboardProc proc)
    {
        return SetWindowsHookEx(WH_KEYBOARD_LL, proc, _moduleHandle, 0);
    }

    private static IntPtr SetMouseHook(LowLevelMouseProc proc)
    {
        return SetWindowsHookEx(WH_MOUSE_LL, proc, _moduleHandle, 0);
    }

    private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                var vkCode = Marshal.ReadInt32(lParam);
                var keyCode = (VirtualKeyCode)vkCode;
                var xeniaKey = keyCode.ToXeniaKey();

                if (!string.IsNullOrEmpty(xeniaKey))
                {
                    Logger.Debug($"Keyboard key pressed: {xeniaKey} (VK: {vkCode:X2})");

                    // Fire event on thread pool to avoid blocking the hook
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            KeyPressed?.Invoke(null, new KeyEventArgs(xeniaKey));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, $"Error in KeyPressed event handler for key: {xeniaKey}");
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error in keyboard hook callback");
        }

        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (nCode >= 0)
            {
                var wParamInt = (int)wParam;
                VirtualKeyCode button;

                // Use dictionary lookup for common mouse buttons
                if (MouseButtonMap.TryGetValue(wParamInt, out button))
                {
                    // Common mouse buttons (LMB, RMB, MMB)
                }
                else if (wParamInt == WM_XBUTTONDOWN)
                {
                    // Handle side buttons (Mouse4, Mouse5)
                    var mouseData = (uint)Marshal.ReadInt32(lParam + MOUSE_DATA_OFFSET);
                    var xButton = mouseData >> HIGH_WORD_SHIFT;
                    button = xButton == XBUTTON1 ? VirtualKeyCode.Mouse4 : VirtualKeyCode.Mouse5;
                }
                else
                {
                    // Unknown mouse event, pass through
                    return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
                }

                var xeniaKey = button.ToXeniaKey();
                if (!string.IsNullOrEmpty(xeniaKey))
                {
                    Logger.Debug($"Mouse button pressed: {xeniaKey} (WM: {wParamInt:X4})");

                    // Fire event on thread pool to avoid blocking the hook
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            MouseClicked?.Invoke(null, new KeyEventArgs(xeniaKey));
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, $"Error in MouseClicked event handler for button: {xeniaKey}");
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error in mouse hook callback");
        }

        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    #endregion
}