using System;
using System.Runtime.InteropServices;

// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class NVAPI
    {
        public static List<string> GetGPUInfo()
        {
            // Get Delegate for Enumerate GPUs and get GPU names
            var enumGPUs = GetDelegate<NvAPI_EnumPhysicalGPUs>(NVAPI_ENUM_PHYSICAL_GPUS);
            var getGPUName = GetDelegate<NvAPI_GPU_GetFullName>(NVAPI_GPU_GET_FULLNAME);

            // Check if GPUs were fetched correctly
            if (enumGPUs == null || getGPUName == null)
            {
                Log.Error("Failed to get GPU Info");
                return null;
            }
            
            IntPtr[] gpuHandles = new IntPtr[64];
            int gpuCount;
            
            // Try to enumerate GPUs
            int result = enumGPUs(gpuHandles, out gpuCount);
            if (result != 0)
            {
                Log.Error($"Failed to enumerate GPUs. Error code: {result}");
                return null;
            }
            
            Log.Information($"Found {gpuCount} NVIDIA GPU(s)");
            return new List<string>();
        }
    }   
}