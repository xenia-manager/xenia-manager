namespace XeniaManager
{
    public static partial class InputListener
    {
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
    }
}