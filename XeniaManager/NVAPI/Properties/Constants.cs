using System;
using System.Runtime.InteropServices;

// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class NVAPI
    {
        // Constants for NVAPI
        private const int NVAPI_MAX_PHYSICAL_GPUS = 64;
        private const int NVAPI_SHORT_STRING_MAX = 64;
    }   
}