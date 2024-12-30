using System.Runtime.InteropServices;

namespace XeniaManager.VFS.Models;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal class XgdHeader
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] Magic = Array.Empty<byte>();

    public uint RootDirSector;

    public uint RootDirSize;

    public long CreationFileTime;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x7c8)]
    public byte[] Padding = Array.Empty<byte>();

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] MagicTail = Array.Empty<byte>();
}