using System.Buffers.Binary;
using XeniaManager.Files;

namespace XeniaManager.Tests.Files;

/// <summary>
/// Header-only XEX parsing must yield the same IDs as full parsing
/// without retaining the executable bytes.
/// </summary>
[TestFixture]
public class XexHeaderOnlyTests
{
    private static byte[] BuildMinimalXex(uint titleId, int peSize = 0x2000, uint mediaId = 0x12345678)
    {
        const uint headerSize = 0x300;
        const uint securityOffset = 0x40;
        const int executionInfoOffset = 0x40 + 0x184;
        int totalSize = (int)headerSize + peSize;
        byte[] xex = new byte[totalSize];
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x00), 0x58455832u);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x08), headerSize);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x10), securityOffset);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x14), 1);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x18), 0x00040006u);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x1C), (uint)executionInfoOffset);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x04), (uint)peSize);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x00), mediaId);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x0C), titleId);
        xex[(int)headerSize] = 0x4D;
        xex[(int)headerSize + 1] = 0x5A;
        return xex;
    }

    [Test]
    public void LoadHeaderOnly_MatchesFullParseIds()
    {
        byte[] xexBytes = BuildMinimalXex(0x4D530910);
        string path = Path.Combine(Path.GetTempPath(), $"xex_hdr_{Guid.NewGuid():N}.xex");
        File.WriteAllBytes(path, xexBytes);
        try
        {
            XexFile full = XexFile.FromBytes(xexBytes);
            Assume.That(full.IsValid, Is.True);

            XexFile header = XexFile.LoadHeaderOnly(path);
            Assert.That(header.IsValid, Is.True);
            Assert.That(header.IsHeaderOnly, Is.True);
            Assert.That(header.TitleId, Is.EqualTo(full.TitleId));
            Assert.That(header.MediaId, Is.EqualTo(full.MediaId));
            // Every text field the browse details pane shows must match the full parse.
            Assert.That(header.Execution, Is.EqualTo(full.Execution));
            Assert.That(header.SecurityInfo.ImageSize, Is.EqualTo(full.SecurityInfo.ImageSize));
            Assert.That(header.Header.ModuleFlags, Is.EqualTo(full.Header.ModuleFlags));
            Assert.That(full.IsHeaderOnly, Is.False);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void ReleaseRawData_ClearsRetainedBytes()
    {
        byte[] xexBytes = BuildMinimalXex(0x4D530910);
        XexFile xex = XexFile.FromBytes(xexBytes);
        Assume.That(xex.IsValid, Is.True);
        xex.ReleaseRawData();
        Assert.That(xex.RawData.Count, Is.EqualTo(0));
        Assert.That(xex.TitleId, Is.EqualTo("4D530910"));
    }
}