using System;
using System.Runtime.InteropServices;
using System.Text;

// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class NVAPI
    {
        /// <summary>
        /// Function to get NVAPI function pointers using their hash IDs
        /// </summary>
        /// <param name="interfaceId"></param>
        /// <returns></returns>
        [DllImport("nvapi64.dll", EntryPoint = "nvapi_QueryInterface", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NvAPI_QueryInterface(uint interfaceId);
        
        /// <summary>
        /// Delegate for the NVAPI initialization function.
        /// Returns 0 for success, non-zero value indicates an error.
        /// </summary>
        /// <returns>Status code where 0 indicates success</returns>
        private delegate int NvAPI_Initialize();

        /// <summary>
        /// Delegate for enumerating physical NVIDIA GPUs in the system.
        /// </summary>
        /// <param name="gpuHandles">Array to store handles to discovered GPUs</param>
        /// <param name="gpuCount">Output parameter that receives the number of GPUs found</param>
        /// <returns>Status code where 0 indicates success</returns>
        private delegate int NvAPI_EnumPhysicalGPUs(IntPtr[] gpuHandles, out int gpuCount);

        /// <summary>
        /// Delegate for retrieving the full name of a GPU.
        /// </summary>
        /// <param name="gpuHandle">Handle to the GPU</param>
        /// <param name="name">StringBuilder that receives the GPU name</param>
        /// <returns>Status code where 0 indicates success</returns>
        private delegate int NvAPI_GPU_GetFullName(IntPtr gpuHandle, [MarshalAs(UnmanagedType.LPStr)] StringBuilder name);
        
        /// <summary>
        /// Retrieves a typed delegate for a named function from the NVAPI library.
        /// </summary>
        /// <typeparam name="T">The type of delegate to create. Must be a Delegate type.</typeparam>
        /// <param name="functionName">The name of the function to look up in the NVAPI library</param>
        /// <returns>
        /// A delegate of type T that can be used to call the function, or null if the function
        /// could not be found or the delegate could not be created.
        /// </returns>
        private static T GetDelegate<T>(uint id) where T : Delegate
        {
            IntPtr functionPtr = NvAPI_QueryInterface(id);
            if (functionPtr == IntPtr.Zero)
            {
                Log.Error($"Failed to get address. Error: {Marshal.GetLastWin32Error()}");
                return null;
            }
            return Marshal.GetDelegateForFunctionPointer<T>(functionPtr);
        }
    }   
}