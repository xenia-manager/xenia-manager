namespace XeniaManager.Core.VirtualFileSystem.XDBF;
public class XdbfFile
{
    public XdbfHeader Header = new XdbfHeader();
    public List<XdbfEntry> Entries = new List<XdbfEntry>();
    public List<XdbfFreeEntry> FreeEntries = new List<XdbfFreeEntry>();
    public byte[] Data = Array.Empty<byte>();

    public void Load(string path)
    {
        using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        using (BinaryReader br = new BinaryReader(fs))
        {
            Header.Read(br);

            // Read Entry Table
            Entries.Clear();
            for (int i = 0; i < Header.EntryCount; i++)
            {
                XdbfEntry entry = new XdbfEntry();
                entry.Read(br, Header.Endian);
                Entries.Add(entry);
            }
            // Skip unused entry table space
            fs.Seek((Header.EntryTableLength - Header.EntryCount) * 18, SeekOrigin.Current);

            // Read Free Space Table
            FreeEntries.Clear();
            for (int i = 0; i < Header.FreeTableCount; i++)
            {
                XdbfFreeEntry free = new XdbfFreeEntry();
                free.Read(br, Header.Endian);
                FreeEntries.Add(free);
            }
            // Skip unused free table space
            fs.Seek((Header.FreeTableLength - Header.FreeTableCount) * 8, SeekOrigin.Current);

            // Read Data
            long dataOffset = 0x18 + (Header.EntryTableLength * 18) + (Header.FreeTableLength * 8);
            fs.Seek(dataOffset, SeekOrigin.Begin);
            Data = br.ReadBytes((int)(fs.Length - dataOffset));
        }
    }

    public void Save(string path)
    {
        using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            Header.Write(bw);

            // Write Entry Table
            foreach (XdbfEntry entry in Entries)
            {
                entry.Write(bw, Header.Endian);
            }
            // Pad to EntryTableLength
            int pad = (int)(Header.EntryTableLength - Header.EntryCount) * 18;
            if (pad > 0) bw.Write(new byte[pad]);

            // Write Free Table
            foreach (XdbfFreeEntry free in FreeEntries)
            {
                free.Write(bw, Header.Endian);
            }
            // Pad to FreeTableLength
            pad = (int)(Header.FreeTableLength - Header.FreeTableCount) * 8;
            if (pad > 0)
            {
                bw.Write(new byte[pad]);
            }

            // Write Data
            bw.Write(Data);
        }
    }

    public byte[] GetEntryData(XdbfEntry entry)
    {
        byte[] buffer = new byte[entry.Length];
        Array.Copy(Data, entry.OffsetSpecifier, buffer, 0, entry.Length);
        return buffer;
    }

    public void SetEntryData(XdbfEntry entry, byte[] newData)
    {
        Array.Copy(newData, 0, Data, entry.OffsetSpecifier, newData.Length);
        entry.Length = (uint)newData.Length;
    }
}