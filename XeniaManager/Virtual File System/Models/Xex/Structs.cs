using System.Runtime.InteropServices;

namespace XeniaManager.VFS.Models;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct XexHeader
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Magic;

    public uint ModuleFlags;

    public uint SizeOfHeaders;

    public uint SizeOfDiscardableHeaders;

    public uint SecurityInfo;

    public uint HeaderDirectoryEntryCount;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct HvImageInfo
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
    public byte[] Signature;

    public uint InfoSize;

    public uint ImageFlags;

    public uint LoadAddress;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
    public byte[] ImageHash;

    public uint ImportTableCoun;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
    public byte[] ImportDigest;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
    public byte[] MediaID;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
    public byte[] ImageKey;

    public uint ExportTableAddress;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
    public byte[] HeaderHash;

    public uint GameRegion;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct XexSecurityInfo
{
    public uint Size;

    public uint ImageSize;

    public HvImageInfo ImageInfo;

    public uint AllowedMediaTypes;

    public uint PageDescriptorCount;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
internal struct XexExecution
{
    [FieldOffset(0)]
    public uint MediaId;

    [FieldOffset(4)]
    public uint Version;

    [FieldOffset(8)]
    public uint BaseVersion;

    [FieldOffset(12)]
    public uint TitleId;

    [FieldOffset(12)]
    public ushort PublisherId;

    [FieldOffset(14)]
    public ushort GameId;

    [FieldOffset(16)]
    public byte Platform;

    [FieldOffset(17)]
    public byte ExecutableType;

    [FieldOffset(18)]
    public byte DiscNum;

    [FieldOffset(19)]
    public byte DiscTotal;

    [FieldOffset(20)]
    public uint SaveGameID;
}