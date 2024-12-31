namespace XeniaManager.VFS.Models;

internal struct TreeNodeInfo
{
    public byte[] DirectoryData { get; set; }
    public uint Offset { get; set; }
    public string Path { get; set; }
};