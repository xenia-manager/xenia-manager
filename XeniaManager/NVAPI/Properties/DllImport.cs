using System;
using System.Runtime.InteropServices;

namespace XeniaManager
{
    public static partial class NVAPI
    {
        /// <summary>
        /// Windows API function for dynamically loading a DLL into the current process's address space.
        /// </summary>
        /// <param name="dllToLoad">The path or name of the DLL to load. If only the DLL name is specified,
        /// the system uses standard search strategies to find the DLL.</param>
        /// <returns>
        /// If successful, returns a handle to the loaded DLL module.
        /// If the function fails, returns IntPtr.Zero. Use Marshal.GetLastWin32Error() to get detailed error information.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        /// <summary>
        /// Windows API function for unloading a DLL that was previously loaded via LoadLibrary.
        /// </summary>
        /// <param name="hModule">Handle to the loaded library module to free, previously obtained from LoadLibrary.</param>
        /// <returns>
        /// true if the module was successfully unloaded;
        /// false if the function fails. Use Marshal.GetLastWin32Error() to get detailed error information.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);
    }   
}