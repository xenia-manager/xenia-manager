using System.Buffers.Binary;
using System.Text;
using XeniaManager.Files;
using XeniaManager.Files.Models.Gpd;
using XeniaManager.Files.Models.Spa;

namespace XeniaManager.Tests.Files;

/// <summary>
/// Tests for dashboard-GPD title management: RemoveTitle, FillTitlePlayedData,
/// and UpdateTitleEntry growth (append + free-space record + table write-back).
/// </summary>
[TestFixture]
public class GpdTitleTests
{
    private static TitleEntry TestTitle(uint titleId = 0x4D5309C9, string name = "Forza Horizon")
    {
        return new TitleEntry
        {
            TitleId = titleId,
            AchievementCount = 10,
            AchievementUnlockedCount = 3,
            GamerscoreTotal = 1000,
            GamerscoreUnlocked = 120,
            TitleName = name
        };
    }

    private static byte[] BuildXthdData(uint titleId, uint type = 1)
    {
        byte[] data = new byte[12 + 32];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), 0x58544844); // XTHD
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(8), 32);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(12), titleId);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(16), type);
        return data;
    }

    private static byte[] BuildXachData(params ushort[] gamerscores)
    {
        byte[] data = new byte[14 + gamerscores.Length * 0x24];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), 0x58414348); // XACH
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(12), (ushort)gamerscores.Length);
        for (int i = 0; i < gamerscores.Length; i++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(14 + i * 0x24), (ushort)(i + 1));
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(14 + i * 0x24 + 12), gamerscores[i]);
        }

        return data;
    }

    private static byte[] BuildXstrData(ushort id, string text)
    {
        byte[] textBytes = Encoding.UTF8.GetBytes(text);
        byte[] data = new byte[14 + 4 + textBytes.Length];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), 0x58535452); // XSTR
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(8), (uint)(4 + textBytes.Length));
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(12), 1);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(14), id);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(16), (ushort)textBytes.Length);
        textBytes.CopyTo(data, 18);
        return data;
    }

    private static SpaFile BuildSpa(uint titleId, string titleName, params ushort[] gamerscores)
    {
        using GpdFile gpd = GpdFile.Create(true);
        gpd.AddRawEntry((EntryNamespace)1, 0x58544844, BuildXthdData(titleId));
        gpd.AddRawEntry((EntryNamespace)1, 0x58414348, BuildXachData(gamerscores));
        gpd.AddRawEntry((EntryNamespace)3, 1, BuildXstrData(0x8000, titleName));
        return SpaFile.FromBytes(gpd.ToBytes());
    }

    [Test]
    public void RemoveTitle_ExistingTitle_RemovesAndRoundTrips()
    {
        using GpdFile gpd = GpdFile.Create();
        gpd.AddTitle(TestTitle(0x4D5309C9));
        gpd.AddTitle(TestTitle(0x584109C2, "Other Game"));

        Assert.That(gpd.RemoveTitle(0x4D5309C9), Is.True);
        Assert.That(gpd.Titles.Select(t => t.TitleId), Is.EquivalentTo(new[]
        {
            0x584109C2u
        }));
        Assert.That(gpd.RemoveTitle(0x4D5309C9), Is.False);

        using GpdFile reloaded = GpdFile.FromBytes(gpd.ToBytes());
        Assert.That(reloaded.Titles.Select(t => t.TitleId), Is.EquivalentTo(new[]
        {
            0x584109C2u
        }));
    }

    [Test]
    public void UpdateTitleEntry_LongerName_AppendsAndFreesOldSpace()
    {
        using GpdFile gpd = GpdFile.Create();
        TitleEntry original = TestTitle(name: "AB");
        gpd.AddTitle(original);
        int freeBefore = gpd.FreeSpaceEntries.Count;
        uint oldLength = (uint)original.ToBytes().Length;

        TitleEntry grown = TestTitle(name: "A much longer title name for growth");
        Assert.That(gpd.UpdateTitleEntry(0x4D5309C9, grown), Is.True);

        // Table row must point at the new data (struct write-back).
        TitleEntry stored = gpd.Titles.Single(t => t.TitleId == 0x4D5309C9);
        Assert.That(stored.TitleName, Is.EqualTo("A much longer title name for growth"));
        Assert.That(stored.GamerscoreTotal, Is.EqualTo(1000));

        // Old space recorded with the OLD length.
        Assert.That(gpd.FreeSpaceEntries.Count, Is.EqualTo(freeBefore + 1));
        Assert.That(gpd.FreeSpaceEntries.Last().Length, Is.EqualTo(oldLength));

        // Survives a serialize round-trip.
        using GpdFile reloaded = GpdFile.FromBytes(gpd.ToBytes());
        Assert.That(reloaded.Titles.Single(t => t.TitleId == 0x4D5309C9).TitleName,
            Is.EqualTo("A much longer title name for growth"));
    }

    [Test]
    public void UpdateTitleEntry_SameSize_OverwritesInPlace()
    {
        using GpdFile gpd = GpdFile.Create();
        gpd.AddTitle(TestTitle(name: "AB"));
        int freeBefore = gpd.FreeSpaceEntries.Count;

        Assert.That(gpd.UpdateTitleEntry(0x4D5309C9, TestTitle(name: "CD")), Is.True);
        Assert.That(gpd.Titles.Single(t => t.TitleId == 0x4D5309C9).TitleName, Is.EqualTo("CD"));
        Assert.That(gpd.FreeSpaceEntries.Count, Is.EqualTo(freeBefore));
        Assert.That(gpd.UpdateTitleEntry(0xDEADBEEF, TestTitle()), Is.False);
    }

    [Test]
    public void FillTitlePlayedData_FromSpa_SetsCountsAndLeavesProgressZero()
    {
        using SpaFile spa = BuildSpa(0x4D5309C9, "Forza Horizon", 100, 50, 25);
        Assert.That(spa.TitleId, Is.EqualTo(0x4D5309C9u));

        TitleEntry filled = GpdFile.FillTitlePlayedData(spa);
        Assert.That(filled.TitleId, Is.EqualTo(0x4D5309C9u));
        Assert.That(filled.AchievementCount, Is.EqualTo(3));
        Assert.That(filled.GamerscoreTotal, Is.EqualTo(175));
        Assert.That(filled.TitleName, Is.EqualTo("Forza Horizon"));
        Assert.That(filled.AchievementUnlockedCount, Is.EqualTo(0));
        Assert.That(filled.GamerscoreUnlocked, Is.EqualTo(0));

        // Fill + AddTitle persists through a round-trip.
        using GpdFile gpd = GpdFile.Create();
        gpd.AddTitle(filled);
        using GpdFile reloaded = GpdFile.FromBytes(gpd.ToBytes());
        TitleEntry stored = reloaded.Titles.Single(t => t.TitleId == 0x4D5309C9);
        Assert.That(stored.AchievementCount, Is.EqualTo(3));
        Assert.That(stored.GamerscoreTotal, Is.EqualTo(175));
        Assert.That(stored.TitleName, Is.EqualTo("Forza Horizon"));
    }
}