using System.Text;
using XeniaManager.Files;
using XeniaManager.Files.Models.SteamShortcuts;

namespace XeniaManager.Tests.Files;

/// <summary>
/// Unit tests for the binary VDF shortcuts.vdf handling (<see cref="SteamShortcutsFile"/>).
/// All fixtures are built in code - no external asset files are required.
/// The builder mirrors files written by Steam itself: lower case "appname" keys,
/// quoted paths where applicable, and four trailing End markers.
/// </summary>
[TestFixture]
public class SteamShortcutsFileTests
{
    private const uint TestAppId = 0xEA35BF0D;
    private const int TestLastPlayTime = 1773906253;

    #region Helpers

    /// <summary>
    /// Builds raw binary VDF bytes in the exact layout Steam writes for shortcuts.vdf.
    /// The constructor writes the root dictionary header ("shortcuts").
    /// </summary>
    private sealed class BinaryVdfBuilder
    {
        private readonly MemoryStream _stream = new MemoryStream();
        private readonly BinaryWriter _writer;

        public BinaryVdfBuilder()
        {
            _writer = new BinaryWriter(_stream);
            WriteType(0x00);
            WriteNullTerminated("shortcuts");
        }

        public BinaryVdfBuilder BeginEntry(string index)
        {
            WriteType(0x00);
            WriteNullTerminated(index);
            return this;
        }

        public BinaryVdfBuilder String(string key, string value)
        {
            WriteType(0x01);
            WriteNullTerminated(key);
            WriteNullTerminated(value);
            return this;
        }

        public BinaryVdfBuilder Int32(string key, int value)
        {
            WriteType(0x02);
            WriteNullTerminated(key);
            _writer.Write(value);
            return this;
        }

        public BinaryVdfBuilder RawInt32(string key, byte[] rawBytes)
        {
            WriteType(0x02);
            WriteNullTerminated(key);
            _writer.Write(rawBytes);
            return this;
        }

        public BinaryVdfBuilder BeginDictionary(string key)
        {
            WriteType(0x00);
            WriteNullTerminated(key);
            return this;
        }

        public BinaryVdfBuilder RawByte(byte value)
        {
            _writer.Write(value);
            return this;
        }

        public BinaryVdfBuilder End()
        {
            WriteType(0x08);
            return this;
        }

        public byte[] ToBytes()
        {
            _writer.Flush();
            return _stream.ToArray();
        }

        private void WriteType(byte type) => _writer.Write(type);

        private void WriteNullTerminated(string value)
        {
            _writer.Write(Encoding.UTF8.GetBytes(value));
            _writer.Write((byte)0x00);
        }
    }

    /// <summary>
    /// Writes the standard shortcut fields (appid .. sortas) using the layout of real Steam files.
    /// </summary>
    private static void AddSteamFields(BinaryVdfBuilder builder, string title, uint appId)
    {
        builder.RawInt32("appid", BitConverter.GetBytes(appId));
        builder.String("appname", title);
        builder.String("Exe", $"\"D:\\Games\\{title}\\game.exe\"");
        builder.String("StartDir", $"D:\\Games\\{title}\\");
        builder.String("icon", "");
        builder.String("ShortcutPath", "");
        builder.String("LaunchOptions", "");
        builder.Int32("IsHidden", 0);
        builder.Int32("AllowDesktopConfig", 1);
        builder.Int32("AllowOverlay", 1);
        builder.Int32("OpenVR", 0);
        builder.Int32("Devkit", 0);
        builder.String("DevkitGameID", "");
        builder.Int32("DevkitOverrideAppID", 0);
        builder.Int32("LastPlayTime", TestLastPlayTime);
        builder.String("FlatpakAppID", "");
        builder.String("sortas", "");
    }

    /// <summary>
    /// Writes one complete shortcut entry using the field layout of real Steam files.
    /// The customize callback runs after the standard fields and may inject extra fields;
    /// an empty tags dictionary and the entry terminator are always appended afterwards.
    /// </summary>
    private static void AddSteamEntry(BinaryVdfBuilder builder, string index, string title, uint appId, Action<BinaryVdfBuilder>? customize = null)
    {
        builder.BeginEntry(index);
        AddSteamFields(builder, title, appId);
        customize?.Invoke(builder);
        builder.BeginDictionary("tags");
        builder.End();
        builder.End();
    }

