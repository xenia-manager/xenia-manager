using XeniaManager.Core.Utilities;

namespace XeniaManager.Tests.Core.Utilities;

public class StorageUtilitiesTests
{
    private string _baseTemp = null!;

    [SetUp]
    public void Setup()
    {
        _baseTemp = Path.Combine(Path.GetTempPath(), $"StorageUtilitiesTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_baseTemp);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_baseTemp))
            {
                Directory.Delete(_baseTemp, true);
            }
        }
        catch { }
    }

    private string CreateSourceWithFiles()
    {
        string src = Path.Combine(_baseTemp, "src");
        Directory.CreateDirectory(src);
        File.WriteAllText(Path.Combine(src, "file1.txt"), "hello");
        File.WriteAllText(Path.Combine(src, "file2.txt"), "world");
        string sub = Path.Combine(src, "sub");
        Directory.CreateDirectory(sub);
        File.WriteAllText(Path.Combine(sub, "nested.txt"), "nested");
        return src;
    }

    [Test]
    public void CopyDirectory_NonRecursive_CopiesOnlyRootFiles()
    {
        string src = CreateSourceWithFiles();
        string dst = Path.Combine(_baseTemp, "dst");
        StorageUtilities.CopyDirectory(src, dst, false);
        Assert.That(File.Exists(Path.Combine(dst, "file1.txt")), Is.True);
        Assert.That(File.Exists(Path.Combine(dst, "file2.txt")), Is.True);
        Assert.That(Directory.Exists(Path.Combine(dst, "sub")), Is.False);
    }

    [Test]
    public void CopyDirectory_Recursive_CopiesSubdirectories()
    {
        string src = CreateSourceWithFiles();
        string dst = Path.Combine(_baseTemp, "dst");
        StorageUtilities.CopyDirectory(src, dst, true);
        Assert.That(File.Exists(Path.Combine(dst, "file1.txt")), Is.True);
        Assert.That(File.Exists(Path.Combine(dst, "sub", "nested.txt")), Is.True);
    }

    [Test]
    public void CopyDirectory_OverwriteFalse_SkipsExisting()
    {
        string src = CreateSourceWithFiles();
        string dst = Path.Combine(_baseTemp, "dst");
        Directory.CreateDirectory(dst);
        File.WriteAllText(Path.Combine(dst, "file1.txt"), "existing");
        StorageUtilities.CopyDirectory(src, dst, false, false);
        Assert.That(File.ReadAllText(Path.Combine(dst, "file1.txt")), Is.EqualTo("existing"));
        Assert.That(File.Exists(Path.Combine(dst, "file2.txt")), Is.True);
    }

    [Test]
    public void CopyDirectory_OverwriteTrue_OverwritesExisting()
    {
        string src = CreateSourceWithFiles();
        string dst = Path.Combine(_baseTemp, "dst");
        Directory.CreateDirectory(dst);
        File.WriteAllText(Path.Combine(dst, "file1.txt"), "existing");
        StorageUtilities.CopyDirectory(src, dst, false, true);
        Assert.That(File.ReadAllText(Path.Combine(dst, "file1.txt")), Is.EqualTo("hello"));
    }

    [Test]
    public void CopyDirectory_DestinationInsideSource_ThrowsArgumentException()
    {
        string src = CreateSourceWithFiles();
        string dst = Path.Combine(src, "inside");
        Assert.Throws<ArgumentException>(() => StorageUtilities.CopyDirectory(src, dst, true));
    }

    [Test]
    public void CopyDirectory_NullSource_ThrowsArgumentException()
    {
        Assert.That(() => StorageUtilities.CopyDirectory(null!, Path.Combine(_baseTemp, "dst"), false), Throws.InstanceOf<ArgumentException>());
        Assert.That(() => StorageUtilities.CopyDirectory("   ", Path.Combine(_baseTemp, "dst"), false), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void CopyDirectory_NullDestination_ThrowsArgumentException()
    {
        string src = CreateSourceWithFiles();
        Assert.That(() => StorageUtilities.CopyDirectory(src, null!, false), Throws.InstanceOf<ArgumentException>());
        Assert.That(() => StorageUtilities.CopyDirectory(src, "   ", false), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void CopyDirectory_NonexistentSource_ThrowsDirectoryNotFoundException()
    {
        string src = Path.Combine(_baseTemp, "nonexistent");
        string dst = Path.Combine(_baseTemp, "dst");
        Assert.Throws<DirectoryNotFoundException>(() => StorageUtilities.CopyDirectory(src, dst, false));
    }

    [Test]
    public void CopyDirectory_CreatesDestinationIfNotExists()
    {
        string src = CreateSourceWithFiles();
        string dst = Path.Combine(_baseTemp, "newDst");
        Assert.That(Directory.Exists(dst), Is.False);
        StorageUtilities.CopyDirectory(src, dst, false);
        Assert.That(Directory.Exists(dst), Is.True);
    }

    [Test]
    public void CopyDirectory_WithCancellation_ThrowsOperationCanceledException()
    {
        string src = CreateSourceWithFiles();
        string dst = Path.Combine(_baseTemp, "dst");
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() => StorageUtilities.CopyDirectory(src, dst, true, cancellationToken: cts.Token));
    }

    [Test]
    public void CopyDirectory_EmptySource_CreatesEmptyDestination()
    {
        string src = Path.Combine(_baseTemp, "empty");
        Directory.CreateDirectory(src);
        string dst = Path.Combine(_baseTemp, "dst");
        StorageUtilities.CopyDirectory(src, dst, true);
        Assert.That(Directory.Exists(dst), Is.True);
        Assert.That(Directory.GetFiles(dst, "*", SearchOption.AllDirectories), Is.Empty);
    }
}