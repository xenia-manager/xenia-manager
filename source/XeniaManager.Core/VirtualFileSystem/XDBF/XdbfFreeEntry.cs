using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.VirtualFileSystem.XDBF;
public class XdbfFreeEntry
{
    public uint OffsetSpecifier;
    public uint Length;

    public void Read(BinaryReader br, XdbfEndian endian)
    {
        bool bigEndian = endian == XdbfEndian.Big;
        OffsetSpecifier = EndianUtils.ReadUInt32(br, bigEndian);
        Length = EndianUtils.ReadUInt32(br, bigEndian);
    }

    public void Write(BinaryWriter bw, XdbfEndian endian)
    {
        bool bigEndian = endian == XdbfEndian.Big;
        EndianUtils.WriteUInt32(bw, OffsetSpecifier, bigEndian);
        EndianUtils.WriteUInt32(bw, Length, bigEndian);
    }
}