using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Serilog;

namespace XeniaManager
{
    public static partial class InputListener
    {
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
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,GetModuleHandle(curModule.ModuleName), 0);
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
                if (VirtualKeyMap.TryGetValue(keyCode, out string key))
                {
                    Log.Information($"Key Pressed: {key}");
                    // Invoking the keyPressed event so the other part of this app can trigger
                    KeyPressed?.Invoke(null, new KeyEventArgs(key));
                }
            }
            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }
    }
}