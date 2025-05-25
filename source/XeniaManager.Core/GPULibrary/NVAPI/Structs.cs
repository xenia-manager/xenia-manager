using System.Runtime.InteropServices;
using System.Text;

namespace XeniaManager.Core.GPULibrary;

internal struct DRS_PROFILE
{
    public uint version;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)Constants.NVAPI_UNICODE_STRING_MAX)]
    public string profileName;

    public NVDRS_GPU_SUPPORT gpuSupport;
    public uint isPredefined;
    public uint numOfApps;
    public uint numOfSettings;
}

[StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
internal struct DRS_APPLICATION_V3
{
    public uint isMetro
    {
        get { return ((uint)((bitvector1 & 1))); }
        set { bitvector1 = ((uint)((value | bitvector1))); }
    }

    public uint version;
    public uint isPredefined;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)Constants.NVAPI_UNICODE_STRING_MAX)]
    public string appName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)Constants.NVAPI_UNICODE_STRING_MAX)]
    public string userFriendlyName;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)Constants.NVAPI_UNICODE_STRING_MAX)]
    public string launcher;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)Constants.NVAPI_UNICODE_STRING_MAX)]
    public string fileInFolder;

    private uint bitvector1;
}

[StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode, Size = 4100)]
internal struct DRS_SETTING_UNION
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4100)]
    public byte[] rawData;

    public byte[] binaryValue
    {
        get
        {
            var length = BitConverter.ToUInt32(rawData, 0);
            var tmpData = new byte[length];
            Buffer.BlockCopy(rawData, 4, tmpData, 0, (int)length);
            return tmpData;
        }

        set
        {
            rawData = new byte[4100];
            if (value != null)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(value.Length), 0, rawData, 0, 4);
                Buffer.BlockCopy(value, 0, rawData, 4, value.Length);
            }
        }
    }

    public uint dwordValue
    {
        get { return BitConverter.ToUInt32(rawData, 0); }

        set
        {
            rawData = new byte[4100];
            Buffer.BlockCopy(BitConverter.GetBytes(value), 0, rawData, 0, 4);
        }
    }

    public string stringValue
    {
        get { return Encoding.Unicode.GetString(rawData).Split(new[] { '\0' }, 2)[0]; }

        set
        {
            rawData = new byte[4100];
            var bytesRaw = Encoding.Unicode.GetBytes(value);
            Buffer.BlockCopy(bytesRaw, 0, rawData, 0, bytesRaw.Length);
        }
    }

    public string ansiStringValue
    {
        get { return Encoding.Default.GetString(rawData).Split(new[] { '\0' }, 2)[0]; }

        set
        {
            rawData = new byte[4100];
            var bytesRaw = Encoding.Default.GetBytes(value);
            Buffer.BlockCopy(bytesRaw, 0, rawData, 0, bytesRaw.Length);
        }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 8, CharSet = CharSet.Unicode)]
internal struct DRS_SETTING
{
    public uint version;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = (int)Constants.NVAPI_UNICODE_STRING_MAX)]
    public string settingName;

    public uint settingId;
    public DRS_SETTING_TYPE settingType;
    public DRS_SETTING_LOCATION settingLocation;
    public uint isCurrentPredefined;
    public uint isPredefinedValid;
    public DRS_SETTING_UNION predefinedValue;
    public DRS_SETTING_UNION currentValue;
}