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
                        return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
                }

                if (VirtualKeyMap.TryGetValue(button, out string key))
                {
                    Log.Information($"Key Pressed: {key}");
                    // Invoking the keyPressed event so the other part of this app can trigger
                    MouseClicked?.Invoke(null, new KeyEventArgs(key));
                }
            }

            return CallNextHookEx(_mouseHookID, nCode, wParam, lParam);
        }
    }
}