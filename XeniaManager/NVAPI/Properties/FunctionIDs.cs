using System;
using System.Runtime.InteropServices;

// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class NVAPI
    {
        /// <summary>
        /// Function ID for NVAPI_INITIALIZE function
        /// </summary>
        private const uint NVAPI_INITIALIZE = 0x0150E828;
        
        /// <summary>
        /// Function ID for NVAPI_ENUM_PHYSICAL_GPUS function
        /// </summary>
        private const uint NVAPI_ENUM_PHYSICAL_GPUS = 0xE5AC921F;
        
        /// <summary>
        /// Function ID for NVAPI_GPU_GET_FULLNAME function
        /// </summary>
        private const uint NVAPI_GPU_GET_FULLNAME = 0xCEEE8E9F;
    }   
}