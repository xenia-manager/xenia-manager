using System.Buffers.Binary;
using System.Text;
using XeniaManager.Core.Manage;

namespace XeniaManager.Tests.Core.Manage;

/// <summary>
/// Tests for <see cref="GameManager.DiscoverGameFiles"/> with Games on Demand (GOD / SVOD) layouts:
/// <c>&lt;TitleID&gt;\00007000\&lt;header&gt;</c> plus the sibling <c>&lt;header&gt;.data\Data####</c> folder.
/// </summary>
[TestFixture]
public class GameManagerDiscoverTests
{
    private string _tempDir = null!;

    [SetUp]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"GameManagerDiscoverTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Best-effort cleanup of the temp test directory.
        }
    }

    /// <summary>
    /// Builds a minimal SVOD header: CON/LIVE/PIRS magic with DescriptorType 1 at 0x3A9.
    /// </summary>
    private static byte[] BuildSvodHeader()
    {
        byte[] header = new byte[0x1000];
        Encoding.ASCII.GetBytes("CON ", header.AsSpan(0, 4));
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0x3A9), 1);
        return header;
    }

    private string CreateGodPackage(string titleId)
    {
        string packageDirectory = Path.Combine(_tempDir, titleId, "00007000");
        Directory.CreateDirectory(packageDirectory);

        string headerPath = Path.Combine(packageDirectory, "A5663BF426DAE20DD37ECEACCF3349B135");
        File.WriteAllBytes(headerPath, BuildSvodHeader());

        string dataDirectory = headerPath + ".data";
        Directory.CreateDirectory(dataDirectory);
        File.WriteAllBytes(Path.Combine(dataDirectory, "Data0000"), [0x01, 0x02, 0x03, 0x04]);
        File.WriteAllBytes(Path.Combine(dataDirectory, "Data0001"), [0x05, 0x06, 0x07, 0x08]);

        return headerPath;
    }

    [Test]
    public void DiscoverGameFiles_GodPackage_ReturnsSvodHeader()
    {
        string headerPath = CreateGodPackage("354807D5");

        List<string> gameFiles = GameManager.DiscoverGameFiles(_tempDir);

        Assert.That(gameFiles, Does.Contain(headerPath));
    }

    [Test]
    public void DiscoverGameFiles_GodPackage_DoesNotTraversePackageDataDirectory()
    {
        string headerPath = CreateGodPackage("354807D5");
        string dataDirectory = headerPath + ".data";

        List<string> scannedDirectories = [];
        GameManager.DiscoverGameFiles(_tempDir, default,
            (_, currentDirectory, _, _, _) => scannedDirectories.Add(currentDirectory));

        Assert.That(scannedDirectories, Does.Not.Contain(dataDirectory));
    }
}