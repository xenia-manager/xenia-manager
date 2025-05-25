using System.Runtime.InteropServices;
using System.Text;

namespace XeniaManager.Core.GPULibrary;

public static partial class DrsWrapper
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr NvAPI_QueryInterfaceDelegate(uint id);
    private static NvAPI_QueryInterfaceDelegate _nvApi_QueryInterface;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvApi_Status NvAPI_InitializeDelegate();
    private static NvAPI_InitializeDelegate _nvApi_Initialize;
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvApi_Status NvAPI_UnloadDelegate();
    private static NvAPI_UnloadDelegate _nvApi_Unload;
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvApi_Status NvAPI_GetErrorMessageDelegate(NvApi_Status status, [MarshalAs(UnmanagedType.LPStr, SizeConst = (int)Constants.NVAPI_SHORT_STRING_MAX)]StringBuilder szDesc);
    private static NvAPI_GetErrorMessageDelegate _nvApi_GetErrorMessage;
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvApi_Status NvAPI_CreateSessionDelegate(ref IntPtr phSession);
    private static NvAPI_CreateSessionDelegate _nvApi_CreateSession;
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvApi_Status NvAPI_DestroySessionDelegate(IntPtr hSession);
    private static NvAPI_DestroySessionDelegate _nvApi_DestroySession;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvApi_Status NvAPI_LoadSettingsDelegate(IntPtr hSession);
    private static NvAPI_LoadSettingsDelegate _nvApi_LoadSettings;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvApi_Status NvAPI_SaveSettingsDelegate(IntPtr hSession);
    private static NvAPI_SaveSettingsDelegate _nvApi_SaveSettings;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvApi_Status NvAPI_CreateProfileDelegate(IntPtr hSession, DRS_PROFILE profileInfo,ref IntPtr phProfile);
    private static NvAPI_CreateProfileDelegate _nvApi_CreateProfile;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvApi_Status NvAPI_CreateApplicationDelegate(IntPtr hSession, IntPtr hProfile, ref DRS_APPLICATION_V3 application);
    private static NvAPI_CreateApplicationDelegate _nvApi_CreateApplication;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvApi_Status NvAPI_DeleteProfileDelegate(IntPtr hSession, IntPtr hProfile);
    private static NvAPI_DeleteProfileDelegate _nvApi_DeleteProfile;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvApi_Status NvAPI_GetProfileInfoDelegate(IntPtr hSession, IntPtr hProfile, ref DRS_PROFILE profileInfo);
    private static NvAPI_GetProfileInfoDelegate _nvApi_GetProfileInfo;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvApi_Status NvAPI_FindApplicationByNameDelegate(IntPtr hSession, [MarshalAs(UnmanagedType.LPWStr, SizeConst = (int)Constants.NVAPI_UNICODE_STRING_MAX)]StringBuilder appName, ref IntPtr phProfile, ref DRS_APPLICATION_V3 pApplication);
    private static NvAPI_FindApplicationByNameDelegate _nvApi_FindApplicationByName;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvApi_Status NvAPI_FindProfileByNameDelegate(IntPtr hSession, [MarshalAs(UnmanagedType.LPWStr, SizeConst = (int)Constants.NVAPI_UNICODE_STRING_MAX)]StringBuilder profileName, ref IntPtr phProfile);
    private static NvAPI_FindProfileByNameDelegate _nvApi_FindProfileByName;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvApi_Status NvAPI_GetSettingDelegate(IntPtr hSession, IntPtr hProfile, ref DRS_SETTING pSetting, uint x, uint y);
    private static NvAPI_GetSettingDelegate _nvApi_GetSetting;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate NvApi_Status NvAPI_SetSettingDelegate(IntPtr hSession, IntPtr hProfile, uint settingId, ref DRS_SETTING pSetting, ref uint x);
    private static NvAPI_SetSettingDelegate _nvApi_SetSetting;

    private static void GetDelegate<T>(uint functionId, out T newDelegate, uint? fallbackId = null) where T : class
    {
        IntPtr functionPtr = _nvApi_QueryInterface((uint)functionId);
        if (functionPtr != IntPtr.Zero)
        {
            newDelegate = Marshal.GetDelegateForFunctionPointer<T>(functionPtr);
        }
        else if (fallbackId != null)
        {
            GetDelegate(fallbackId.Value, out newDelegate);
        }
        else
        {
            newDelegate = null;
        }
    }

    private static T GetFunctionDelegate<T>(IntPtr functionPtr, string functionName)
    {
        T functionDelegate = default(T);
        IntPtr functionAddress = Helpers.GetProcAddress(functionPtr, functionName);
        if (functionAddress != IntPtr.Zero)
        {
            functionDelegate = Marshal.GetDelegateForFunctionPointer<T>(functionAddress);
        }
        return functionDelegate;
    }
}