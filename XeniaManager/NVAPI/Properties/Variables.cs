using System;
using System.Runtime.InteropServices;

namespace XeniaManager
{
    /// <summary>
    /// Provides a managed wrapper for NVIDIA's NVAPI interface, allowing interaction with NVIDIA GPU drivers and hardware.
    /// This static class encapsulates the necessary P/Invoke declarations and initialization logic for NVAPI access.
    /// </summary>
    public static partial class NVAPI
    {
        /// <summary>
        /// Maintains a handle to the loaded NVAPI library instance.
        /// This handle is used for all subsequent NVAPI function calls.
        /// </summary>
        /// <remarks>
        /// The handle is initialized when the library is loaded and should be cleaned up
        /// when no longer needed by calling FreeLibrary.
        /// A zero/null handle indicates that the library failed to load.
        /// </remarks>
        private static IntPtr _nvapiHandle;
    }
}