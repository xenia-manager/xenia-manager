namespace XeniaManager.VFS.Interface;

public interface IContainerReader
{
    public SectorDecoder GetDecoder();
    public bool TryMount();
    public void Dismount();
    public int GetMountCount();
    public bool TryGetDefault(out byte[] defaultData);
}