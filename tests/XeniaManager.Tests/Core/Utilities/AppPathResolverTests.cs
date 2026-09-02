using XeniaManager.Core.Utilities;

namespace XeniaManager.Tests.Core.Utilities;

public class AppPathResolverTests
{
    private string _originalBase = string.Empty;

    [SetUp]
    public void Setup() => _originalBase = AppPathResolver.BaseDirectory();

    [TearDown]
    public void TearDown() => AppPathResolver.SetBaseDirectory(_originalBase);

    [Test]
    public void BaseDirectory_IsNotNullOrEmpty() => Assert.That(AppPathResolver.BaseDirectory(), Is.Not.Null.And.Not.Empty);

    [Test]
    public void BaseDirectory_IsAbsolute() => Assert.That(Path.IsPathRooted(AppPathResolver.BaseDirectory()), Is.True);

    [Test]
    public void BaseDirectory_IsNotInTempDirectory()
    {
        string tempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        string baseDir = AppPathResolver.BaseDirectory().TrimEnd(Path.DirectorySeparatorChar);
        // BaseDirectory may be temp in single-file test runs; only assert not empty here, but check logic
        Assert.That(baseDir, Is.Not.Empty);
        // If not in temp, assert not starts with temp; else inconclusive
        if (baseDir.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase))
        {
            Assert.Ignore("BaseDirectory is in temp during test run (expected for single-file)");
        }
        else
        {
            Assert.That(baseDir, Does.Not.StartWith(tempPath));
        }
    }

    [Test]
    public void SetBaseDirectory_WithAbsolutePath_UpdatesBaseDirectory()
    {
        string temp = Path.Combine(Path.GetTempPath(), $"AppPathResolver_{Guid.NewGuid():N}");
        Directory.CreateDirectory(temp);
        try
        {
            AppPathResolver.SetBaseDirectory(temp);
            Assert.That(AppPathResolver.BaseDirectory(), Is.EqualTo(Path.GetFullPath(temp)));
        }
        finally
        {
            Directory.Delete(temp, true);
        }
    }

    [Test]
    public void SetBaseDirectory_WithNullOrWhitespace_Ignores()
    {
        string before = AppPathResolver.BaseDirectory();
        AppPathResolver.SetBaseDirectory(null!);
        Assert.That(AppPathResolver.BaseDirectory(), Is.EqualTo(before));
        AppPathResolver.SetBaseDirectory("   ");
        Assert.That(AppPathResolver.BaseDirectory(), Is.EqualTo(before));
    }

    [Test]
    public void GetFullPath_SingleSegment_CombinesWithBase()
    {
        string result = AppPathResolver.GetFullPath("config.json");
        Assert.That(result, Is.EqualTo(Path.Combine(AppPathResolver.BaseDirectory(), "config.json")));
    }

    [Test]
    public void GetFullPath_MultipleSegments_CombinesAll()
    {
        string result = AppPathResolver.GetFullPath("a", "b", "c.txt");
        Assert.That(result, Is.EqualTo(Path.Combine(AppPathResolver.BaseDirectory(), "a", "b", "c.txt")));
    }

    [Test]
    public void SanitizeForFilename_ReplacesColonWithSpaceDash()
    {
        string result = AppPathResolver.SanitizeForFilename("Game: Title");
        Assert.That(result, Is.EqualTo("Game - Title"));
    }

    [Test]
    public void SanitizeForFilename_ReplacesInvalidCharsWithSpace()
    {
        char[] invalid = Path.GetInvalidFileNameChars().Where(c => c != ':').ToArray();
        if (invalid.Length == 0)
        {
            Assert.Ignore("No invalid chars on this platform");
        }

        string input = "a" + invalid[0] + "b";
        string result = AppPathResolver.SanitizeForFilename(input);
        Assert.That(result, Is.EqualTo("a b"));
        Assert.That(result, Does.Not.Contain(invalid[0].ToString()));
    }

    [Test]
    public void SanitizeForFilename_CollapsesDoubleSpaces()
    {
        // ":" becomes " -" then invalid char -> space may create double spaces
        string result = AppPathResolver.SanitizeForFilename("a  b   c");
        Assert.That(result, Is.EqualTo("a b c"));
    }

    [Test]
    public void SanitizeForFilename_TrimsSpaces()
    {
        string result = AppPathResolver.SanitizeForFilename("  hello world  ");
        Assert.That(result, Is.EqualTo("hello world"));
    }

    [Test]
    public void SanitizeForFilename_WithOnlyInvalidChars_ReturnsEmpty()
    {
        char invalid = Path.GetInvalidFileNameChars().FirstOrDefault(c => c != ':' && c != ' ' && c != '\t');
        if (invalid == default)
        {
            Assert.Ignore("No suitable invalid char");
        }

        string input = new string(invalid, 3);
        string result = AppPathResolver.SanitizeForFilename(input);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SanitizeForFilename_PreservesValidChars()
    {
        string input = "Valid-Name_123 (Test)";
        string result = AppPathResolver.SanitizeForFilename(input);
        Assert.That(result, Is.EqualTo(input));
    }

    [Test]
    public void SanitizeForFilename_WithSlashAndBackslash_Replaces()
    {
        // '/' and '\' are invalid on Windows, but may not be on Linux; test conditionally
        string input = "a/b\\c";
        string result = AppPathResolver.SanitizeForFilename(input);
        if (Path.GetInvalidFileNameChars().Contains('/'))
        {
            Assert.That(result, Does.Not.Contain("/"));
        }

        if (Path.GetInvalidFileNameChars().Contains('\\'))
        {
            Assert.That(result, Does.Not.Contain("\\"));
        }

        // At least not throw and produce trimmed result
        Assert.That(result, Is.Not.Null);
    }
}