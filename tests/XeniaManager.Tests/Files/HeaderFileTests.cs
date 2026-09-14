using System.Reflection;
using XeniaManager.Files;

namespace XeniaManager.Tests.Files;

[TestFixture]
public class HeaderFileTests
{
    private string _testHeaderFile308Path = string.Empty;
    private string _testHeaderFile328Path = string.Empty;
    private string _testHeaderFile332Path = string.Empty;

    [SetUp]
    public void Setup()
    {
        // Assembly Location
        string assemblyLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Assets");

        _testHeaderFile308Path = Path.Combine(assemblyLocation, @"TestHeaderFile (308).header");
        _testHeaderFile328Path = Path.Combine(assemblyLocation, @"TestHeaderFile (328).header");
        _testHeaderFile332Path = Path.Combine(assemblyLocation, @"TestHeaderFile (332).header");
    }

    [Test]
    public void Load_ValidHeaderFile308_ReturnsHeaderFile()
    {
        // Skip if the test file doesn't exist
        if (!File.Exists(_testHeaderFile308Path))
        {
            Assert.Ignore("Header test file (308) not found");
            return;
        }

        // Act
        HeaderFile header = HeaderFile.Load(_testHeaderFile308Path);

        // Assert
        Assert.That(header, Is.Not.Null);
        Assert.That(header.HeaderSize, Is.EqualTo(308));
    }

    [Test]
    public void Load_ValidHeaderFile328_ReturnsHeaderFile()
    {
        // Skip if the test file doesn't exist
        if (!File.Exists(_testHeaderFile328Path))
        {
            Assert.Ignore("Header test file (328) not found");
            return;
        }

        // Act
        HeaderFile header = HeaderFile.Load(_testHeaderFile328Path);

        // Assert
        Assert.That(header, Is.Not.Null);
        Assert.That(header.HeaderSize, Is.EqualTo(328));
    }

    [Test]
    public void Load_ValidHeaderFile332_ReturnsHeaderFile()
    {
        // Skip if the test file doesn't exist
        if (!File.Exists(_testHeaderFile332Path))
        {
            Assert.Ignore("Header test file (332) not found");
            return;
        }

        // Act
        HeaderFile header = HeaderFile.Load(_testHeaderFile332Path);

        // Assert
        Assert.That(header, Is.Not.Null);
        Assert.That(header.HeaderSize, Is.EqualTo(332));
    }

    [TestCaseSource(nameof(GetHeaderTestFiles))]
    public void Load_SaveAndReload_PreservesData(string headerFilePath)
    {
        // Skip if the test file doesn't exist
        if (!File.Exists(headerFilePath))
        {
            Assert.Ignore($"Header test file not found: {headerFilePath}");
            return;
        }

        // Arrange
        string tempFilePath = Path.Combine(Path.GetTempPath(), $"test_header_{Guid.NewGuid()}.header");

        try
        {
            // Load original
            HeaderFile originalHeader = HeaderFile.Load(headerFilePath);

            // Act - Save to new file
            originalHeader.Save(tempFilePath);

            // Reload the saved file
            HeaderFile reloadedHeader = HeaderFile.Load(tempFilePath);

            // Assert - Compare original and reloaded
            Assert.That(reloadedHeader, Is.Not.Null);
            Assert.That(reloadedHeader.HeaderSize, Is.EqualTo(originalHeader.HeaderSize));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Test]
    public void FromBytes_ShortHeader_ReturnsDefaultTitleId()
    {
        // 0x136 bytes: rounds down to the 0x134 base, which carries no title_id field.
        byte[] data = new byte[0x136];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), 1); // device_id
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4), 2); // content_type

        HeaderFile header = HeaderFile.FromBytes(data);

        Assert.That(header.TitleId, Is.EqualTo(0xFFFFFFFF));
    }

    [Test]
    public void FromBytes_AggregateHeader_ReadsTitleAt0x13C()
    {
        // Layout matching Xenia's XCONTENT_DATA_AGGREGATE: XUID at 0x134, title at 0x13C.
        byte[] data = new byte[0x148];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), 1);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(0x134), 0x1122334455667788ul);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x13C), 0x4D5309C9u);

        HeaderFile header = HeaderFile.FromBytes(data);

        Assert.That(header.HeaderSize, Is.EqualTo(0x148));
        Assert.That(header.AccountXuid.Value, Is.EqualTo(0x1122334455667788ul));
        Assert.That(header.TitleId, Is.EqualTo(0x4D5309C9u));
    }

    [Test]
    public void FromBytes_CrossTitleHeader_ReadsTitleAt0x134()
    {
        // 0x138 cross-title headers carry title_id directly after the 0x134 base (no XUID).
        byte[] data = new byte[0x138];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x134), 0x4D5309C9u);

        HeaderFile header = HeaderFile.FromBytes(data);

        Assert.That(header.HeaderSize, Is.EqualTo(0x138));
        Assert.That(header.TitleId, Is.EqualTo(0x4D5309C9u));
    }

    [Test]
    public void ToBytes_CrossTitleHeader_DoesNotThrowAndRoundTrips()
    {
        // Regression: writing a 0x138 header used to index past the buffer.
        HeaderFile header = new HeaderFile
        {
            HeaderSize = 0x138,
            TitleId = 0x4D5309C9u,
            DisplayName = "Test",
            FileName = "test"
        };

        byte[] bytes = header.ToBytes();

        Assert.That(bytes.Length, Is.EqualTo(0x138));
        Assert.That(HeaderFile.FromBytes(bytes).TitleId, Is.EqualTo(0x4D5309C9u));
    }

    [Test]
    public void FromBytes_LegacyTitleAt0x140_FallsBack()
    {
        // Headers written by older revisions stored title_id at 0x140 with 0x13C zeroed.
        byte[] data = new byte[0x14C];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x140), 0x4D5309C9u);

        HeaderFile header = HeaderFile.FromBytes(data);

        Assert.That(header.TitleId, Is.EqualTo(0x4D5309C9u));
    }

    [Test]
    public void ToBytes_FullHeader_WritesTitleAtBothOffsets()
    {
        HeaderFile header = new HeaderFile("4D5309C9", XeniaManager.Files.Models.Stfs.ContentType.GameOnDemand, "pkg", "Game");

        byte[] bytes = header.ToBytes();

        Assert.That(System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(0x13C)), Is.EqualTo(0x4D5309C9u));
        Assert.That(System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(0x140)), Is.EqualTo(0x4D5309C9u));
        Assert.That(HeaderFile.FromBytes(bytes).TitleId, Is.EqualTo(0x4D5309C9u));
    }

    [Test]
    public void ToBytes_FullHeader_RoundTripsTitleAndLicense()
    {
        HeaderFile header = new HeaderFile("4D5309C9", XeniaManager.Files.Models.Stfs.ContentType.GameOnDemand, "pkg", "Game")
        {
            LicenseMask = 0xAABBCCDDu
        };

        HeaderFile reloaded = HeaderFile.FromBytes(header.ToBytes());

        Assert.That(reloaded.TitleId, Is.EqualTo(0x4D5309C9u));
        Assert.That(reloaded.LicenseMask, Is.EqualTo(0xAABBCCDDu));
        Assert.That(reloaded.DisplayName, Is.EqualTo("Game"));
    }

    private static string[] GetHeaderTestFiles()
    {
        return
        [
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Assets", @"TestHeaderFile (308).header"),
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Assets", @"TestHeaderFile (328).header"),
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Assets", @"TestHeaderFile (332).header")
        ];
    }
}