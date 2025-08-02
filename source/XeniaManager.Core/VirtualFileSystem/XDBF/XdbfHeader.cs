using System.Text;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.VirtualFileSystem.XDBF;
public class XdbfHeader
{
    public string Magic = string.Empty; // "XDBF"
    public uint Version;
    public uint EntryTableLength;
    public uint EntryCount;
    public uint FreeTableLength;
    public uint FreeTableCount;
    public XdbfEndian Endian;

    public void Read(BinaryReader br)
    {
        byte[] magicBytes = br.ReadBytes(4);
        Magic = Encoding.ASCII.GetString(magicBytes);
        Endian = (magicBytes[0] == 0x58) ? XdbfEndian.Big : XdbfEndian.Little; // 'X'
        bool bigEndian = Endian == XdbfEndian.Big;

        Version = EndianUtils.ReadUInt32(br, bigEndian);
        EntryTableLength = EndianUtils.ReadUInt32(br, bigEndian);
        EntryCount = EndianUtils.ReadUInt32(br, bigEndian);
        FreeTableLength = EndianUtils.ReadUInt32(br, bigEndian);
        FreeTableCount = EndianUtils.ReadUInt32(br, bigEndian);
    }

    public void Write(BinaryWriter bw)
    {
        bw.Write(Encoding.ASCII.GetBytes(Magic));
        bool bigEndian = Endian == XdbfEndian.Big;

        EndianUtils.WriteUInt32(bw, Version, bigEndian);
        EndianUtils.WriteUInt32(bw, EntryTableLength, bigEndian);
        EndianUtils.WriteUInt32(bw, EntryCount, bigEndian);
        EndianUtils.WriteUInt32(bw, FreeTableLength, bigEndian);
        EndianUtils.WriteUInt32(bw, FreeTableCount, bigEndian);
    }
}