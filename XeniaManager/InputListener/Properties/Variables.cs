namespace XeniaManager
{
    public static partial class InputListener
    {
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
    }
}