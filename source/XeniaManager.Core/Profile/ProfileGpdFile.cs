using XeniaManager.Core.Utilities;
using XeniaManager.Core.VirtualFileSystem.XDBF;

namespace XeniaManager.Core.Profile;
public class ProfileGpdFile
{
    public XdbfFile File { get; private set; } = new XdbfFile();
    public void Load(string path) => File.Load(path);
    public void Save(string path) => File.Save(path);

    /// <summary>
    /// Updates the unlocked achievement count and gamerscore for a title in the account GPD.
    /// </summary>
    public void UpdateUnlockedAchievementsForTitle(uint titleId, int unlockedCount, int unlockedGamerscore)
    {
        foreach (var entry in File.Entries)
        {
            if (entry.Namespace == 4 && entry.Id == titleId)
            {
                byte[] data = File.GetEntryData(entry);
                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader br = new BinaryReader(ms))
                {
                    uint readTitleId = EndianUtils.ReadUInt32(br, true);
                    int achievementCount = EndianUtils.ReadInt32(br, true);
                    int achievementUnlocked = EndianUtils.ReadInt32(br, true);
                    int gamerscoreTotal = EndianUtils.ReadInt32(br, true);
                    int gamerscoreUnlocked = EndianUtils.ReadInt32(br, true);

                    // Update only the unlocked count and gamerscore
                    achievementUnlocked = unlockedCount;
                    gamerscoreUnlocked = unlockedGamerscore;

                    // Read the rest of the fields
                    byte[] rest = br.ReadBytes((int)(ms.Length - ms.Position));

                    // Write back
                    using (MemoryStream ms2 = new MemoryStream())
                    using (BinaryWriter bw = new BinaryWriter(ms2))
                    {
                        EndianUtils.WriteUInt32(bw, readTitleId, true);
                        EndianUtils.WriteInt32(bw, achievementCount, true);
                        EndianUtils.WriteInt32(bw, achievementUnlocked, true);
                        EndianUtils.WriteInt32(bw, gamerscoreTotal, true);
                        EndianUtils.WriteInt32(bw, gamerscoreUnlocked, true);
                        bw.Write(rest);

                        File.SetEntryData(entry, ms2.ToArray());
                    }
                }
            }
        }
    }
}