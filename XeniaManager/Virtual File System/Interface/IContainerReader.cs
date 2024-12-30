namespace XeniaManager.VFS.Interface;

public interface IContainerReader
{
    public SectorDecoder GetDecoder();
    public bool TryMount();
    public void Dismount();
    public int GetMountCount();
    //public bool TryExtractFiles(string destFilePath);
    //public bool TryGetDataSectors(out HashSet<uint> dataSectors);
    public bool TryGetDefault(out byte[] defaultData);
}