    /// <summary>
    /// Closes the root dictionary and appends the extra terminator Steam always writes.
    /// </summary>
    private static byte[] Finish(BinaryVdfBuilder builder) => builder.End().End().ToBytes();

    private static string WriteTempFile(byte[] bytes)
    {
        string path = Path.Combine(Path.GetTempPath(), $"steam_shortcuts_{Guid.NewGuid():N}.vdf");
        File.WriteAllBytes(path, bytes);
        return path;
    }

    private static SteamShortcut CreateXeniaShortcut(string appName, uint appId)
    {
        SteamShortcut shortcut = new SteamShortcut
        {
            AppName = appName,
            Exe = @"C:\Program Files\Xenia Manager\XeniaManager.exe",
            IsCreatedByXenia = true
        };
        shortcut.SetAppIdFromUint(appId);
        return shortcut;
    }

    #endregion

    #region Load Tests

    [Test]
    public void Load_ValidFile_ReturnsShortcutsFile()
    {
        // Arrange
        BinaryVdfBuilder builder = new BinaryVdfBuilder();
        AddSteamEntry(builder, "0", "First Game", TestAppId);
        AddSteamEntry(builder, "1", "Second Game", 0xEECC0A56);
        string path = WriteTempFile(Finish(builder));

        try
        {
            // Act
            SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Load(path);

            // Assert
            Assert.That(shortcutsFile, Is.Not.Null);
            Assert.That(shortcutsFile.Shortcuts, Has.Count.EqualTo(2));
            Assert.That(shortcutsFile.FilePath, Is.EqualTo(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Load_NonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        string path = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.vdf");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => SteamShortcutsFile.Load(path));
    }

    [Test]
    public void Load_EmptyFile_ReturnsEmptyShortcutsFile()
    {
        // Arrange
        string path = WriteTempFile([]);

        try
        {
            // Act
            SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Load(path);

            // Assert
            Assert.That(shortcutsFile.Shortcuts, Is.Empty);
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion

    #region Parsing Tests

    [Test]
    public void FromBytes_SteamStyleEntry_ParsesAllFields()
    {
        // Arrange - single entry using the exact layout of real Steam output
        BinaryVdfBuilder builder = new BinaryVdfBuilder();
        AddSteamEntry(builder, "0", "Assassins Creed Black Flag Resynced", TestAppId);

        // Act
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.FromBytes(Finish(builder));

        // Assert
        Assert.That(shortcutsFile.Shortcuts, Has.Count.EqualTo(1));
        SteamShortcut shortcut = shortcutsFile.Shortcuts[0];
        Assert.That(shortcut.AppName, Is.EqualTo("Assassins Creed Black Flag Resynced"));
        Assert.That(shortcut.Exe, Is.EqualTo("\"D:\\Games\\Assassins Creed Black Flag Resynced\\game.exe\""));
        Assert.That(shortcut.StartDir, Is.EqualTo("D:\\Games\\Assassins Creed Black Flag Resynced\\"));
        Assert.That(shortcut.Icon, Is.EqualTo(""));
        Assert.That(shortcut.LaunchOptions, Is.EqualTo(""));
        Assert.That(shortcut.IsHidden, Is.False);
        Assert.That(shortcut.AllowDesktopConfig, Is.True);
        Assert.That(shortcut.AllowOverlay, Is.True);
        Assert.That(shortcut.OpenVR, Is.False);
        Assert.That(shortcut.Devkit, Is.False);
        Assert.That(shortcut.FlatpakAppID, Is.EqualTo(""));
        Assert.That(shortcut.SortAs, Is.EqualTo(""));
        Assert.That(shortcut.GetAppIdAsUint(), Is.EqualTo(TestAppId));
        Assert.That(shortcut.GetLastPlayTimeAsInt(), Is.EqualTo(TestLastPlayTime));
        Assert.That(shortcut.Tags, Is.Empty);
        Assert.That(shortcut.UnknownFields, Is.Empty);
    }

    [Test]
    public void FromBytes_MultipleEntries_AreParsedInOrder()
    {
        // Arrange
        BinaryVdfBuilder builder = new BinaryVdfBuilder();
        AddSteamEntry(builder, "0", "First Game", 0xEA35BF0D);
        AddSteamEntry(builder, "1", "Bean (GoldenEye 007)", 0xEECC0A56);
        AddSteamEntry(builder, "2", "Perfect Dark", 0xE86E0D52);

        // Act
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.FromBytes(Finish(builder));

        // Assert
        Assert.That(shortcutsFile.Shortcuts.Select(s => s.AppName),
            Is.EqualTo(new[]
            {
                "First Game", "Bean (GoldenEye 007)", "Perfect Dark"
            }));
    }

    [Test]
    public void FromBytes_KnownKeysMatchCaseInsensitively()
    {
        // Arrange - Steam rewrites keys to its own casing ("appname"); any casing must map to the model
        BinaryVdfBuilder builder = new BinaryVdfBuilder();
        builder.BeginEntry("0")
            .RawInt32("APPID", BitConverter.GetBytes(TestAppId))
            .String("AppName", "Cased Title")
            .Int32("ISHIDDEN", 1)
            .String("launchoptions", "-test")
            .BeginDictionary("tags").End()
            .End();

        // Act
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.FromBytes(builder.End().End().ToBytes());

        // Assert
        SteamShortcut shortcut = shortcutsFile.Shortcuts[0];
        Assert.That(shortcut.AppName, Is.EqualTo("Cased Title"));
        Assert.That(shortcut.IsHidden, Is.True);
        Assert.That(shortcut.LaunchOptions, Is.EqualTo("-test"));
        Assert.That(shortcut.UnknownFields, Is.Empty, "Casing variants of known keys must be matched, not preserved as unknown");
    }

    [Test]
    public void FromBytes_UnicodeTitle_IsPreserved()
    {
        // Arrange
        const string title = "Halo: Reach \U0001F3AE";
        BinaryVdfBuilder builder = new BinaryVdfBuilder();
        AddSteamEntry(builder, "0", title, TestAppId);

        // Act
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.FromBytes(Finish(builder));

        // Assert
        Assert.That(shortcutsFile.Shortcuts[0].AppName, Is.EqualTo(title));
    }

    [Test]
    public void FromBytes_Tags_AreParsedIntoList()
    {
        // Arrange - entry composed manually so the tags dictionary contains values
        BinaryVdfBuilder builder = new BinaryVdfBuilder();
        builder.BeginEntry("0");
        AddSteamFields(builder, "Tagged Game", TestAppId);
        builder.BeginDictionary("tags")
            .String("0", "Action")
            .String("1", "Singleplayer")
            .End();
        builder.End();
        AddSteamEntry(builder, "1", "Untagged Game", 0xEECC0A56);

        // Act
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.FromBytes(Finish(builder));

        // Assert
        Assert.That(shortcutsFile.Shortcuts[0].Tags, Is.EqualTo(new List<string>
        {
            "Action",
            "Singleplayer"
        }));
        Assert.That(shortcutsFile.Shortcuts[1].Tags, Is.Empty);
    }

    [Test]
    public void FromBytes_UnknownFields_AreCapturedAsRawBytes()
    {
        // Arrange - fields written by a future Steam version that this model does not know
        byte[] unknownIntPayload = BitConverter.GetBytes(7);
        BinaryVdfBuilder builder = new BinaryVdfBuilder();
        AddSteamEntry(builder, "0", "Future Proofed Game", TestAppId, b => b
            .String("SomeNewField", "reserved")
            .Int32("AnotherNewField", 7));

        // Act
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.FromBytes(Finish(builder));

        // Assert
        SteamShortcut shortcut = shortcutsFile.Shortcuts[0];
        Assert.That(shortcut.UnknownFields, Has.Count.EqualTo(2));

        UnknownVdfField stringField = shortcut.UnknownFields.Single(f => f.Key == "SomeNewField");
        Assert.That(stringField.Type, Is.EqualTo(0x01));
        Assert.That(Encoding.UTF8.GetString(stringField.Value), Is.EqualTo("reserved\0"));

        UnknownVdfField intField = shortcut.UnknownFields.Single(f => f.Key == "AnotherNewField");
        Assert.That(intField.Type, Is.EqualTo(0x02));
        Assert.That(intField.Value, Is.EqualTo(unknownIntPayload));
    }

    [Test]
    public void FromBytes_UnknownDictionary_CapturedIncludingEndMarker()
    {
        // Arrange
        BinaryVdfBuilder builder = new BinaryVdfBuilder();
        AddSteamEntry(builder, "0", "Dict Game", TestAppId, b => b
            .BeginDictionary("metadata")
            .String("extra", "value")
            .End());

        // Act
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.FromBytes(Finish(builder));

        // Assert
        SteamShortcut shortcut = shortcutsFile.Shortcuts[0];
        Assert.That(shortcut.UnknownFields, Has.Count.EqualTo(1));
        UnknownVdfField dictField = shortcut.UnknownFields[0];
        Assert.That(dictField.Key, Is.EqualTo("metadata"));
        Assert.That(dictField.Type, Is.EqualTo(0x00));
        Assert.That(dictField.Value.Last(), Is.EqualTo(0x08), "Dictionary payload must include its terminating End marker");
    }

    #endregion

    #region Corruption Detection Tests

    [Test]
    public void FromBytes_UnknownTypeByte_ThrowsFormatExceptionWithDetails()
    {
        // Arrange - valid structure with an injected unknown type marker (0x05)
        List<byte> bytes =
        [
            0x00, .. Encoding.UTF8.GetBytes("shortcuts"), 0x00,
            0x00, .. Encoding.UTF8.GetBytes("0"), 0x00,
            0x02, .. Encoding.UTF8.GetBytes("appid"), 0x00, .. BitConverter.GetBytes(unchecked((int)TestAppId)),
            0x05, .. Encoding.UTF8.GetBytes("badkey"), 0x00,
            0x08,
            0x08
        ];

        // Act & Assert - must fail loudly instead of silently desyncing and stripping titles
        FormatException? ex = Assert.Throws<FormatException>(() => SteamShortcutsFile.FromBytes([.. bytes]));
        Assert.That(ex!.Message, Does.Contain("0x05"));
    }

    [Test]
    public void FromBytes_TruncatedData_ThrowsFormatException()
    {
        // Arrange
        BinaryVdfBuilder builder = new BinaryVdfBuilder();
        AddSteamEntry(builder, "0", "Truncated Game", TestAppId);
        byte[] truncated = Finish(builder)[..^4];

        // Act & Assert
        Assert.Throws<FormatException>(() => SteamShortcutsFile.FromBytes(truncated));
    }

    [Test]
    public void FromBytes_TrailingGarbage_ThrowsFormatException()
    {
        // Arrange - valid file followed by unparsed data; saving would strip it
        BinaryVdfBuilder builder = new BinaryVdfBuilder();
        AddSteamEntry(builder, "0", "Garbage Game", TestAppId);
        List<byte> corrupted = [.. Finish(builder), 0x01, (byte)'A', 0x00];

        // Act & Assert
        Assert.Throws<FormatException>(() => SteamShortcutsFile.FromBytes([.. corrupted]));
    }

    [Test]
    public void FromBytes_WrongRootKey_ThrowsFormatException()
    {
        // Arrange
        List<byte> bytes = [0x00, .. Encoding.UTF8.GetBytes("notshortcuts"), 0x00];

        // Act & Assert
        Assert.Throws<FormatException>(() => SteamShortcutsFile.FromBytes([.. bytes]));
    }

    [Test]
    public void FromBytes_EmptyInput_ThrowsFormatException()
    {
        // Act & Assert
        Assert.Throws<FormatException>(() => SteamShortcutsFile.FromBytes([]));
    }

    [Test]
    public void FromBytes_LegacyExtraEndMarkers_LoadsSuccessfully()
    {
        // Arrange - older writers appended extra End markers after the root dictionary
        BinaryVdfBuilder builder = new BinaryVdfBuilder();
        AddSteamEntry(builder, "0", "Legacy Game", TestAppId);
        List<byte> legacy = [.. Finish(builder), 0x08, 0x08];

        // Act
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.FromBytes([.. legacy]);

        // Assert
        Assert.That(shortcutsFile.Shortcuts, Has.Count.EqualTo(1));
        Assert.That(shortcutsFile.Shortcuts[0].AppName, Is.EqualTo("Legacy Game"));
    }

    #endregion

    #region Writer Tests

    [Test]
    public void ToBytes_WritesCanonicalTrailingTerminators()
    {
        // Arrange
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        shortcutsFile.AddShortcut(CreateXeniaShortcut("App", TestAppId));

        // Act
        byte[] bytes = shortcutsFile.ToBytes();

        // Assert - matching files written by Steam itself, the tail is:
        // 0x08 (tags dict end) + 0x08 (entry end) + 0x08 (root end) + 0x08 (extra terminator).
        // Steam deletes shortcuts.vdf as corrupted when the extra terminator is missing.
        Assert.That(bytes[^1], Is.EqualTo(0x08));
        Assert.That(bytes[^2], Is.EqualTo(0x08));
        Assert.That(bytes[^3], Is.EqualTo(0x08));
        Assert.That(bytes[^4], Is.EqualTo(0x08));
        Assert.That(bytes[^5], Is.Not.EqualTo(0x08));
    }

    [Test]
    public void ToBytes_DuplicateAppIds_DoesNotThrowAndKeepsAllEntries()
    {
        // Arrange - pre-existing duplicates in foreign entries must never block saving
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        SteamShortcut first = CreateXeniaShortcut("App 1", TestAppId);
        SteamShortcut second = CreateXeniaShortcut("App 2", TestAppId);
        shortcutsFile.AddShortcut(first);
        shortcutsFile.AddShortcut(second);
        string path = WriteTempFile([]);

        try
        {
            // Act
            shortcutsFile.Save(path);
            SteamShortcutsFile loaded = SteamShortcutsFile.Load(path);

            // Assert
            Assert.That(loaded.Shortcuts, Has.Count.EqualTo(2));
            Assert.That(loaded.Shortcuts[0].AppName, Is.EqualTo("App 1"));
            Assert.That(loaded.Shortcuts[1].AppName, Is.EqualTo("App 2"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Save_CreatesMissingDirectory()
    {
        // Arrange
        string directory = Path.Combine(Path.GetTempPath(), $"steam_shortcuts_{Guid.NewGuid():N}", "config");
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        shortcutsFile.AddShortcut(CreateXeniaShortcut("App", TestAppId));
        string path = Path.Combine(directory, "shortcuts.vdf");

        try
        {
            // Act & Assert
            Assert.DoesNotThrow(() => shortcutsFile.Save(path));
            Assert.That(File.Exists(path), Is.True);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void Save_XeniaShortcut_FormatsPaths()
    {
        // Arrange
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        SteamShortcut shortcut = new SteamShortcut
        {
            AppName = "Formatted App",
            Exe = @"C:/Program Files/Xenia Manager/XeniaManager.exe",
            StartDir = @"C:/Program Files/Xenia Manager/",
            Icon = @"C:/icons/icon.ico",
            IsCreatedByXenia = true
        };
        shortcut.SetAppIdFromUint(TestAppId);
        shortcutsFile.AddShortcut(shortcut);
        string path = WriteTempFile([]);

        try
        {
            // Act
            shortcutsFile.Save(path);
            SteamShortcutsFile loaded = SteamShortcutsFile.Load(path);

            // Assert - forward slashes become backslashes on Windows; Exe is force-quoted,
            // paths without spaces are not quoted
            if (OperatingSystem.IsWindows())
            {
                Assert.That(loaded.Shortcuts[0].Exe, Is.EqualTo("\"C:\\Program Files\\Xenia Manager\\XeniaManager.exe\""));
                Assert.That(loaded.Shortcuts[0].StartDir, Is.EqualTo("\"C:\\Program Files\\Xenia Manager\\\""));
                Assert.That(loaded.Shortcuts[0].Icon, Is.EqualTo("C:\\icons\\icon.ico"));
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Save_ForeignShortcut_WritesPathsVerbatim()
    {
        // Arrange - entries not created by Xenia Manager must not be reformatted
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        SteamShortcut foreign = new SteamShortcut
        {
            AppName = "Foreign App",
            Exe = "D:\\Games\\foreign.exe",
            StartDir = "D:\\Games\\",
            Icon = "",
            IsCreatedByXenia = false
        };
        foreign.SetAppIdFromUint(TestAppId);
        shortcutsFile.AddShortcut(foreign);
        string path = WriteTempFile([]);

        try
        {
            // Act
            shortcutsFile.Save(path);
            SteamShortcutsFile loaded = SteamShortcutsFile.Load(path);

            // Assert
            Assert.That(loaded.Shortcuts[0].Exe, Is.EqualTo("D:\\Games\\foreign.exe"), "Foreign Exe must not gain quotes");
            Assert.That(loaded.Shortcuts[0].StartDir, Is.EqualTo("D:\\Games\\"), "Foreign StartDir must stay unquoted");
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion

    #region Round-Trip Tests

    [Test]
    public void RoundTrip_AllPropertiesPreserved()
    {
        // Arrange
        SteamShortcutsFile original = SteamShortcutsFile.Create();
        SteamShortcut shortcut = new SteamShortcut
        {
            AppName = "Full App",
            Exe = "\"C:\\Games\\Full App\\game.exe\"",
            StartDir = "C:\\Games\\Full App\\",
            Icon = "C:\\Games\\Full App\\icon.ico",
            ShortcutPath = "",
            LaunchOptions = "-windowed",
            IsHidden = false,
            AllowDesktopConfig = true,
            AllowOverlay = false,
            OpenVR = true,
            Devkit = false,
            FlatpakAppID = "com.test.app",
            SortAs = "Full App",
            IsCreatedByXenia = true
        };
        shortcut.SetAppIdFromUint(TestAppId);
        shortcut.SetLastPlayTimeFromInt(TestLastPlayTime);
        shortcut.Tags.Add("Action");
        shortcut.Tags.Add("Adventure");
        original.AddShortcut(shortcut);
        string path = WriteTempFile([]);

        try
        {
            // Act
            original.Save(path);
            SteamShortcutsFile loaded = SteamShortcutsFile.Load(path);

            // Assert
            SteamShortcut roundTripped = loaded.Shortcuts[0];
            Assert.That(roundTripped.AppName, Is.EqualTo("Full App"));
            Assert.That(roundTripped.Exe, Is.EqualTo("\"C:\\Games\\Full App\\game.exe\""));
            Assert.That(roundTripped.StartDir, Is.EqualTo("\"C:\\Games\\Full App\\\""), "StartDir with spaces gets quoted");
            Assert.That(roundTripped.Icon, Is.EqualTo("\"C:\\Games\\Full App\\icon.ico\""), "Icon with spaces gets quoted");
            Assert.That(roundTripped.LaunchOptions, Is.EqualTo("-windowed"));
            Assert.That(roundTripped.AllowDesktopConfig, Is.True);
            Assert.That(roundTripped.AllowOverlay, Is.False);
            Assert.That(roundTripped.OpenVR, Is.True);
            Assert.That(roundTripped.FlatpakAppID, Is.EqualTo("com.test.app"));
            Assert.That(roundTripped.SortAs, Is.EqualTo("Full App"));
            Assert.That(roundTripped.GetAppIdAsUint(), Is.EqualTo(TestAppId));
            Assert.That(roundTripped.GetLastPlayTimeAsInt(), Is.EqualTo(TestLastPlayTime));
            Assert.That(roundTripped.Tags, Is.EqualTo(new List<string>
            {
                "Action",
                "Adventure"
            }));
            Assert.That(roundTripped.UnknownFields, Is.Empty);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void RoundTrip_UnknownFields_ReplayedVerbatim()
    {
        // Arrange
        BinaryVdfBuilder builder = new BinaryVdfBuilder();
        AddSteamEntry(builder, "0", "Replayed Game", TestAppId, b => b
            .String("FutureString", "keep me")
            .Int32("FutureInt", 42));
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.FromBytes(Finish(builder));
        string path = WriteTempFile([]);

        try
        {
            // Act - save the parsed file back out without modifications
            shortcutsFile.Save(path);
            SteamShortcutsFile reloaded = SteamShortcutsFile.Load(path);

            // Assert - unknown fields survive the round-trip intact
            SteamShortcut roundTripped = reloaded.Shortcuts[0];
            Assert.That(roundTripped.UnknownFields, Has.Count.EqualTo(2));
            UnknownVdfField stringField = roundTripped.UnknownFields.Single(f => f.Key == "FutureString");
            Assert.That(Encoding.UTF8.GetString(stringField.Value), Is.EqualTo("keep me\0"));
            UnknownVdfField intField = roundTripped.UnknownFields.Single(f => f.Key == "FutureInt");
            Assert.That(BitConverter.ToInt32(intField.Value, 0), Is.EqualTo(42));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void RoundTrip_RepeatedSaves_StableOutput()
    {
        // Arrange
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        shortcutsFile.AddShortcut(CreateXeniaShortcut("Stable App", TestAppId));
        string firstPath = WriteTempFile([]);
        string secondPath = WriteTempFile([]);

        try
        {
            // Act - save twice through separate load/save cycles
            shortcutsFile.Save(firstPath);
            SteamShortcutsFile firstReload = SteamShortcutsFile.Load(firstPath);
            firstReload.Save(secondPath);
            SteamShortcutsFile secondReload = SteamShortcutsFile.Load(secondPath);

            // Assert
            Assert.That(File.ReadAllBytes(firstPath), Is.EqualTo(File.ReadAllBytes(secondPath)),
                "Repeated load/save cycles must produce identical bytes");
            Assert.That(secondReload.Shortcuts[0].AppName, Is.EqualTo("Stable App"));
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    #endregion

    #region Regression Tests (Issue #577)

    [Test]
    public void LoadAddSave_PreservesExistingTitlesAndUnknownData()
    {
        // Arrange - the exact scenario from issue #577: a shortcuts.vdf written by Steam
        // (lower case "appname" keys) already contains games; Xenia Manager adds one more
        BinaryVdfBuilder builder = new BinaryVdfBuilder();
        AddSteamEntry(builder, "0", "Assassins Creed Black Flag Resynced", 0xEA35BF0D, b => b
            .String("SomeNewField", "reserved")
            .Int32("AnotherNewField", 7));
        AddSteamEntry(builder, "1", "Bean (GoldenEye 007)", 0xEECC0A56);
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.FromBytes(Finish(builder));
        string path = WriteTempFile([]);

        try
        {
            // Act
            shortcutsFile.AddShortcut(CreateXeniaShortcut("Perfect Dark", 0xE86E0D52));
            shortcutsFile.Save(path);
            SteamShortcutsFile reloaded = SteamShortcutsFile.Load(path);

            // Assert - nothing may be stripped or duplicated
            Assert.That(reloaded.Shortcuts, Has.Count.EqualTo(3));
            Assert.That(reloaded.Shortcuts.Select(s => s.AppName), Does.Contain("Assassins Creed Black Flag Resynced"));
            Assert.That(reloaded.Shortcuts.Select(s => s.AppName), Does.Contain("Bean (GoldenEye 007)"));
            Assert.That(reloaded.Shortcuts.Select(s => s.AppName), Does.Contain("Perfect Dark"));

            SteamShortcut original = reloaded.Shortcuts[0];
            Assert.That(original.Exe, Is.EqualTo("\"D:\\Games\\Assassins Creed Black Flag Resynced\\game.exe\""),
                "Original paths must survive untouched");
            Assert.That(original.UnknownFields.Select(f => f.Key),
                Is.EquivalentTo(new[]
                {
                    "SomeNewField", "AnotherNewField"
                }),
                "Genuinely unknown fields must be preserved through the round-trip");
            Assert.That(reloaded.Shortcuts[1].UnknownFields, Is.Empty);
            Assert.That(reloaded.Shortcuts[1].GetAppIdAsUint(), Is.EqualTo(0xEECC0A56));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void LoadAddSave_DoesNotDuplicateEntriesForExistingTitles()
    {
        // Arrange
        BinaryVdfBuilder builder = new BinaryVdfBuilder();
        AddSteamEntry(builder, "0", "Existing Game", TestAppId);
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.FromBytes(Finish(builder));

        // Act - adding a game whose title matches an existing entry is a no-op at manager level
        bool existsBefore = shortcutsFile.Shortcuts.Any(s =>
            string.Equals(s.AppName, "Existing Game", StringComparison.OrdinalIgnoreCase));

        // Assert
        Assert.That(existsBefore, Is.True, "Title lookup used for deduplication must find parsed entries");
        Assert.That(shortcutsFile.Shortcuts, Has.Count.EqualTo(1));
    }

    #endregion

    #region Model Tests

    [Test]
    public void ComputeAppId_SetsHighBit_AndIsDeterministic()
    {
        // Arrange
        SteamShortcut shortcut = new SteamShortcut
        {
            AppName = "Game",
            Exe = "game.exe"
        };

        // Act
        uint appId = shortcut.ComputeAppId();
        uint repeated = shortcut.ComputeAppId();

        // Assert
        Assert.That(appId, Is.EqualTo(repeated));
        Assert.That(appId & 0x80000000, Is.Not.Zero, "Non-Steam game AppIds must have the high bit set");
    }

    [Test]
    public void ComputeAppId_SaltChangesResult()
    {
        // Arrange - salt is used to resolve AppId collisions
        SteamShortcut shortcut = new SteamShortcut
        {
            AppName = "Game",
            Exe = "game.exe"
        };

        // Act
        uint unsalted = shortcut.ComputeAppId();
        uint salted = shortcut.ComputeAppId("#0");

        // Assert
        Assert.That(salted, Is.Not.EqualTo(unsalted));
    }

    [Test]
    public void GetSetAppId_RoundTrips()
    {
        // Arrange
        SteamShortcut shortcut = new SteamShortcut();

        // Act
        shortcut.SetAppIdFromUint(TestAppId);

        // Assert
        Assert.That(shortcut.GetAppIdAsUint(), Is.EqualTo(TestAppId));
        Assert.That(new SteamShortcut().GetAppIdAsUint(), Is.EqualTo(0), "Unset AppId reads as zero");
    }

    [Test]
    public void LastPlayTime_HelpersRoundTrip()
    {
        // Arrange
        SteamShortcut shortcut = new SteamShortcut();

        // Act
        shortcut.SetLastPlayTimeFromInt(TestLastPlayTime);

        // Assert
        Assert.That(shortcut.GetLastPlayTimeAsInt(), Is.EqualTo(TestLastPlayTime));
        Assert.That(new SteamShortcut().GetLastPlayTimeAsInt(), Is.EqualTo(0));
    }

    [Test]
    public void DevkitOverrideAppId_HelpersRoundTrip()
    {
        // Arrange
        SteamShortcut shortcut = new SteamShortcut();

        // Act
        shortcut.SetDevkitOverrideAppIdFromUint(0x12345678);

        // Assert
        Assert.That(shortcut.GetDevkitOverrideAppIdAsUint(), Is.EqualTo(0x12345678));
        Assert.That(new SteamShortcut().GetDevkitOverrideAppIdAsUint(), Is.EqualTo(0));
    }

    [Test]
    public void Shortcut_Defaults_AreEmpty()
    {
        // Arrange & Act
        SteamShortcut shortcut = new SteamShortcut();

        // Assert
        Assert.That(shortcut.AppName, Is.Null);
        Assert.That(shortcut.Exe, Is.Null);
        Assert.That(shortcut.StartDir, Is.Null);
        Assert.That(shortcut.Icon, Is.Null);
        Assert.That(shortcut.ShortcutPath, Is.Null);
        Assert.That(shortcut.LaunchOptions, Is.Null);
        Assert.That(shortcut.DevkitGameID, Is.Null);
        Assert.That(shortcut.FlatpakAppID, Is.Null);
        Assert.That(shortcut.SortAs, Is.Null);
        Assert.That(shortcut.IsHidden, Is.False);
        Assert.That(shortcut.AllowDesktopConfig, Is.False);
        Assert.That(shortcut.AllowOverlay, Is.False);
        Assert.That(shortcut.OpenVR, Is.False);
        Assert.That(shortcut.Devkit, Is.False);
        Assert.That(shortcut.IsCreatedByXenia, Is.False);
        Assert.That(shortcut.Tags, Is.Empty);
        Assert.That(shortcut.UnknownFields, Is.Empty);
    }

    [Test]
    public void Shortcut_Manipulation_MethodsWork()
    {
        // Arrange
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        SteamShortcut kept = CreateXeniaShortcut("Kept", TestAppId);
        SteamShortcut removed = CreateXeniaShortcut("Removed", 0xEECC0A56);
        shortcutsFile.AddShortcut(kept);
        shortcutsFile.AddShortcut(removed);

        // Act & Assert
        Assert.That(shortcutsFile.GetShortcutByAppId(TestAppId), Is.EqualTo(kept));
        Assert.That(shortcutsFile.RemoveShortcutByAppId(0xEECC0A56), Is.True);
        Assert.That(shortcutsFile.Shortcuts, Has.Count.EqualTo(1));
        Assert.That(shortcutsFile.RemoveShortcutByAppId(0xDEADBEEF), Is.False);
        Assert.That(shortcutsFile.GetShortcutByAppId(0xDEADBEEF), Is.Null);
    }

    #endregion
}