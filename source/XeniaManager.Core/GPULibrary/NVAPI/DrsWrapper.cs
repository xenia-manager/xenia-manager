using System.Runtime.InteropServices;
using System.Text;

namespace XeniaManager.Core.GPULibrary;

public static partial class DrsWrapper
{
    private static readonly string _nvApiDll = IntPtr.Size == 4 ? Constants.NVAPI32 : Constants.NVAPI64;
    private static IntPtr _nvApiLibrary;

    public static void Initialize()
    {
        _nvApiLibrary = Helpers.LoadLibrary(_nvApiDll);
        if (_nvApiLibrary == IntPtr.Zero)
        {
            throw new Exception($"Failed to load {_nvApiDll}");
        }

        _nvApi_QueryInterface = GetFunctionDelegate<NvAPI_QueryInterfaceDelegate>(_nvApiLibrary, "nvapi_QueryInterface");
        if (_nvApi_QueryInterface == null)
        {
            throw new Exception("Failed to load NvAPI_QueryInterface");
        }

        GetDelegate((uint)NvAPI_FunctionId.Initialize, out _nvApi_Initialize);
        if (_nvApi_Initialize() != NvApi_Status.OK)
        {
            throw new Exception("Failed to load NvAPI_Initialize");
        }

        GetDelegate((uint)NvAPI_FunctionId.Unload, out _nvApi_Unload);
        GetDelegate((uint)NvAPI_FunctionId.GetErrorMessage, out _nvApi_GetErrorMessage);
        GetDelegate((uint)NvAPI_FunctionId.DRS_CreateSession, out _nvApi_CreateSession);
        GetDelegate((uint)NvAPI_FunctionId.DRS_DestroySession, out _nvApi_DestroySession);
        GetDelegate((uint)NvAPI_FunctionId.DRS_LoadSettings, out _nvApi_LoadSettings);
        GetDelegate((uint)NvAPI_FunctionId.DRS_SaveSettings, out _nvApi_SaveSettings);
        GetDelegate((uint)NvAPI_FunctionId.DRS_CreateProfile, out _nvApi_CreateProfile);
        GetDelegate((uint)NvAPI_FunctionId.DRS_CreateApplication, out _nvApi_CreateApplication);
        GetDelegate((uint)NvAPI_FunctionId.DRS_DeleteProfile, out _nvApi_DeleteProfile);
        GetDelegate((uint)NvAPI_FunctionId.DRS_GetProfileInfo, out _nvApi_GetProfileInfo);
        GetDelegate((uint)NvAPI_FunctionId.DRS_FindApplicationByName, out _nvApi_FindApplicationByName);
        GetDelegate((uint)NvAPI_FunctionId.DRS_FindProfileByName, out _nvApi_FindProfileByName);
        GetDelegate((uint)NvAPI_FunctionId.DRS_GetSetting, out _nvApi_GetSetting, 0x8A2CF5F5);
        GetDelegate((uint)NvAPI_FunctionId.DRS_SetSetting, out _nvApi_SetSetting, 0xEA99498D);
    }
}