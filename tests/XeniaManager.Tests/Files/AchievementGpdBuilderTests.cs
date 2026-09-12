using System.Buffers.Binary;
using System.Text;
using XeniaManager.Files;
using XeniaManager.Files.Models.Account;
using XeniaManager.Files.Models.Gpd;
using XeniaManager.Files.Models.XConfig;
using XeniaManager.Files.Utilities;

namespace XeniaManager.Tests.Files;

[TestFixture]
public class AchievementGpdBuilderTests
{
    private static readonly byte[] MinimalPng =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
        0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
        0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
        0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
        0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
        0x42, 0x60, 0x82
    ];

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

    private static byte[] BuildXachData(params (ushort id, ushort label, ushort desc, ushort unach, uint image, ushort score, uint flags)[] rows)
    {
        byte[] data = new byte[14 + rows.Length * 0x24];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), 0x58414348); // XACH
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(12), (ushort)rows.Length);
        for (int i = 0; i < rows.Length; i++)
        {
            int off = 14 + i * 0x24;
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(off), rows[i].id);
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(off + 2), rows[i].label);
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(off + 4), rows[i].desc);
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(off + 6), rows[i].unach);
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(off + 8), rows[i].image);
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(off + 12), rows[i].score);
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(off + 16), rows[i].flags);
        }

        return data;
    }

    private static byte[] BuildXstrData(params (ushort id, string text)[] strings)
    {
        List<byte> body = [];
        foreach ((ushort id, string text) in strings)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            byte[] entry = new byte[4 + textBytes.Length];
            BinaryPrimitives.WriteUInt16BigEndian(entry.AsSpan(0), id);
            BinaryPrimitives.WriteUInt16BigEndian(entry.AsSpan(2), (ushort)textBytes.Length);
            textBytes.CopyTo(entry, 4);
            body.AddRange(entry);
        }

        byte[] data = new byte[14 + body.Count];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), 0x58535452); // XSTR
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(8), (uint)body.Count);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(12), (ushort)strings.Length);
        body.ToArray().CopyTo(data, 14);
        return data;
    }

    private static SpaFile BuildSpa(uint titleId = 0x4D5309C9, uint titleType = 1)
    {
        using GpdFile gpd = GpdFile.Create(true);
        gpd.AddRawEntry((EntryNamespace)1, 0x58544844, BuildXthdData(titleId, titleType));
        gpd.AddRawEntry((EntryNamespace)1, 0x58414348, BuildXachData(
            (1, 10, 11, 12, 0x100, 100, 0),
            (2, 20, 21, 22, 0x101, 50, 0),
            (3, 30, 31, 32, 0x102, 25, 0)));
        gpd.AddRawEntry((EntryNamespace)3, 1, BuildXstrData(
            (0x8000, "Test Game"),
            (10, "First"), (11, "First unlocked"), (12, "First locked"),
            (20, "Second"), (21, "Second unlocked"), (22, "Second locked"),
            (30, "Third"), (31, "Third unlocked"), (32, "Third locked")));
        gpd.AddRawEntry((EntryNamespace)3, 4, BuildXstrData(
            (0x8000, "Jeu de test"),
            (10, "Premier"), (11, "Premier débloqué"), (12, "Premier verrouillé"),
            (20, "Deuxième"), (21, "Deuxième débloqué"), (22, "Deuxième verrouillé"),
            (30, "Troisième"), (31, "Troisième débloqué"), (32, "Troisième verrouillé")));
        gpd.AddImage(0x100, MinimalPng);
        gpd.AddImage(0x101, MinimalPng);
        gpd.AddImage(0x102, MinimalPng);
        gpd.AddImage(0x8000, MinimalPng);
        return SpaFile.FromBytes(gpd.ToBytes());
    }

    private static string NewTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), $"gpdbuilder_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Test]
    public void FromConsoleLanguage_MapsExpectedValues()
    {
        Assert.That(AchievementGpdBuilder.FromConsoleLanguage(ConsoleLanguage.English), Is.EqualTo(XLanguage.English));
        Assert.That(AchievementGpdBuilder.FromConsoleLanguage(ConsoleLanguage.French), Is.EqualTo(XLanguage.French));
        Assert.That(AchievementGpdBuilder.FromConsoleLanguage(ConsoleLanguage.SimplifiedChinese), Is.EqualTo(XLanguage.SChinese));
        Assert.That(AchievementGpdBuilder.FromConsoleLanguage(ConsoleLanguage.TraditionalChinese), Is.EqualTo(XLanguage.TChinese));
        Assert.That(AchievementGpdBuilder.FromConsoleLanguage(ConsoleLanguage.None), Is.EqualTo(XLanguage.Invalid));
    }

    [Test]
    public void EnsureFromSpa_CreatesTitleAndProfileGpd()
    {
        string dir = NewTempDir();
        try
        {
            string titlePath = Path.Combine(dir, "4D5309C9.gpd");
            string profilePath = Path.Combine(dir, "FFFE07D1.gpd");
            using SpaFile spa = BuildSpa();

            AchievementGpdBuildResult result = AchievementGpdBuilder.EnsureFromSpa(spa, titlePath, profilePath, XLanguage.English);

            Assert.That(result.TitleId, Is.EqualTo(0x4D5309C9u));
            Assert.That(result.AchievementsAdded, Is.EqualTo(3));
            Assert.That(result.AchievementsTotal, Is.EqualTo(3));
            Assert.That(result.ProfileUpdated, Is.True);

            using GpdFile titleGpd = GpdFile.Load(titlePath);
            Assert.That(titleGpd.Achievements.Count(), Is.EqualTo(3));
            Assert.That(titleGpd.Achievements.Select(a => a.Name), Is.EquivalentTo(["First", "Second", "Third"]));
            Assert.That(titleGpd.GetTotalPossibleGamerscore(), Is.EqualTo(175));
            Assert.That(titleGpd.GetImage(0x8000)!.IsValidPng, Is.True);
            Assert.That(titleGpd.GetString(0x8000)!.Value, Is.EqualTo("Test Game"));

            using GpdFile profileGpd = GpdFile.Load(profilePath);
            TitleEntry entry = profileGpd.Titles.Single(t => t.TitleId == 0x4D5309C9);
            Assert.That(entry.AchievementCount, Is.EqualTo(3));
            Assert.That(entry.GamerscoreTotal, Is.EqualTo(175));
            Assert.That(entry.TitleName, Is.EqualTo("Test Game"));
            Assert.That(entry.AchievementUnlockedCount, Is.EqualTo(0));
            Assert.That(entry.LastPlayedTime, Is.Not.EqualTo(0));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void EnsureFromSpa_FillsMissingAndPreservesUnlocks()
    {
        string dir = NewTempDir();
        try
        {
            string titlePath = Path.Combine(dir, "4D5309C9.gpd");
            string profilePath = Path.Combine(dir, "FFFE07D1.gpd");

            // Pre-existing GPD with only the first achievement, already unlocked.
            using (GpdFile existing = GpdFile.Create(true))
            {
                existing.AddAchievement(new AchievementEntry
                {
                    AchievementId = 1,
                    ImageId = 0x100,
                    Gamerscore = 100,
                    Name = "First",
                    UnlockedDescription = "First unlocked",
                    LockedDescription = "First locked"
                });
                existing.UnlockAchievement(1);
                existing.Save(titlePath);
            }

            using (GpdFile profile = GpdFile.Create(true))
            {
                profile.AddTitle(new TitleEntry
                {
                    TitleId = 0x4D5309C9,
                    AchievementCount = 1,
                    AchievementUnlockedCount = 1,
                    GamerscoreTotal = 100,
                    GamerscoreUnlocked = 100,
                    TitleName = "Test Game"
                });
                profile.Save(profilePath);
            }

            using SpaFile spa = BuildSpa();
            AchievementGpdBuildResult result = AchievementGpdBuilder.EnsureFromSpa(spa, titlePath, profilePath, XLanguage.English);

            Assert.That(result.AchievementsAdded, Is.EqualTo(2));
            Assert.That(result.AchievementsTotal, Is.EqualTo(3));

            using GpdFile titleGpd = GpdFile.Load(titlePath);
            AchievementEntry first = titleGpd.GetAchievement(1)!;
            Assert.That(first.IsEarned, Is.True, "Existing unlock must be preserved");

            using GpdFile profileGpd = GpdFile.Load(profilePath);
            TitleEntry entry = profileGpd.Titles.Single(t => t.TitleId == 0x4D5309C9);
            Assert.That(entry.AchievementCount, Is.EqualTo(3));
            Assert.That(entry.GamerscoreTotal, Is.EqualTo(175));
            Assert.That(entry.AchievementUnlockedCount, Is.EqualTo(1), "Unlock progress must be preserved");
            Assert.That(entry.GamerscoreUnlocked, Is.EqualTo(100));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void EnsureFromSpa_UsesUserLanguageWithFallback()
    {
        string dir = NewTempDir();
        try
        {
            using SpaFile spa = BuildSpa();

            string frenchPath = Path.Combine(dir, "fr.gpd");
            AchievementGpdBuilder.EnsureFromSpa(spa, frenchPath, null, XLanguage.French);
            using GpdFile french = GpdFile.Load(frenchPath);
            Assert.That(french.GetAchievement(1)!.Name, Is.EqualTo("Premier"));

            // German has no string table: falls back to the default (English) language.
            string germanPath = Path.Combine(dir, "de.gpd");
            AchievementGpdBuilder.EnsureFromSpa(spa, germanPath, null, XLanguage.German);
            using GpdFile german = GpdFile.Load(germanPath);
            Assert.That(german.GetAchievement(1)!.Name, Is.EqualTo("First"));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void FetchFromSpa_FillsMissingOnlyByDefault()
    {
        string dir = NewTempDir();
        try
        {
            string titlePath = Path.Combine(dir, "4D5309C9.gpd");
            using SpaFile spa = BuildSpa();
            AchievementGpdBuilder.EnsureFromSpa(spa, titlePath, null, XLanguage.English);

            int written = AchievementGpdBuilder.FetchFromSpa(spa, titlePath, false);
            Assert.That(written, Is.EqualTo(3)); // 3 achievements (title icon is added by Ensure)

            // Second run finds everything present.
            Assert.That(AchievementGpdBuilder.FetchFromSpa(spa, titlePath, false), Is.EqualTo(0));

            using GpdFile titleGpd = GpdFile.Load(titlePath);
            Assert.That(titleGpd.GetImage(0x100)!.IsValidPng, Is.True);
            Assert.That(titleGpd.GetImage(0x8000)!.IsValidPng, Is.True);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void FetchFromSpa_OverwriteAll_ReplacesExisting()
    {
        string dir = NewTempDir();
        try
        {
            string titlePath = Path.Combine(dir, "4D5309C9.gpd");
            using SpaFile spa = BuildSpa();
            AchievementGpdBuilder.EnsureFromSpa(spa, titlePath, null, XLanguage.English);
            Assert.That(AchievementGpdBuilder.FetchFromSpa(spa, titlePath, false), Is.GreaterThan(0));

            int countBefore;
            using (GpdFile before = GpdFile.Load(titlePath))
            {
                countBefore = before.Entries.Count;
            }

            int written = AchievementGpdBuilder.FetchFromSpa(spa, titlePath, true);
            Assert.That(written, Is.EqualTo(4));
            // Overwrite replaces rows instead of duplicating them.
            using GpdFile after = GpdFile.Load(titlePath);
            Assert.That(after.Entries.Count, Is.EqualTo(countBefore));
            Assert.That(after.GetImage(0x100)!.IsValidPng, Is.True);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void EnsureFromSpa_SystemTitle_ThrowsInvalidOperation()
    {
        string dir = NewTempDir();
        try
        {
            using SpaFile spa = BuildSpa(titleType: 0); // System app, excluded from profiles
            Assert.Throws<InvalidOperationException>(() => AchievementGpdBuilder.EnsureFromSpa(
                spa, Path.Combine(dir, "4D5309C9.gpd"), Path.Combine(dir, "FFFE07D1.gpd"), XLanguage.English));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void AddString_GetString_RoundTrips()
    {
        using GpdFile gpd = GpdFile.Create(true);
        Assert.That(gpd.GetString(0x8000), Is.Null);

        gpd.AddString(0x8000, "Test Game");
        Assert.That(gpd.GetString(0x8000)!.Value, Is.EqualTo("Test Game"));

        using GpdFile reloaded = GpdFile.FromBytes(gpd.ToBytes());
        Assert.That(reloaded.GetString(0x8000)!.Value, Is.EqualTo("Test Game"));
    }

    [Test]
    public void ToBytes_SortsEntriesByNamespaceThenId()
    {
        using GpdFile gpd = GpdFile.Create(true);
        gpd.AddTitle(new TitleEntry
        {
            TitleId = 0x4D5309C9,
            TitleName = "B"
        });
        gpd.AddAchievement(new AchievementEntry
        {
            AchievementId = 2,
            Name = "B"
        });
        gpd.AddImage(0x101, MinimalPng);
        gpd.AddAchievement(new AchievementEntry
        {
            AchievementId = 1,
            Name = "A"
        });
        gpd.AddString(0x8000, "B");

        using GpdFile reloaded = GpdFile.FromBytes(gpd.ToBytes());
        List<(ushort, ulong Id)> order = reloaded.Entries.Select(e => ((ushort)e.Namespace, e.Id)).ToList();
        Assert.That(order, Is.Ordered);
        Assert.That(order.Select(o => o.Item1), Is.EquivalentTo(new ushort[]
        {
            1, 1, 2, 4, 5
        }));
    }

    [Test]
    public void Create_SeedsEndOfDataMarker()
    {
        using GpdFile gpd = GpdFile.Create(true);
        Assert.That(gpd.FreeSpaceEntries.Count, Is.EqualTo(1));
        Assert.That(gpd.FreeSpaceEntries[0].OffsetSpecifier, Is.EqualTo(0));
        Assert.That(gpd.FreeSpaceEntries[0].IsFreeSpace, Is.False);

        using GpdFile reloaded = GpdFile.FromBytes(gpd.ToBytes());
        Assert.That(reloaded.FreeSpaceEntries.Count, Is.EqualTo(1));
        Assert.That(reloaded.FreeSpaceEntries[0].OffsetSpecifier, Is.EqualTo((uint)reloaded.Data.Length));
        Assert.That(reloaded.FreeSpaceEntries[0].Length, Is.EqualTo(uint.MaxValue - (uint)reloaded.Data.Length));
    }

    [Test]
    public void Save_MaintainsSingleEndOfDataMarker()
    {
        string dir = NewTempDir();
        try
        {
            string titlePath = Path.Combine(dir, "4D5309C9.gpd");
            using SpaFile spa = BuildSpa();
            AchievementGpdBuilder.EnsureFromSpa(spa, titlePath, null, XLanguage.English);

            using GpdFile first = GpdFile.Load(titlePath);
            Assert.That(first.FreeSpaceEntries.Count(f => !f.IsFreeSpace), Is.EqualTo(1));

            // Saving again neither duplicates the marker nor moves real entries.
            first.Save(titlePath);
            using GpdFile second = GpdFile.Load(titlePath);
            Assert.That(second.FreeSpaceEntries.Count(f => !f.IsFreeSpace), Is.EqualTo(1));
            Assert.That(second.FreeSpaceEntries.Single(f => !f.IsFreeSpace).OffsetSpecifier,
                Is.EqualTo((uint)second.Data.Length));
            Assert.That(second.Achievements.Count(), Is.EqualTo(3));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void EnsureFromSpa_RepairsMissingMarker()
    {
        string dir = NewTempDir();
        try
        {
            string titlePath = Path.Combine(dir, "4D5309C9.gpd");
            using SpaFile spa = BuildSpa();
            AchievementGpdBuilder.EnsureFromSpa(spa, titlePath, null, XLanguage.English);

            // Simulate a file predating the marker by zeroing the free-used header field.
            byte[] raw = File.ReadAllBytes(titlePath);
            BinaryPrimitives.WriteUInt32BigEndian(raw.AsSpan(0x14), 0);
            File.WriteAllBytes(titlePath, raw);
            using (GpdFile stripped = GpdFile.Load(titlePath))
            {
                Assert.That(stripped.FreeSpaceEntries, Is.Empty);
            }

            // Nothing to add, but the repair still saves.
            AchievementGpdBuildResult result = AchievementGpdBuilder.EnsureFromSpa(spa, titlePath, null, XLanguage.English);
            Assert.That(result.AchievementsAdded, Is.EqualTo(0));

            using GpdFile repaired = GpdFile.Load(titlePath);
            Assert.That(repaired.FreeSpaceEntries.Count(f => !f.IsFreeSpace), Is.EqualTo(1));
            Assert.That(repaired.Achievements.Count(), Is.EqualTo(3));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void EnsureFromSpa_CorruptArt_StaysSingle()
    {
        string dir = NewTempDir();
        try
        {
            string titlePath = Path.Combine(dir, "4D5309C9.gpd");
            using SpaFile spa = BuildSpa();
            AchievementGpdBuilder.EnsureFromSpa(spa, titlePath, null, XLanguage.English);

            // Corrupt the title icon and name in place (simulating a damaged file).
            using (GpdFile damaged = GpdFile.Load(titlePath))
            {
                damaged.RemoveImage(0x8000);
                damaged.AddRawEntry(EntryNamespace.Image, 0x8000, [0x00, 0x01, 0x02]);
                damaged.Save(titlePath);
            }

            using (GpdFile check = GpdFile.Load(titlePath))
            {
                Assert.That(check.GetImage(0x8000), Is.Null);
            }

            // Repeated runs neither heal nor duplicate the corrupt entry.
            AchievementGpdBuilder.EnsureFromSpa(spa, titlePath, null, XLanguage.English);
            Assert.That(AchievementGpdBuilder.FetchFromSpa(spa, titlePath, false), Is.EqualTo(3));

            using GpdFile repaired = GpdFile.Load(titlePath);
            Assert.That(repaired.Entries.Count(e => e.Namespace == EntryNamespace.Image && e.Id == 0x8000), Is.EqualTo(1));

            // Overwrite-all still heals it.
            Assert.That(AchievementGpdBuilder.FetchFromSpa(spa, titlePath, true), Is.EqualTo(4));
            using GpdFile healed = GpdFile.Load(titlePath);
            Assert.That(healed.GetImage(0x8000)!.IsValidPng, Is.True);
            Assert.That(healed.Entries.Count(e => e.Namespace == EntryNamespace.Image && e.Id == 0x8000), Is.EqualTo(1));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void RefreshStringsFromSpa_UpdatesLanguageAndPreservesProgress()
    {
        string dir = NewTempDir();
        try
        {
            string titlePath = Path.Combine(dir, "4D5309C9.gpd");
            using SpaFile spa = BuildSpa();
            AchievementGpdBuilder.EnsureFromSpa(spa, titlePath, null, XLanguage.English);

            long unlockTime;
            using (GpdFile built = GpdFile.Load(titlePath))
            {
                Assert.That(built.UnlockAchievement(1), Is.True);
                built.Save(titlePath);
                unlockTime = built.GetAchievement(1)!.UnlockTime;
                Assert.That(unlockTime, Is.Not.EqualTo(0));
            }

            int updated = AchievementGpdBuilder.RefreshStringsFromSpa(spa, titlePath, XLanguage.French);
            Assert.That(updated, Is.EqualTo(3));

            using GpdFile refreshed = GpdFile.Load(titlePath);
            AchievementEntry first = refreshed.GetAchievement(1)!;
            Assert.That(first.Name, Is.EqualTo("Premier"));
            Assert.That(first.UnlockedDescription, Is.EqualTo("Premier débloqué"));
            Assert.That(first.IsEarned, Is.True);
            Assert.That(first.UnlockTime, Is.EqualTo(unlockTime));
            Assert.That(first.Gamerscore, Is.EqualTo(100));
            Assert.That(first.ImageId, Is.EqualTo(0x100));

            // Second run finds nothing to change.
            Assert.That(AchievementGpdBuilder.RefreshStringsFromSpa(spa, titlePath, XLanguage.French), Is.EqualTo(0));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void RefreshStringsFromSpa_ResizeFreesOldLength()
    {
        string dir = NewTempDir();
        try
        {
            string titlePath = Path.Combine(dir, "4D5309C9.gpd");
            using SpaFile spa = BuildSpa();
            AchievementGpdBuilder.EnsureFromSpa(spa, titlePath, null, XLanguage.English);

            uint oldOffset;
            uint oldLength;
            using (GpdFile before = GpdFile.Load(titlePath))
            {
                EntryTableEntry row = before.Entries.First(e => e.Namespace == EntryNamespace.Achievement && e.Id == 1);
                oldOffset = row.OffsetSpecifier;
                oldLength = row.Length;
            }

            // French strings are longer, forcing the resize path.
            Assert.That(AchievementGpdBuilder.RefreshStringsFromSpa(spa, titlePath, XLanguage.French), Is.EqualTo(3));

            using GpdFile after = GpdFile.Load(titlePath);
            Assert.That(after.FreeSpaceEntries.Any(f => f.OffsetSpecifier == oldOffset && f.Length == oldLength), Is.True);
            Assert.That(after.GetAchievement(1)!.Name, Is.EqualTo("Premier"));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void RefreshStringsFromSpa_SkipsAchievementsMissingFromSpa()
    {
        string dir = NewTempDir();
        try
        {
            string titlePath = Path.Combine(dir, "4D5309C9.gpd");
            using SpaFile spa = BuildSpa();
            AchievementGpdBuilder.EnsureFromSpa(spa, titlePath, null, XLanguage.English);

            using (GpdFile gpd = GpdFile.Load(titlePath))
            {
                gpd.AddAchievement(new AchievementEntry
                {
                    AchievementId = 99,
                    Gamerscore = 5,
                    Name = "Extra",
                    UnlockedDescription = "Extra unlocked",
                    LockedDescription = "Extra locked"
                });
                gpd.Save(titlePath);
            }

            Assert.That(AchievementGpdBuilder.RefreshStringsFromSpa(spa, titlePath, XLanguage.French), Is.EqualTo(3));

            using GpdFile refreshed = GpdFile.Load(titlePath);
            Assert.That(refreshed.GetAchievement(99)!.Name, Is.EqualTo("Extra"));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Test]
    public void EnsureAchievements_MissingDisc_ThrowsFileNotFound()
    {
        string missing = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.iso");
        Assert.Throws<FileNotFoundException>(() =>
            AchievementGpdBuilder.EnsureAchievements(missing, Path.Combine(Path.GetTempPath(), "x.gpd"), null, XLanguage.English));
    }

    [Test]
    public void EnsureAchievements_DiscWithoutSpa_ThrowsInvalidOperation()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(path, [0x00, 0x01, 0x02, 0x03]);
            Assert.Throws<InvalidOperationException>(() =>
                AchievementGpdBuilder.EnsureAchievements(path, Path.Combine(Path.GetTempPath(), "x.gpd"), null, XLanguage.English));
        }
        finally
        {
            File.Delete(path);
        }
    }
}