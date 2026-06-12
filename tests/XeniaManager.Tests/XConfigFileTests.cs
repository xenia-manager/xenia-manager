using System.Buffers.Binary;
using System.Reflection;
using System.Text;
using XeniaManager.Core.Files;
using XeniaManager.Core.Models.Files.XConfig;

namespace XeniaManager.Tests;

[TestFixture]
public class XConfigFileTests
{
    private string _testFileLocation = string.Empty;

    [SetUp]
    public void Setup()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), "XConfigTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        _testFileLocation = tempDir;
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(_testFileLocation))
        {
            Directory.Delete(_testFileLocation, recursive: true);
        }
    }

    [Test]
    public void Create_DefaultsAreSet()
    {
        XConfigFile xconfig = XConfigFile.Create();

        Assert.That(xconfig, Is.Not.Null);

        uint language = xconfig.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language);
        Assert.That(language, Is.EqualTo(1u), "Default language should be English (1)");

        uint avRegion = xconfig.ReadSetting<uint>(XConfigCategory.Secured, (ushort)XConfigSecuredSetting.AvRegion);
        Assert.That(avRegion, Is.EqualTo(0x00400100u), "Default AV region should be NTSCM");

        uint audioFlags = xconfig.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.AudioFlags);
        Assert.That(audioFlags, Is.EqualTo(0x00010001u), "Default audio flags should be DolbyDigital | DolbyProLogic");

        byte country = xconfig.ReadSetting<byte>(XConfigCategory.User, (ushort)XConfigUserSetting.Country);
        Assert.That(country, Is.EqualTo(103), "Default country should be United States");

        uint retailFlags = xconfig.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.RetailFlags);
        Assert.That(retailFlags, Is.EqualTo(0x00000040u), "Default retail flags should include DashboardInitialized");

        float musicVolume = xconfig.ReadSetting<float>(XConfigCategory.User, (ushort)XConfigUserSetting.MusicVolume);
        Assert.That(musicVolume, Is.EqualTo(0.7f).Within(0.001f), "Default music volume should be 0.7");
    }

    [Test]
    public void Create_DefaultTimeZoneIsLondon()
    {
        XConfigFile xconfig = XConfigFile.Create();

        int tzBias = xconfig.ReadSetting<int>(XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneBias);
        Assert.That(tzBias, Is.EqualTo(0), "Default timezone bias should be 0 (GMT)");

        int tzDltBias = xconfig.ReadSetting<int>(XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneDltBias);
        Assert.That(tzDltBias, Is.EqualTo(-60), "Default DLT bias should be -60 (BST)");
    }

    [Test]
    public void Create_DefaultSmbConfigIsZero()
    {
        XConfigFile xconfig = XConfigFile.Create();
        byte[] smbConfig = new byte[256];
        xconfig.ReadSetting(XConfigCategory.User, (ushort)XConfigUserSetting.SmbConfig, smbConfig);

        Assert.That(smbConfig, Is.All.EqualTo(0), "Default SMB config should be all zeros");
    }

    [Test]
    public void FromBytes_ValidData_CreatesInstance()
    {
        byte[] data = new byte[XConfigOffsets.TotalSize];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(XConfigOffsets.CategoryOffset(XConfigCategory.User) + 0x02C), 42u);

        XConfigFile xconfig = XConfigFile.FromBytes(data);

        uint language = xconfig.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language);
        Assert.That(language, Is.EqualTo(42u));
    }

    [Test]
    public void FromBytes_NullData_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => XConfigFile.FromBytes(null!));
    }

    [Test]
    public void FromBytes_WrongSize_ThrowsArgumentException()
    {
        byte[] data = new byte[100];
        Assert.Throws<ArgumentException>(() => XConfigFile.FromBytes(data));
    }

    [Test]
    public void ReadWrite_TypedUInt32_RoundTrips()
    {
        XConfigFile xconfig = XConfigFile.Create();

        xconfig.WriteSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language, 7u);
        uint result = xconfig.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language);

        Assert.That(result, Is.EqualTo(7u));
    }

    [Test]
    public void ReadWrite_TypedInt32_RoundTrips()
    {
        XConfigFile xconfig = XConfigFile.Create();

        xconfig.WriteSetting<int>(XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneBias, -480);
        int result = xconfig.ReadSetting<int>(XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneBias);

        Assert.That(result, Is.EqualTo(-480));
    }

    [Test]
    public void ReadWrite_TypedUInt64_RoundTrips()
    {
        XConfigFile xconfig = XConfigFile.Create();

        xconfig.WriteSetting<ulong>(XConfigCategory.User, (ushort)XConfigUserSetting.DefaultProfile, 0xDEADBEEFCAFEBABEul);
        ulong result = xconfig.ReadSetting<ulong>(XConfigCategory.User, (ushort)XConfigUserSetting.DefaultProfile);

        Assert.That(result, Is.EqualTo(0xDEADBEEFCAFEBABEul));
    }

    [Test]
    public void ReadWrite_TypedFloat_RoundTrips()
    {
        XConfigFile xconfig = XConfigFile.Create();

        xconfig.WriteSetting<float>(XConfigCategory.User, (ushort)XConfigUserSetting.MusicVolume, 0.5f);
        float result = xconfig.ReadSetting<float>(XConfigCategory.User, (ushort)XConfigUserSetting.MusicVolume);

        Assert.That(result, Is.EqualTo(0.5f).Within(0.001f));
    }

    [Test]
    public void ReadWrite_TypedByte_RoundTrips()
    {
        XConfigFile xconfig = XConfigFile.Create();

        xconfig.WriteSetting<byte>(XConfigCategory.User, (ushort)XConfigUserSetting.Country, 0xAA);
        byte result = xconfig.ReadSetting<byte>(XConfigCategory.User, (ushort)XConfigUserSetting.Country);

        Assert.That(result, Is.EqualTo(0xAA));
    }

    [Test]
    public void ReadWrite_TypedUInt16_RoundTrips()
    {
        XConfigFile xconfig = XConfigFile.Create();

        xconfig.WriteSetting<ushort>(XConfigCategory.Secured, (ushort)XConfigSecuredSetting.GameRegion, 0x1234);
        ushort result = xconfig.ReadSetting<ushort>(XConfigCategory.Secured, (ushort)XConfigSecuredSetting.GameRegion);

        Assert.That(result, Is.EqualTo(0x1234));
    }

    [Test]
    public void ReadWrite_RawBytes_RoundTrips()
    {
        XConfigFile xconfig = XConfigFile.Create();
        byte[] written = [0xDE, 0xAD, 0xBE, 0xEF];

        xconfig.WriteSetting(XConfigCategory.User, (ushort)XConfigUserSetting.VideoFlags, written);

        byte[] read = new byte[4];
        xconfig.ReadSetting(XConfigCategory.User, (ushort)XConfigUserSetting.VideoFlags, read);

        Assert.That(read, Is.EqualTo(written));
    }

    [Test]
    public void ReadWrite_RawBytesOverLargeField_RoundTrips()
    {
        XConfigFile xconfig = XConfigFile.Create();
        byte[] written = new byte[256];
        for (int i = 0; i < written.Length; i++)
            written[i] = (byte)(i ^ 0xA5);

        xconfig.WriteSetting(XConfigCategory.Console, (ushort)XConfigConsoleSetting.WirelessSettings, written);

        byte[] read = new byte[256];
        xconfig.ReadSetting(XConfigCategory.Console, (ushort)XConfigConsoleSetting.WirelessSettings, read);

        Assert.That(read, Is.EqualTo(written));
    }

    [Test]
    public void ReadSetting_UnknownSetting_ReturnsDefault()
    {
        XConfigFile xconfig = XConfigFile.Create();

        uint result = xconfig.ReadSetting<uint>(XConfigCategory.Statistic, 0xFF);
        Assert.That(result, Is.EqualTo(0u), "Unknown setting should return default value");
    }

    [Test]
    public void WriteSetting_UnknownSetting_DoesNotThrow()
    {
        XConfigFile xconfig = XConfigFile.Create();

        Assert.DoesNotThrow(() =>
            xconfig.WriteSetting<uint>(XConfigCategory.Statistic, 0xFF, 42u));
    }

    [Test]
    public void BigEndian_WriteThenRead_RawBytesCorrect()
    {
        XConfigFile xconfig = XConfigFile.Create();

        xconfig.WriteSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language, 0x01020304u);

        byte[] raw = xconfig.GetRawData();
        int offset = XConfigOffsets.CategoryOffset(XConfigCategory.User) + 0x02C;

        uint stored = BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(offset));
        Assert.That(stored, Is.EqualTo(0x01020304u), "Data should be stored in big-endian format");
    }

    [Test]
    public void SaveAndLoad_RoundTrips()
    {
        string filePath = Path.Combine(_testFileLocation, "xconfig.settings");

        XConfigFile original = XConfigFile.Create();
        original.WriteSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language, 9u);
        original.Save(filePath);

        Assert.That(File.Exists(filePath), Is.True);

        XConfigFile loaded = XConfigFile.Load(filePath);
        uint language = loaded.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language);

        Assert.That(language, Is.EqualTo(9u), "Language should persist after save and load");

        uint avRegion = loaded.ReadSetting<uint>(XConfigCategory.Secured, (ushort)XConfigSecuredSetting.AvRegion);
        Assert.That(avRegion, Is.EqualTo(0x00400100u), "Default AV region should persist after save and load");
    }

    [Test]
    public void Load_NonExistentFile_CreatesWithDefaults()
    {
        string filePath = Path.Combine(_testFileLocation, "new_xconfig.settings");

        XConfigFile xconfig = XConfigFile.Load(filePath);

        Assert.That(xconfig, Is.Not.Null);
        Assert.That(File.Exists(filePath), Is.True, "Load should create the file with defaults");

        uint language = xconfig.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language);
        Assert.That(language, Is.EqualTo(1u), "Auto-created file should have defaults");
    }

    [Test]
    public void Load_ExistentFile_ReadsCorrectly()
    {
        string filePath = Path.Combine(_testFileLocation, "xconfig.settings");
        XConfigFile original = XConfigFile.Create();
        original.WriteSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language, 5u);
        original.Save(filePath);

        XConfigFile loaded = XConfigFile.Load(filePath);

        uint language = loaded.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language);
        Assert.That(language, Is.EqualTo(5u));
    }

    [Test]
    public void SaveToNewPath_UpdatesFilePath()
    {
        XConfigFile xconfig = XConfigFile.Create();
        string path1 = Path.Combine(_testFileLocation, "path1.bin");
        string path2 = Path.Combine(_testFileLocation, "path2.bin");

        xconfig.Save(path1);
        Assert.That(File.Exists(path1), Is.True);

        xconfig.Save(path2);
        Assert.That(File.Exists(path2), Is.True);
        Assert.That(xconfig.FilePath, Is.EqualTo(path2));
    }

    [Test]
    public void Save_WithoutPath_ThrowsInvalidOperationException()
    {
        XConfigFile xconfig = XConfigFile.Create();

        Assert.Throws<InvalidOperationException>(() => xconfig.Save());
    }

    [Test]
    public void GetSettingSize_RegisteredFields_ReturnsCorrectSize()
    {
        XConfigFile xconfig = XConfigFile.Create();

        Assert.That(xconfig.GetSettingSize(XConfigCategory.User, (ushort)XConfigUserSetting.Language), Is.EqualTo(4));
        Assert.That(xconfig.GetSettingSize(XConfigCategory.User, (ushort)XConfigUserSetting.Country), Is.EqualTo(1));
        Assert.That(xconfig.GetSettingSize(XConfigCategory.User, (ushort)XConfigUserSetting.DefaultProfile), Is.EqualTo(8));
        Assert.That(xconfig.GetSettingSize(XConfigCategory.Secured, (ushort)XConfigSecuredSetting.MacAddress), Is.EqualTo(6));
        Assert.That(xconfig.GetSettingSize(XConfigCategory.Console, (ushort)XConfigConsoleSetting.WirelessSettings), Is.EqualTo(256));
    }

    [Test]
    public void GetSettingSize_UnknownSetting_ReturnsZero()
    {
        XConfigFile xconfig = XConfigFile.Create();

        Assert.That(xconfig.GetSettingSize(XConfigCategory.Statistic, 0xFF), Is.EqualTo(0));
    }

    [Test]
    public void GetRawData_ReturnsClone()
    {
        XConfigFile xconfig = XConfigFile.Create();
        byte[] data1 = xconfig.GetRawData();
        byte[] data2 = xconfig.GetRawData();

        Assert.That(data1, Is.EqualTo(data2));
        Assert.That(data1, Is.Not.SameAs(data2), "GetRawData should return a new copy each time");
    }

    [Test]
    public void GetRawData_ReflectsWrittenValues()
    {
        XConfigFile xconfig = XConfigFile.Create();
        xconfig.WriteSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language, 0xAABBCCDDu);

        byte[] raw = xconfig.GetRawData();
        int offset = XConfigOffsets.CategoryOffset(XConfigCategory.User) + 0x02C;

        Assert.That(BinaryPrimitives.ReadUInt32BigEndian(raw.AsSpan(offset)), Is.EqualTo(0xAABBCCDDu));
    }

    [Test]
    public void AllRegisteredFields_RoundTrip()
    {
        XConfigFile xconfig = XConfigFile.Create();

        var fields = new (XConfigCategory Cat, ushort Id, int Size, object Value)[]
        {
            (XConfigCategory.Static, (ushort)XConfigStaticSetting.FirstPowerOnDate, 5, new byte[] { 1, 2, 3, 4, 5 }),
            (XConfigCategory.Secured, (ushort)XConfigSecuredSetting.OnlineNetworkId, 4, 0x01020304u),
            (XConfigCategory.Secured, (ushort)XConfigSecuredSetting.AvRegion, 4, 0xDEADBEEFu),
            (XConfigCategory.Secured, (ushort)XConfigSecuredSetting.GameRegion, 2, (ushort)0x1234),
            (XConfigCategory.Secured, (ushort)XConfigSecuredSetting.SystemFlags, 4, 0xCAFEBABEu),
            (XConfigCategory.Secured, (ushort)XConfigSecuredSetting.ResetKey, 4, 0x12345678u),
            (XConfigCategory.Console, (ushort)XConfigConsoleSetting.ScreenSaver, 2, (short)-123),
            (XConfigCategory.Console, (ushort)XConfigConsoleSetting.AutoShutOff, 2, (short)456),
            (XConfigCategory.Console, (ushort)XConfigConsoleSetting.KeyboardLayout, 2, (short)0x7F),
            (XConfigCategory.System, (ushort)XConfigSystemSetting.AlarmTime, 8, 0xABCDEF0123456789ul),
            (XConfigCategory.System, (ushort)XConfigSystemSetting.PreviousFlashVersion, 4, 0x00010002u),
        };

        foreach (var (cat, id, size, value) in fields)
        {
            switch (value)
            {
                case uint u:
                    xconfig.WriteSetting<uint>(cat, id, u);
                    Assert.That(xconfig.ReadSetting<uint>(cat, id), Is.EqualTo(u));
                    break;
                case ushort us:
                    xconfig.WriteSetting<ushort>(cat, id, us);
                    Assert.That(xconfig.ReadSetting<ushort>(cat, id), Is.EqualTo(us));
                    break;
                case short s:
                    xconfig.WriteSetting<short>(cat, id, s);
                    Assert.That(xconfig.ReadSetting<short>(cat, id), Is.EqualTo(s));
                    break;
                case ulong ul:
                    xconfig.WriteSetting<ulong>(cat, id, ul);
                    Assert.That(xconfig.ReadSetting<ulong>(cat, id), Is.EqualTo(ul));
                    break;
                case byte[] bytes:
                    xconfig.WriteSetting(cat, id, bytes);
                    byte[] read = new byte[bytes.Length];
                    xconfig.ReadSetting(cat, id, read);
                    Assert.That(read, Is.EqualTo(bytes));
                    break;
            }
        }
    }

    [Test]
    public void ThreadSafety_ConcurrentReadsDoNotThrow()
    {
        XConfigFile xconfig = XConfigFile.Create();
        int threadCount = 8;
        int iterationsPerThread = 100;

        var tasks = new Task[threadCount];
        for (int t = 0; t < threadCount; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    uint language = xconfig.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language);
                    uint flags = xconfig.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.VideoFlags);
                    float volume = xconfig.ReadSetting<float>(XConfigCategory.User, (ushort)XConfigUserSetting.MusicVolume);
                }
            });
        }

        Assert.DoesNotThrowAsync(() => Task.WhenAll(tasks));
    }

    [Test]
    public void ThreadSafety_ConcurrentWritesDoNotThrow()
    {
        XConfigFile xconfig = XConfigFile.Create();
        int threadCount = 4;
        int iterationsPerThread = 50;
        int completedWrites = 0;
        object counterLock = new();

        var tasks = new Task[threadCount];
        for (int t = 0; t < threadCount; t++)
        {
            uint threadValue = (uint)(t + 1);
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    xconfig.WriteSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language, threadValue);
                    // Every write flushes to a single temp file; just verify no exceptions
                }

                lock (counterLock)
                {
                    completedWrites++;
                }
            });
        }

        Assert.DoesNotThrowAsync(() => Task.WhenAll(tasks));
        Assert.That(completedWrites, Is.EqualTo(threadCount), "All threads should complete without throwing");

        uint finalValue = xconfig.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language);
        Assert.That(finalValue, Is.GreaterThan(0u).And.LessThanOrEqualTo((uint)threadCount),
            "Final value should be one of the thread values");
    }

    [Test]
    public void SaveAndLoad_PreservesAllDefaults()
    {
        string filePath = Path.Combine(_testFileLocation, "xconfig.settings");

        XConfigFile original = XConfigFile.Create();
        original.Save(filePath);

        XConfigFile loaded = XConfigFile.Load(filePath);

        Assert.That(loaded.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language), Is.EqualTo(1u));
        Assert.That(loaded.ReadSetting<uint>(XConfigCategory.Secured, (ushort)XConfigSecuredSetting.AvRegion), Is.EqualTo(0x00400100u));
        Assert.That(loaded.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.AudioFlags), Is.EqualTo(0x00010001u));
        Assert.That(loaded.ReadSetting<byte>(XConfigCategory.User, (ushort)XConfigUserSetting.Country), Is.EqualTo(103));
        Assert.That(loaded.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.RetailFlags), Is.EqualTo(0x00000040u));
        Assert.That(loaded.ReadSetting<float>(XConfigCategory.User, (ushort)XConfigUserSetting.MusicVolume), Is.EqualTo(0.7f).Within(0.001f));
        Assert.That(loaded.ReadSetting<int>(XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneBias), Is.EqualTo(0));
        Assert.That(loaded.ReadSetting<int>(XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneDltBias), Is.EqualTo(-60));
    }

    [Test]
    public void Load_TestAsset_HasExpectedValues()
    {
        string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string assetPath = Path.Combine(assemblyLocation, "Assets", "TestXConfig.settings");

        Assert.That(File.Exists(assetPath), Is.True, $"Test asset does not exist at {assetPath}");

        XConfigFile xconfig = XConfigFile.Load(assetPath);

        Assert.That(xconfig, Is.Not.Null);
        Assert.That(xconfig.FilePath, Is.EqualTo(assetPath));

        // Default values preserved from SetDefaults()
        Assert.That(xconfig.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language), Is.EqualTo(1u), "Language should be English (1)");
        Assert.That(xconfig.ReadSetting<uint>(XConfigCategory.Secured, (ushort)XConfigSecuredSetting.AvRegion), Is.EqualTo(0x00400100u), "AV region should be NTSCM");
        Assert.That(xconfig.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.AudioFlags), Is.EqualTo(0x00010001u), "Audio should be DolbyDigital | DolbyProLogic");
        Assert.That(xconfig.ReadSetting<float>(XConfigCategory.User, (ushort)XConfigUserSetting.MusicVolume), Is.EqualTo(0.7f).Within(0.001f), "Music volume should be 0.7");
        Assert.That(xconfig.ReadSetting<int>(XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneDltBias), Is.EqualTo(-60), "DLT bias should be -60 (BST)");

        // PcGame set to 0x000000FF by defaults
        Assert.That(xconfig.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.PcGame), Is.EqualTo(0x000000FFu), "PC game should be NoGameRestrictions");
        Assert.That(xconfig.ReadSetting<byte>(XConfigCategory.User, (ushort)XConfigUserSetting.PcFlags), Is.EqualTo(0x03), "PC flags should be XBLAllowed | XBLMembershipCreationAllowed");

        // Modified values in test asset (deviations from defaults)
        Assert.That(xconfig.ReadSetting<byte>(XConfigCategory.User, (ushort)XConfigUserSetting.Country), Is.EqualTo(0x67), "Country should be 0x67 (103) in test asset");
        Assert.That(xconfig.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.RetailFlags), Is.EqualTo(0x00000000u), "Retail flags should be 0 in test asset");

        // Timezone strings are fixed byte buffers, not null-terminated strings
        byte[] tzStdName = new byte[4];
        byte[] tzDltName = new byte[4];
        xconfig.ReadSetting(XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneStdName, tzStdName);
        xconfig.ReadSetting(XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneDltName, tzDltName);
        Assert.That(Encoding.ASCII.GetString(tzStdName).TrimEnd('\0'), Is.EqualTo("GMT"));
        Assert.That(Encoding.ASCII.GetString(tzDltName).TrimEnd('\0'), Is.EqualTo("BST"));

        // Values that were not set should be zero
        Assert.That(xconfig.ReadSetting<ushort>(XConfigCategory.Secured, (ushort)XConfigSecuredSetting.GameRegion), Is.EqualTo(0), "Game region should be 0 (unset)");
        Assert.That(xconfig.ReadSetting<uint>(XConfigCategory.Secured, (ushort)XConfigSecuredSetting.DvdRegion), Is.EqualTo(0u), "DVD region should be 0 (unset)");
        Assert.That(xconfig.ReadSetting<uint>(XConfigCategory.Secured, (ushort)XConfigSecuredSetting.ResetKey), Is.EqualTo(0u), "Reset key should be 0 (unset)");
        Assert.That(xconfig.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.PcGame), Is.EqualTo(0x000000FFu), "PC game should be NoGameRestrictions");
    }

    [Test]
    public void Load_TestAsset_SaveAndReloadPreservesValues()
    {
        string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        string assetPath = Path.Combine(assemblyLocation, "Assets", "TestXConfig.settings");
        string copyPath = Path.Combine(_testFileLocation, "xconfig.settings");

        XConfigFile original = XConfigFile.Load(assetPath);
        uint originalLanguage = original.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language);
        byte originalCountry = original.ReadSetting<byte>(XConfigCategory.User, (ushort)XConfigUserSetting.Country);

        original.Save(copyPath);

        XConfigFile reloaded = XConfigFile.Load(copyPath);

        Assert.That(reloaded.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language), Is.EqualTo(originalLanguage));
        Assert.That(reloaded.ReadSetting<byte>(XConfigCategory.User, (ushort)XConfigUserSetting.Country), Is.EqualTo(originalCountry));
        Assert.That(reloaded.ReadSetting<float>(XConfigCategory.User, (ushort)XConfigUserSetting.MusicVolume), Is.EqualTo(0.7f).Within(0.001f));
        Assert.That(reloaded.ReadSetting<int>(XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneDltBias), Is.EqualTo(-60));
    }

    // ── Property-based API tests ─────────────────────────────────────────

    [Test]
    public void Properties_DefaultsMatchSetDefaults()
    {
        XConfigFile xconfig = XConfigFile.Create();

        Assert.That(xconfig.Language, Is.EqualTo(XLanguage.English));
        Assert.That(xconfig.AudioFlags, Is.EqualTo(XAudioFlags.DolbyDigital | XAudioFlags.DolbyProLogic));
        Assert.That(xconfig.VideoFlags, Is.EqualTo(XVideoFlags.RatioNormal));
        Assert.That(xconfig.Country, Is.EqualTo(XOnlineCountry.UnitedStates));
        Assert.That(xconfig.MusicVolume, Is.EqualTo(0.7f).Within(0.001f));
        Assert.That(xconfig.TimeZoneBias, Is.EqualTo(0));
        Assert.That(xconfig.TimeZoneDltBias, Is.EqualTo(-60));
        Assert.That(xconfig.RetailFlags, Is.EqualTo(XRetailFlags.DashboardInitialized));
        Assert.That(xconfig.DefaultProfile, Is.EqualTo(0ul));
        Assert.That(xconfig.VideoOutputBlackLevels, Is.EqualTo((XBlackLevel)0));
        Assert.That(xconfig.AvRegion, Is.EqualTo(XAvRegion.NtscM));
        Assert.That(xconfig.GameRegion, Is.EqualTo((ushort)0));
        Assert.That(xconfig.DvdRegion, Is.EqualTo(0u));
        Assert.That(xconfig.ResetKey, Is.EqualTo(0u));
        Assert.That(xconfig.SystemFlags, Is.EqualTo(0u));
        Assert.That(xconfig.ScreenSaver, Is.EqualTo((XScreenSaver)0));
        Assert.That(xconfig.AutoShutOff, Is.EqualTo(XAutoShutOff.Off));
        Assert.That(xconfig.KeyboardLayout, Is.EqualTo(XKeyboardLayout.Default));
        Assert.That(xconfig.AlarmTime, Is.EqualTo(0ul));
        Assert.That(xconfig.PreviousFlashVersion, Is.EqualTo(0u));
    }

    [Test]
    public void Properties_WriteThenRead_RoundTrips()
    {
        XConfigFile xconfig = XConfigFile.Create();

        xconfig.Language = XLanguage.Spanish;
        xconfig.AudioFlags = (XAudioFlags)0xAABBCCDDu;
        xconfig.VideoFlags = (XVideoFlags)0x12345678u;
        xconfig.Country = (XOnlineCountry)0x42;
        xconfig.MusicVolume = 0.3f;
        xconfig.TimeZoneBias = -480;
        xconfig.TimeZoneDltBias = -420;
        xconfig.RetailFlags = XRetailFlags.DashboardInitialized;
        xconfig.DefaultProfile = 0xDEADBEEFCAFEBABEul;
        xconfig.VideoOutputBlackLevels = (XBlackLevel)2u;
        xconfig.AvRegion = XAvRegion.NtscJ;
        xconfig.GameRegion = 0x7FFF;
        xconfig.DvdRegion = 0x00000001u;
        xconfig.ResetKey = 0x12345678u;
        xconfig.SystemFlags = 0x0000000Fu;
        xconfig.ScreenSaver = (XScreenSaver)(-1);
        xconfig.AutoShutOff = (XAutoShutOff)300;
        xconfig.KeyboardLayout = (XKeyboardLayout)0x0409;
        xconfig.AlarmTime = 0x0000000100000001ul;
        xconfig.PreviousFlashVersion = 0x00020003u;

        Assert.That(xconfig.Language, Is.EqualTo(XLanguage.Spanish));
        Assert.That(xconfig.AudioFlags, Is.EqualTo((XAudioFlags)0xAABBCCDDu));
        Assert.That(xconfig.VideoFlags, Is.EqualTo((XVideoFlags)0x12345678u));
        Assert.That(xconfig.Country, Is.EqualTo((XOnlineCountry)0x42));
        Assert.That(xconfig.MusicVolume, Is.EqualTo(0.3f).Within(0.001f));
        Assert.That(xconfig.TimeZoneBias, Is.EqualTo(-480));
        Assert.That(xconfig.TimeZoneDltBias, Is.EqualTo(-420));
        Assert.That(xconfig.RetailFlags, Is.EqualTo(XRetailFlags.DashboardInitialized));
        Assert.That(xconfig.DefaultProfile, Is.EqualTo(0xDEADBEEFCAFEBABEul));
        Assert.That(xconfig.VideoOutputBlackLevels, Is.EqualTo((XBlackLevel)2u));
        Assert.That(xconfig.AvRegion, Is.EqualTo(XAvRegion.NtscJ));
        Assert.That(xconfig.GameRegion, Is.EqualTo(0x7FFF));
        Assert.That(xconfig.DvdRegion, Is.EqualTo(1u));
        Assert.That(xconfig.ResetKey, Is.EqualTo(0x12345678u));
        Assert.That(xconfig.SystemFlags, Is.EqualTo(0x0000000Fu));
        Assert.That(xconfig.ScreenSaver, Is.EqualTo((XScreenSaver)(-1)));
        Assert.That(xconfig.AutoShutOff, Is.EqualTo((XAutoShutOff)300));
        Assert.That(xconfig.KeyboardLayout, Is.EqualTo((XKeyboardLayout)0x0409));
        Assert.That(xconfig.AlarmTime, Is.EqualTo(0x0000000100000001ul));
        Assert.That(xconfig.PreviousFlashVersion, Is.EqualTo(0x00020003u));
    }

    [Test]
    public void Properties_ModifyThenSaveThenLoad_PreservesValues()
    {
        string filePath = Path.Combine(_testFileLocation, "xconfig.settings");

        XConfigFile xconfig = XConfigFile.Create();
        xconfig.Language = XLanguage.TChinese;
        xconfig.Country = (XOnlineCountry)0x55;
        xconfig.MusicVolume = 1.0f;
        xconfig.AvRegion = XAvRegion.Pal;
        xconfig.AvHdmiScreenSize = XConfigResolution.R1920x1080;
        xconfig.AvComponentScreenSize = XConfigResolution.R1920x1080;
        xconfig.AvVgaScreenSize = XConfigResolution.R1920x1080;
        xconfig.DefaultProfile = 0xB13EBABEBABEBABEul;
        xconfig.Save(filePath);

        XConfigFile loaded = XConfigFile.Load(filePath);
        Assert.That(loaded.Language, Is.EqualTo(XLanguage.TChinese));
        Assert.That(loaded.Country, Is.EqualTo((XOnlineCountry)0x55));
        Assert.That(loaded.MusicVolume, Is.EqualTo(1.0f).Within(0.001f));
        Assert.That(loaded.AvRegion, Is.EqualTo(XAvRegion.Pal));
        Assert.That(loaded.AvHdmiScreenSize, Is.EqualTo(XConfigResolution.R1920x1080));
        Assert.That(loaded.AvComponentScreenSize, Is.EqualTo(XConfigResolution.R1920x1080));
        Assert.That(loaded.AvVgaScreenSize, Is.EqualTo(XConfigResolution.R1920x1080));
        Assert.That(loaded.DefaultProfile, Is.EqualTo(0xB13EBABEBABEBABEul));
    }

    [Test]
    public void Properties_PropertyAndReadSetting_Agree()
    {
        XConfigFile xconfig = XConfigFile.Create();

        xconfig.Language = XLanguage.Korean;
        XLanguage fromProperty = xconfig.Language;
        uint fromMethod = xconfig.ReadSetting<uint>(XConfigCategory.User, (ushort)XConfigUserSetting.Language);

        Assert.That((uint)fromProperty, Is.EqualTo(fromMethod));
    }

    [Test]
    public void Properties_ResolutionDefaults()
    {
        XConfigFile xconfig = XConfigFile.Create();
        Assert.That(xconfig.AvHdmiScreenSize, Is.EqualTo(XConfigFile.PackResolution(1280, 720)));
        Assert.That(xconfig.AvComponentScreenSize, Is.EqualTo(XConfigFile.PackResolution(1280, 720)));
        Assert.That(xconfig.AvVgaScreenSize, Is.EqualTo(XConfigFile.PackResolution(720, 576)));
    }

    [Test]
    public void Properties_ResolutionRoundTrip()
    {
        XConfigFile xconfig = XConfigFile.Create();
        xconfig.AvHdmiScreenSize = XConfigFile.PackResolution(1920, 1080);
        xconfig.AvComponentScreenSize = XConfigFile.PackResolution(640, 480);
        xconfig.AvVgaScreenSize = XConfigFile.PackResolution(1024, 768);
        Assert.That(XConfigFile.UnpackWidth(xconfig.AvHdmiScreenSize), Is.EqualTo(1920));
        Assert.That(XConfigFile.UnpackHeight(xconfig.AvHdmiScreenSize), Is.EqualTo(1080));
        Assert.That(XConfigFile.UnpackWidth(xconfig.AvComponentScreenSize), Is.EqualTo(640));
        Assert.That(XConfigFile.UnpackHeight(xconfig.AvComponentScreenSize), Is.EqualTo(480));
        Assert.That(XConfigFile.UnpackWidth(xconfig.AvVgaScreenSize), Is.EqualTo(1024));
        Assert.That(XConfigFile.UnpackHeight(xconfig.AvVgaScreenSize), Is.EqualTo(768));
    }

    [Test]
    public void Properties_PcFlagsDefaults()
    {
        XConfigFile xconfig = XConfigFile.Create();
        Assert.That(xconfig.PcFlags, Is.EqualTo(XPcFlags.XblAllowed | XPcFlags.XblMembershipCreationAllowed));
        Assert.That(xconfig.IsXblAllowed, Is.True);
        Assert.That(xconfig.IsXblMembershipCreationAllowed, Is.True);
        Assert.That(xconfig.IsXboxOneGameAllowed, Is.False);
        Assert.That(xconfig.IsPcEnabled, Is.False);
    }

    [Test]
    public void Properties_PcFlagsRoundTrip()
    {
        XConfigFile xconfig = XConfigFile.Create();
        xconfig.IsXboxOneGameAllowed = true;
        xconfig.IsPcEnabled = true;
        Assert.That(xconfig.IsXboxOneGameAllowed, Is.True);
        Assert.That(xconfig.IsPcEnabled, Is.True);
        Assert.That(xconfig.PcFlags, Is.EqualTo(XPcFlags.XblAllowed | XPcFlags.XblMembershipCreationAllowed | XPcFlags.XboxOneGameAllowed | XPcFlags.PcEnabled));
        xconfig.IsXblAllowed = false;
        Assert.That(xconfig.IsXblAllowed, Is.False);
        Assert.That(xconfig.IsXblMembershipCreationAllowed, Is.True);
    }

    [Test]
    public void Properties_NetworkDefaults()
    {
        XConfigFile xconfig = XConfigFile.Create();
        Assert.That(xconfig.OnlineNetworkId, Is.EqualTo(0u));
        byte[] mac = xconfig.MacAddress;
        Assert.That(mac, Has.Length.EqualTo(6));
        Assert.That(mac, Is.All.EqualTo(0));
    }

    [Test]
    public void Properties_NetworkRoundTrip()
    {
        XConfigFile xconfig = XConfigFile.Create();
        xconfig.OnlineNetworkId = 0xDEADBEEFu;
        xconfig.MacAddress = [0x00, 0x1A, 0x38, 0x4B, 0x9C, 0x2D];
        Assert.That(xconfig.OnlineNetworkId, Is.EqualTo(0xDEADBEEFu));
        Assert.That(xconfig.MacAddress, Is.EqualTo(new byte[] { 0x00, 0x1A, 0x38, 0x4B, 0x9C, 0x2D }));
    }

    [Test]
    public void Properties_MacAddress_RejectsInvalidLength()
    {
        XConfigFile xconfig = XConfigFile.Create();
        Assert.That(() => xconfig.MacAddress = [0x01, 0x02, 0x03], Throws.ArgumentException);
        Assert.That(() => xconfig.MacAddress = null!, Throws.ArgumentNullException);
    }
}