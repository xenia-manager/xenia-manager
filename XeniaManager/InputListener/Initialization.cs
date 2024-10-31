using System;

namespace XeniaManager
{
    public static partial class InputListener
    {
        /// <summary>
        /// Starts listening for keyboard and mouse events
        /// </summary>
        public static void Start()
        {
            _keyboardHookID = SetKeyboardHook(_keyboardProc);
            _mouseHookID = SetMouseHook(_mouseProc);
        }

        /// <summary>
        /// Stops listening for keyboard and mouse events
        /// </summary>
        public static void Stop()
        {
            UnhookWindowsHookEx(_keyboardHookID);
            UnhookWindowsHookEx(_mouseHookID);
        }
    }
}