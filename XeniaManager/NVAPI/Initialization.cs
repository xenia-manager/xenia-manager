using System;
using System.Runtime.InteropServices;

// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class NVAPI
    {
        // Loading of NVAPI Library
        /// <summary>
        /// Determines the appropriate NVAPI DLL path based on the current system architecture.
        /// </summary>
        /// <returns>
        /// The name of the NVAPI DLL:
        /// <para>"nvapi64.dll" for 64-bit systems</para>
        /// "nvapi.dll" for 32-bit systems
        /// </returns>
        private static string GetNvApiDll()
        {
            return IntPtr.Size == 8 ? "nvapi64.dll" : "nvapi.dll";
        }

        /// <summary>
        /// Loads the NVAPI library into the current process.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the library was loaded successfully; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method must be called before any other NVAPI functions can be used.
        /// The appropriate version (32-bit or 64-bit) of the library will be loaded
        /// based on the current process architecture.
        /// </para>
        /// <para>
        /// If the loading fails, the error will be logged using Serilog, including
        /// the Windows error code for debugging purposes.
        /// </para>
        /// </remarks>
        public static bool LoadNVAPIlibrary()
        {
            string nvApiDll = GetNvApiDll();
            _nvapiHandle = LoadLibrary(nvApiDll);

            if (_nvapiHandle == IntPtr.Zero)
            {
                Log.Error($"Failed to load NVAPI. Error code: {Marshal.GetLastWin32Error()}");
                return false;
            }

            Log.Information($"Loaded NVAPI successfully.");
            return true;
        }
        
        /// <summary>
        /// Unloads the NVAPI library from the current process.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method should be called when the application is shutting down or
        /// when NVAPI functionality is no longer needed to free system resources.
        /// </para>
        /// <para>
        /// It is safe to call this method multiple times, as it checks whether
        /// the library is already unloaded before attempting to unload it again.
        /// </para>
        /// </remarks>
        public static void UnloadNvApiLibrary()
        {
            if (_nvapiHandle != IntPtr.Zero)
            {
                FreeLibrary(_nvapiHandle);
                _nvapiHandle = IntPtr.Zero;
            }
        }
        
        // Loading of NVAPI from the library
        /// <summary>
        /// Initializes the NVAPI interface after loading the library.
        /// </summary>
        /// <returns>True if initialization was successful, false otherwise.</returns>
        public static bool Initialize()
        {
            // Trying to load NVAPI Library
            if (!LoadNVAPIlibrary())
            {
                return false;
            }

            var nvInit = GetDelegate<NvAPI_Initialize>(NVAPI_INITIALIZE);
            if (nvInit == null)
                return false;

            int result = nvInit();
            if (result != 0)
            {
                Log.Error($"Failed to initialize NVAPI. Error code: {result}");
                return false;
            }

            return true;
        }
    }
}