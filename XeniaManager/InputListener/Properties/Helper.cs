namespace XeniaManager
{
    public static partial class InputListener
    {
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
    }
}