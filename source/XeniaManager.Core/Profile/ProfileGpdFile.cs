using XeniaManager.Core.VirtualFileSystem.XDBF;

namespace XeniaManager.Core.Profile;
public class ProfileGpdFile
{
    public XdbfFile File { get; private set; } = new XdbfFile();
    public void Load(string path) => File.Load(path);
    public void Save(string path) => File.Save(path);
}