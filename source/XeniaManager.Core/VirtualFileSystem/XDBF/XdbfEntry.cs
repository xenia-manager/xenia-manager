using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.VirtualFileSystem.XDBF;
public class XdbfEntry
{
    public ushort Namespace;
    public ulong Id;
    public uint OffsetSpecifier;
    public uint Length;

    public void Read(BinaryReader br, XdbfEndian endian)
    {
        bool bigEndian = endian == XdbfEndian.Big;
        Namespace = EndianUtils.ReadUInt16(br, bigEndian);
        Id = EndianUtils.ReadUInt64(br, bigEndian);
        OffsetSpecifier = EndianUtils.ReadUInt32(br, bigEndian);
        Length = EndianUtils.ReadUInt32(br, bigEndian);
    }

    public void Write(BinaryWriter bw, XdbfEndian endian)
    {
        bool bigEndian = endian == XdbfEndian.Big;
        EndianUtils.WriteUInt16(bw, Namespace, bigEndian);
        EndianUtils.WriteUInt64(bw, Id, bigEndian);
        EndianUtils.WriteUInt32(bw, OffsetSpecifier, bigEndian);
        EndianUtils.WriteUInt32(bw, Length, bigEndian);
    }
}