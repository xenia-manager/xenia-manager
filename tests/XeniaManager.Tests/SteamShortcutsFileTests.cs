using System.Reflection;
using System.Text;
using XeniaManager.Core.Files;
using XeniaManager.Core.Models.Files.SteamShortcuts;

namespace XeniaManager.Tests;

[TestFixture]
public class SteamShortcutsFileTests
{
    private string _assetsFolder = string.Empty;
    private string _testShortcutsFilePath = string.Empty;

    [SetUp]
    public void Setup()
    {
        // Get the path to the Assets directory
        string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        _assetsFolder = Path.Combine(assemblyLocation, "Assets");
        _testShortcutsFilePath = Path.Combine(_assetsFolder, "TestShortcuts.vdf");

        // Verify the test file exists
        Assert.That(File.Exists(_testShortcutsFilePath), Is.True, $"Test shortcuts file does not exist at {_testShortcutsFilePath}");
    }

    #region Load Tests

    [Test]
    public void Load_ValidShortcutsFile_ReturnsSteamShortcutsFile()
    {
        // Act
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Load(_testShortcutsFilePath);

        // Assert
        Assert.That(shortcutsFile, Is.Not.Null);
        Assert.That(shortcutsFile.Shortcuts, Is.Not.Null);
    }

    [Test]
    public void Load_ValidShortcutsFile_ParsesShortcuts()
    {
        // Act
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Load(_testShortcutsFilePath);

        // Assert
        Assert.That(shortcutsFile.Shortcuts, Has.Count.EqualTo(2));
    }

    [Test]
    public void Load_ValidShortcutsFile_ParsesShortcutProperties()
    {
        // Act
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Load(_testShortcutsFilePath);

        // Assert
        Assert.That(shortcutsFile.Shortcuts, Has.Count.EqualTo(2));

        SteamShortcut xenia = shortcutsFile.Shortcuts[0];
        Assert.That(xenia.AppName, Is.EqualTo("Xenia Manager"));
        Assert.That(xenia.Exe, Is.EqualTo("\"E:\\XeniaManager\\XeniaManager.exe\""));
        Assert.That(xenia.StartDir, Is.EqualTo("E:\\XeniaManager\\"));
        Assert.That(xenia.LaunchOptions, Is.EqualTo("-skiplauncher"));
        Assert.That(xenia.AllowDesktopConfig, Is.True);
        Assert.That(xenia.AllowOverlay, Is.True);
        Assert.That(xenia.OpenVR, Is.True);
        Assert.That(xenia.GetAppIdAsUint(), Is.EqualTo(0x8A12BC34));
        Assert.That(xenia.GetLastPlayTimeAsInt(), Is.EqualTo(1773906253));
        Assert.That(xenia.Tags, Is.EqualTo(new List<string> { "Finished" }));

        SteamShortcut foreign = shortcutsFile.Shortcuts[1];
        Assert.That(foreign.AppName, Is.EqualTo("Halo: Reach \U0001F3AE"));
        Assert.That(foreign.Exe, Is.EqualTo(@"D:\Games\halo.exe"));
        Assert.That(foreign.SortAs, Is.EqualTo("halo reach"));
        Assert.That(foreign.GetAppIdAsUint(), Is.EqualTo(0x8F0E5D21));
    }

    [Test]
    public void Load_ValidShortcutsFile_PreservesUnknownFields()
    {
        // Act
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Load(_testShortcutsFilePath);

        // Assert - unrecognized fields must be captured, not dropped
        SteamShortcut xenia = shortcutsFile.Shortcuts[0];
        Assert.That(xenia.UnknownFields, Has.Count.EqualTo(1));
        UnknownVdfField compatField = xenia.UnknownFields[0];
        Assert.That(compatField.Type, Is.EqualTo(0x01));
        Assert.That(compatField.Key, Is.EqualTo("XeniaCompat"));
        Assert.That(System.Text.Encoding.UTF8.GetString(compatField.Value), Is.EqualTo("v2\0"));

        SteamShortcut foreign = shortcutsFile.Shortcuts[1];
        Assert.That(foreign.UnknownFields, Has.Count.EqualTo(2));

        UnknownVdfField intField = foreign.UnknownFields.First(f => f.Key == "SteamNewFlag");
        Assert.That(intField.Type, Is.EqualTo(0x02));
        Assert.That(BitConverter.ToInt32(intField.Value, 0), Is.EqualTo(7));

        UnknownVdfField dictField = foreign.UnknownFields.First(f => f.Key == "metadata");
        Assert.That(dictField.Type, Is.EqualTo(0x00));
        // Dictionary payload includes its terminating End marker
        Assert.That(dictField.Value.Last(), Is.EqualTo(0x08));
    }

    [Test]
    public void Load_NonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        string nonexistentPath = Path.Combine(_assetsFolder, "nonexistent.vdf");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => SteamShortcutsFile.Load(nonexistentPath));
    }

    [Test]
    public void Load_EmptyFile_ReturnsEmptyShortcutsFile()
    {
        // Arrange
        string emptyPath = Path.Combine(Path.GetTempPath(), $"empty_{Guid.NewGuid()}.vdf");
        File.WriteAllBytes(emptyPath, []);

        try
        {
            // Act
            SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Load(emptyPath);

            // Assert
            Assert.That(shortcutsFile, Is.Not.Null);
            Assert.That(shortcutsFile.Shortcuts, Is.Empty);
        }
        finally
        {
            // Cleanup
            if (File.Exists(emptyPath))
            {
                File.Delete(emptyPath);
            }
        }
    }

    #endregion

    #region FromBytes Tests

    [Test]
    public void FromBytes_EmptyBytes_ThrowsFormatException()
    {
        // Arrange
        byte[] emptyBytes = [];

        // Act & Assert
        Assert.Throws<FormatException>(() => SteamShortcutsFile.FromBytes(emptyBytes));
    }

    [Test]
    public void FromBytes_InvalidData_ThrowsFormatException()
    {
        // Arrange
        byte[] invalidBytes = [0x00, 0x01, 0x02, 0x03];

        // Act & Assert
        Assert.Throws<FormatException>(() => SteamShortcutsFile.FromBytes(invalidBytes));
    }

    #endregion

    #region Create Tests

    [Test]
    public void Create_CreatesEmptyShortcutsFile()
    {
        // Act
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();

        // Assert
        Assert.That(shortcutsFile, Is.Not.Null);
        Assert.That(shortcutsFile.Shortcuts, Is.Empty);
    }

    #endregion

    #region AddShortcut Tests

    [Test]
    public void AddShortcut_AddsToCollection()
    {
        // Arrange
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        SteamShortcut shortcut = new SteamShortcut
        {
            AppName = "Test App",
            Exe = @"C:\Test\app.exe",
            StartDir = @"C:\Test\"
        };

        // Act
        shortcutsFile.AddShortcut(shortcut);

        // Assert
        Assert.That(shortcutsFile.Shortcuts, Has.Count.EqualTo(1));
        Assert.That(shortcutsFile.Shortcuts[0], Is.EqualTo(shortcut));
    }

    [Test]
    public void AddShortcut_MultipleShortcuts_AddsAll()
    {
        // Arrange
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        SteamShortcut shortcut1 = new SteamShortcut { AppName = "App 1" };
        SteamShortcut shortcut2 = new SteamShortcut { AppName = "App 2" };
        SteamShortcut shortcut3 = new SteamShortcut { AppName = "App 3" };

        // Act
        shortcutsFile.AddShortcut(shortcut1);
        shortcutsFile.AddShortcut(shortcut2);
        shortcutsFile.AddShortcut(shortcut3);

        // Assert
        Assert.That(shortcutsFile.Shortcuts, Has.Count.EqualTo(3));
    }

    #endregion

    #region RemoveShortcut Tests

    [Test]
    public void RemoveShortcutAt_ValidIndex_RemovesShortcut()
    {
        // Arrange
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        shortcutsFile.AddShortcut(new SteamShortcut { AppName = "App 1" });
        shortcutsFile.AddShortcut(new SteamShortcut { AppName = "App 2" });

        // Act
        bool result = shortcutsFile.RemoveShortcutAt(0);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(shortcutsFile.Shortcuts, Has.Count.EqualTo(1));
        Assert.That(shortcutsFile.Shortcuts[0].AppName, Is.EqualTo("App 2"));
    }

    [Test]
    public void RemoveShortcutAt_InvalidIndex_ReturnsFalse()
    {
        // Arrange
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        shortcutsFile.AddShortcut(new SteamShortcut { AppName = "App 1" });

        // Act
        bool result = shortcutsFile.RemoveShortcutAt(5);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void RemoveShortcutByAppId_ExistingAppId_RemovesShortcut()
    {
        // Arrange
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        SteamShortcut shortcut = new SteamShortcut { AppName = "Test App" };
        shortcut.SetAppIdFromUint(0x80001234);
        shortcutsFile.AddShortcut(shortcut);

        // Act
        bool result = shortcutsFile.RemoveShortcutByAppId(0x80001234);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(shortcutsFile.Shortcuts, Is.Empty);
    }

    [Test]
    public void RemoveShortcutByAppId_NonExistentAppId_ReturnsFalse()
    {
        // Arrange
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        shortcutsFile.AddShortcut(new SteamShortcut { AppName = "Test App" });

        // Act
        bool result = shortcutsFile.RemoveShortcutByAppId(0x99999999);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region GetShortcutByAppId Tests

    [Test]
    public void GetShortcutByAppId_ExistingAppId_ReturnsShortcut()
    {
        // Arrange
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        SteamShortcut shortcut = new SteamShortcut { AppName = "Test App" };
        shortcut.SetAppIdFromUint(0x80001234);
        shortcutsFile.AddShortcut(shortcut);

        // Act
        SteamShortcut? found = shortcutsFile.GetShortcutByAppId(0x80001234);

        // Assert
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.AppName, Is.EqualTo("Test App"));
    }

    [Test]
    public void GetShortcutByAppId_NonExistentAppId_ReturnsNull()
    {
        // Arrange
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        shortcutsFile.AddShortcut(new SteamShortcut { AppName = "Test App" });

        // Act
        SteamShortcut? found = shortcutsFile.GetShortcutByAppId(0x99999999);

        // Assert
        Assert.That(found, Is.Null);
    }

    #endregion

    #region SteamShortcut Tests

    [Test]
    public void SteamShortcut_GetAppIdAsUint_ReturnsCorrectValue()
    {
        // Arrange
        SteamShortcut shortcut = new SteamShortcut();
        shortcut.SetAppIdFromUint(0x8000ABCD);

        // Act
        uint appId = shortcut.GetAppIdAsUint();

        // Assert
        Assert.That(appId, Is.EqualTo(0x8000ABCD));
    }

    [Test]
    public void SteamShortcut_GetAppIdAsUint_NullAppId_ReturnsZero()
    {
        // Arrange
        SteamShortcut shortcut = new SteamShortcut();

        // Act
        uint appId = shortcut.GetAppIdAsUint();

        // Assert
        Assert.That(appId, Is.EqualTo(0));
    }

    [Test]
    public void SteamShortcut_GetLastPlayTimeAsInt_ReturnsCorrectValue()
    {
        // Arrange
        SteamShortcut shortcut = new SteamShortcut();
        shortcut.SetLastPlayTimeFromInt(1773906253);

        // Act
        int timestamp = shortcut.GetLastPlayTimeAsInt();

        // Assert
        Assert.That(timestamp, Is.EqualTo(1773906253));
    }

    [Test]
    public void SteamShortcut_GetDevkitOverrideAppIdAsUint_ReturnsCorrectValue()
    {
        // Arrange
        SteamShortcut shortcut = new SteamShortcut();
        shortcut.SetDevkitOverrideAppIdFromUint(0x12345678);

        // Act
        uint appId = shortcut.GetDevkitOverrideAppIdAsUint();

        // Assert
        Assert.That(appId, Is.EqualTo(0x12345678));
    }

    [Test]
    public void SteamShortcut_ComputeAppId_ReturnsValidAppId()
    {
        // Arrange
        SteamShortcut shortcut = new SteamShortcut
        {
            AppName = "Test App",
            Exe = @"C:\Test\app.exe"
        };

        // Act
        uint appId = shortcut.ComputeAppId();

        // Assert
        Assert.That((appId & 0x80000000) != 0, Is.True); // High bit should be set for non-Steam games
    }

    [Test]
    public void SteamShortcut_ComputeAppId_SameInput_ReturnsSameAppId()
    {
        // Arrange
        SteamShortcut shortcut1 = new SteamShortcut
        {
            AppName = "Test App",
            Exe = @"C:\Test\app.exe"
        };
        SteamShortcut shortcut2 = new SteamShortcut
        {
            AppName = "Test App",
            Exe = @"C:\Test\app.exe"
        };

        // Act
        uint appId1 = shortcut1.ComputeAppId();
        uint appId2 = shortcut2.ComputeAppId();

        // Assert
        Assert.That(appId1, Is.EqualTo(appId2));
    }

    [Test]
    public void SteamShortcut_ComputeAppId_DifferentInput_ReturnsDifferentAppId()
    {
        // Arrange
        SteamShortcut shortcut1 = new SteamShortcut
        {
            AppName = "Test App 1",
            Exe = @"C:\Test\app1.exe"
        };
        SteamShortcut shortcut2 = new SteamShortcut
        {
            AppName = "Test App 2",
            Exe = @"C:\Test\app2.exe"
        };

        // Act
        uint appId1 = shortcut1.ComputeAppId();
        uint appId2 = shortcut2.ComputeAppId();

        // Assert
        Assert.That(appId1, Is.Not.EqualTo(appId2));
    }

    [Test]
    public void SteamShortcut_Tags_InitializedAsEmptyList()
    {
        // Arrange
        SteamShortcut shortcut = new SteamShortcut();

        // Assert
        Assert.That(shortcut.Tags, Is.Not.Null);
        Assert.That(shortcut.Tags, Is.Empty);
    }

    [Test]
    public void SteamShortcut_ToString_ReturnsFormattedString()
    {
        // Arrange
        SteamShortcut shortcut = new SteamShortcut
        {
            AppName = "Test App"
        };
        shortcut.SetAppIdFromUint(0x80001234);

        // Act
        string result = shortcut.ToString();

        // Assert
        Assert.That(result, Does.Contain("Test App"));
        Assert.That(result, Does.Contain("2147488308")); // 0x80001234 in decimal
    }

    [Test]
    public void SteamShortcut_ToString_NullAppName_ReturnsUnknown()
    {
        // Arrange
        SteamShortcut shortcut = new SteamShortcut();

        // Act
        string result = shortcut.ToString();

        // Assert
        Assert.That(result, Does.Contain("Unknown"));
    }

    #endregion

    #region Save Tests

    [Test]
    public void Save_CreatedShortcutsFile_WritesValidBinaryVdf()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.vdf");
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        SteamShortcut shortcut = new SteamShortcut
        {
            AppName = "Test App",
            Exe = @"C:\Test\app.exe",
            StartDir = @"C:\Test\",
            IsHidden = false,
            AllowDesktopConfig = true,
            AllowOverlay = true
        };
        shortcut.SetAppIdFromUint(0x80001234);
        shortcutsFile.AddShortcut(shortcut);

        try
        {
            // Act
            shortcutsFile.Save(tempPath);

            // Assert
            Assert.That(File.Exists(tempPath), Is.True);

            // Load and verify content
            SteamShortcutsFile loaded = SteamShortcutsFile.Load(tempPath);
            Assert.That(loaded.Shortcuts, Has.Count.EqualTo(1));
            Assert.That(loaded.Shortcuts[0].AppName, Is.EqualTo("Test App"));
            Assert.That(loaded.Shortcuts[0].GetAppIdAsUint(), Is.EqualTo(0x80001234));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void Save_WithTags_WritesTagsCorrectly()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.vdf");
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        SteamShortcut shortcut = new SteamShortcut
        {
            AppName = "Test App",
            Exe = @"C:\Test\app.exe"
        };
        shortcut.SetAppIdFromUint(0x80001234);
        shortcut.Tags.Add("Action");
        shortcut.Tags.Add("Singleplayer");
        shortcutsFile.AddShortcut(shortcut);

        try
        {
            // Act
            shortcutsFile.Save(tempPath);

            // Assert
            SteamShortcutsFile loaded = SteamShortcutsFile.Load(tempPath);
            Assert.That(loaded.Shortcuts[0].Tags, Has.Count.EqualTo(2));
            Assert.That(loaded.Shortcuts[0].Tags, Does.Contain("Action"));
            Assert.That(loaded.Shortcuts[0].Tags, Does.Contain("Singleplayer"));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void Save_DuplicateAppIds_KeepsAllEntries()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.vdf");
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        SteamShortcut shortcut1 = new SteamShortcut { AppName = "App 1" };
        shortcut1.SetAppIdFromUint(0x80001234);
        SteamShortcut shortcut2 = new SteamShortcut { AppName = "App 2" };
        shortcut2.SetAppIdFromUint(0x80001234); // Same AppId
        shortcutsFile.AddShortcut(shortcut1);
        shortcutsFile.AddShortcut(shortcut2);

        try
        {
            // Act - duplicates are warned about but never block saving (no data loss)
            shortcutsFile.Save(tempPath);

            // Assert
            SteamShortcutsFile loaded = SteamShortcutsFile.Load(tempPath);
            Assert.That(loaded.Shortcuts, Has.Count.EqualTo(2));
            Assert.That(loaded.Shortcuts[0].AppName, Is.EqualTo("App 1"));
            Assert.That(loaded.Shortcuts[1].AppName, Is.EqualTo("App 2"));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    #endregion

    #region ToBytes Tests

    [Test]
    public void ToBytes_EmptyShortcuts_GeneratesValidBinaryVdf()
    {
        // Arrange
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();

        // Act
        byte[] bytes = shortcutsFile.ToBytes();

        // Assert
        Assert.That(bytes, Is.Not.Null);
        Assert.That(bytes.Length, Is.GreaterThan(0));
    }

    [Test]
    public void ToBytes_WithShortcuts_GeneratesValidBinaryVdf()
    {
        // Arrange
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        SteamShortcut shortcut = new SteamShortcut
        {
            AppName = "Test App",
            Exe = @"C:\Test\app.exe"
        };
        shortcut.SetAppIdFromUint(0x80001234);
        shortcutsFile.AddShortcut(shortcut);

        // Act
        byte[] bytes = shortcutsFile.ToBytes();

        // Assert
        Assert.That(bytes, Is.Not.Null);
        Assert.That(bytes.Length, Is.GreaterThan(10)); // Should have some content
    }

    [Test]
    public void ToBytes_WritesCanonicalTerminators()
    {
        // Arrange - tagless shortcut so no tag entries are written
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        SteamShortcut shortcut = new SteamShortcut { AppName = "App", Exe = "app.exe" };
        shortcut.SetAppIdFromUint(0x80001234);
        shortcutsFile.AddShortcut(shortcut);

        // Act
        byte[] bytes = shortcutsFile.ToBytes();

        // Assert - the writer always emits the "tags" container, so the tail is:
        // 0x08 (tags dict end) + 0x08 (entry end) + 0x08 (root end)
        // No fourth End marker may follow (older versions wrote an extra one).
        Assert.That(bytes[^1], Is.EqualTo(0x08));
        Assert.That(bytes[^2], Is.EqualTo(0x08));
        Assert.That(bytes[^3], Is.EqualTo(0x08));
        Assert.That(bytes[^4], Is.Not.EqualTo(0x08));
    }

    #endregion

    #region Round-Trip Tests

    [Test]
    public void SaveAndLoad_RoundTrip_PreservesAllData()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.vdf");
        SteamShortcutsFile original = SteamShortcutsFile.Create();

        SteamShortcut shortcut = new SteamShortcut
        {
            AppName = "Test App",
            Exe = @"C:\Xenia Manager\app.exe",
            StartDir = @"C:\Xenia Manager\",
            Icon = @"C:\Xenia Manager\icon.ico",
            LaunchOptions = "-windowed",
            IsHidden = false,
            AllowDesktopConfig = true,
            AllowOverlay = false,
            OpenVR = true,
            Devkit = false,
            FlatpakAppID = "com.test.app",
            SortAs = "Test",
            IsCreatedByXenia = true
        };
        shortcut.SetAppIdFromUint(0x80001234);
        shortcut.SetLastPlayTimeFromInt(1773906253);
        shortcut.Tags.Add("Action");
        shortcut.Tags.Add("Adventure");

        original.AddShortcut(shortcut);

        try
        {
            // Act - Save and reload
            original.Save(tempPath);
            SteamShortcutsFile loaded = SteamShortcutsFile.Load(tempPath);

            // Assert
            Assert.That(loaded.Shortcuts, Has.Count.EqualTo(1));
            SteamShortcut loadedShortcut = loaded.Shortcuts[0];

            Assert.That(loadedShortcut.AppName, Is.EqualTo("Test App"));
            Assert.That(loadedShortcut.Exe, Is.EqualTo("\"C:\\Xenia Manager\\app.exe\""));
            Assert.That(loadedShortcut.StartDir, Is.EqualTo("\"C:\\Xenia Manager\\\""));
            Assert.That(loadedShortcut.Icon, Is.EqualTo("\"C:\\Xenia Manager\\icon.ico\""));
            Assert.That(loadedShortcut.LaunchOptions, Is.EqualTo("-windowed"));
            Assert.That(loadedShortcut.IsHidden, Is.False);
            Assert.That(loadedShortcut.AllowDesktopConfig, Is.True);
            Assert.That(loadedShortcut.AllowOverlay, Is.False);
            Assert.That(loadedShortcut.OpenVR, Is.True);
            Assert.That(loadedShortcut.Devkit, Is.False);
            Assert.That(loadedShortcut.FlatpakAppID, Is.EqualTo("com.test.app"));
            Assert.That(loadedShortcut.SortAs, Is.EqualTo("Test"));
            Assert.That(loadedShortcut.GetAppIdAsUint(), Is.EqualTo(0x80001234));
            Assert.That(loadedShortcut.GetLastPlayTimeAsInt(), Is.EqualTo(1773906253));
            Assert.That(loadedShortcut.Tags, Has.Count.EqualTo(2));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    #endregion

    #region Integration Tests

    [Test]
    public void LoadModifyAndSave_IntegrationTest()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.vdf");

        // Create the initial file
        SteamShortcutsFile original = SteamShortcutsFile.Create();
        SteamShortcut shortcut = new SteamShortcut
        {
            AppName = "Original App",
            Exe = @"C:\Original\app.exe"
        };
        shortcut.SetAppIdFromUint(0x80001234);
        original.AddShortcut(shortcut);
        original.Save(tempPath);

        try
        {
            // Act - Load the file
            SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Load(tempPath);

            // Verify initial value
            Assert.That(shortcutsFile.Shortcuts[0].AppName, Is.EqualTo("Original App"));

            // Modify the shortcut
            shortcutsFile.Shortcuts[0].AppName = "Modified App";
            shortcutsFile.Shortcuts[0].LaunchOptions = "-fullscreen";

            // Add a new shortcut
            SteamShortcut newShortcut = new SteamShortcut
            {
                AppName = "New App",
                Exe = @"C:\New\app.exe"
            };
            newShortcut.SetAppIdFromUint(0x80005678);
            shortcutsFile.AddShortcut(newShortcut);

            // Save the changes
            shortcutsFile.Save();

            // Reload and verify changes
            SteamShortcutsFile reloaded = SteamShortcutsFile.Load(tempPath);
            Assert.That(reloaded.Shortcuts, Has.Count.EqualTo(2));
            Assert.That(reloaded.Shortcuts[0].AppName, Is.EqualTo("Modified App"));
            Assert.That(reloaded.Shortcuts[0].LaunchOptions, Is.EqualTo("-fullscreen"));
            Assert.That(reloaded.Shortcuts[1].AppName, Is.EqualTo("New App"));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    #endregion

    #region Regression Tests (Issue #577)

    [Test]
    public void LoadAddSave_PreservesExistingTitlesAndForeignData()
    {
        // Arrange - simulate the exact scenario from issue #577:
        // an existing shortcuts.vdf contains foreign entries, Xenia Manager adds a game
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.vdf");

        try
        {
            SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Load(_testShortcutsFilePath);

            // Act - add a new game like ShortcutManager does
            SteamShortcut newShortcut = new SteamShortcut
            {
                AppName = "New Game",
                Exe = @"C:\Program Files\Xenia Manager\XeniaManager.exe",
                IsCreatedByXenia = true
            };
            newShortcut.SetAppIdFromUint(0x8D11AA22);
            shortcutsFile.AddShortcut(newShortcut);
            shortcutsFile.Save(tempPath);

            // Assert - reload and verify nothing was stripped or mutated
            SteamShortcutsFile reloaded = SteamShortcutsFile.Load(tempPath);
            Assert.That(reloaded.Shortcuts, Has.Count.EqualTo(3));

            SteamShortcut xenia = reloaded.Shortcuts[0];
            Assert.That(xenia.AppName, Is.EqualTo("Xenia Manager"), "Original title must survive");
            Assert.That(xenia.Exe, Is.EqualTo("\"E:\\XeniaManager\\XeniaManager.exe\""));
            Assert.That(xenia.GetLastPlayTimeAsInt(), Is.EqualTo(1773906253));
            Assert.That(xenia.Tags, Is.EqualTo(new List<string> { "Finished" }));
            Assert.That(xenia.UnknownFields.Any(f => f.Key == "XeniaCompat"), Is.True, "Unknown field must survive");

            SteamShortcut foreign = reloaded.Shortcuts[1];
            Assert.That(foreign.AppName, Is.EqualTo("Halo: Reach \U0001F3AE"), "Original title must survive");
            Assert.That(foreign.Exe, Is.EqualTo(@"D:\Games\halo.exe"), "Foreign paths must not be reformatted");
            Assert.That(foreign.UnknownFields.Count(f => f.Key == "SteamNewFlag"), Is.EqualTo(1), "Unknown int must survive");
            Assert.That(foreign.UnknownFields.Count(f => f.Key == "metadata"), Is.EqualTo(1), "Unknown dictionary must survive");

            Assert.That(reloaded.Shortcuts[2].AppName, Is.EqualTo("New Game"));
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void FromBytes_UnknownTypeByte_ThrowsFormatException()
    {
        // Arrange - valid structure with an injected unknown type marker (0x05)
        List<byte> bytes =
        [
            0x00, // root dictionary
            .. Encoding.UTF8.GetBytes("shortcuts"),
            0x00,
            0x00, // entry dictionary
            .. Encoding.UTF8.GetBytes("0"),
            0x00,
            0x02, .. Encoding.UTF8.GetBytes("appid"), 0x00, 0x34, 0xBC, 0x12, 0x8A,
            0x01, .. Encoding.UTF8.GetBytes("AppName"), 0x00, (byte)'X', 0x00,
            0x05, .. Encoding.UTF8.GetBytes("badkey"), 0x00, // unknown type
            0x08, // entry end
            0x08 // root end
        ];

        // Act & Assert - must fail loudly instead of silently desyncing and stripping data
        FormatException? ex = Assert.Throws<FormatException>(() => SteamShortcutsFile.FromBytes([.. bytes]));
        Assert.That(ex!.Message, Does.Contain("0x05"));
    }

    [Test]
    public void FromBytes_TruncatedData_ThrowsFormatException()
    {
        // Arrange - a valid file cut short mid-entry
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        SteamShortcut shortcut = new SteamShortcut { AppName = "App", Exe = "app.exe" };
        shortcut.SetAppIdFromUint(0x80001234);
        shortcutsFile.AddShortcut(shortcut);
        byte[] valid = shortcutsFile.ToBytes();

        // Act & Assert
        Assert.Throws<FormatException>(() => SteamShortcutsFile.FromBytes(valid[..^3]));
    }

    [Test]
    public void FromBytes_TrailingGarbage_ThrowsFormatException()
    {
        // Arrange - valid file followed by unparsed data
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        SteamShortcut shortcut = new SteamShortcut { AppName = "App", Exe = "app.exe" };
        shortcut.SetAppIdFromUint(0x80001234);
        shortcutsFile.AddShortcut(shortcut);

        List<byte> corrupted = [.. shortcutsFile.ToBytes(), 0x01, (byte)'A', 0x00];

        // Act & Assert
        Assert.Throws<FormatException>(() => SteamShortcutsFile.FromBytes([.. corrupted]));
    }

    [Test]
    public void FromBytes_LegacyExtraTerminators_LoadsSuccessfully()
    {
        // Arrange - files written by older Xenia Manager versions had extra End markers
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        SteamShortcut shortcut = new SteamShortcut { AppName = "App", Exe = "app.exe" };
        shortcut.SetAppIdFromUint(0x80001234);
        shortcutsFile.AddShortcut(shortcut);

        List<byte> legacy = [.. shortcutsFile.ToBytes(), 0x08, 0x08];

        // Act
        SteamShortcutsFile loaded = SteamShortcutsFile.FromBytes([.. legacy]);

        // Assert
        Assert.That(loaded.Shortcuts, Has.Count.EqualTo(1));
        Assert.That(loaded.Shortcuts[0].AppName, Is.EqualTo("App"));
    }

    #endregion

    #region Edge Cases

    [Test]
    public void SteamShortcut_BooleanProperties_DefaultToFalse()
    {
        // Arrange
        SteamShortcut shortcut = new SteamShortcut();

        // Assert
        Assert.That(shortcut.IsHidden, Is.False);
        Assert.That(shortcut.AllowDesktopConfig, Is.False);
        Assert.That(shortcut.AllowOverlay, Is.False);
        Assert.That(shortcut.OpenVR, Is.False);
        Assert.That(shortcut.Devkit, Is.False);
    }

    [Test]
    public void SteamShortcut_StringProperties_DefaultToNull()
    {
        // Arrange
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
    }

    [Test]
    public void SteamShortcutsFile_ToBytes_MultipleShortcuts_WritesAllShortcuts()
    {
        // Arrange
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        for (int i = 0; i < 5; i++)
        {
            SteamShortcut shortcut = new SteamShortcut
            {
                AppName = $"App {i}",
                Exe = $@"C:\App{i}\app.exe"
            };
            shortcut.SetAppIdFromUint((uint)(0x80001000 + i));
            shortcutsFile.AddShortcut(shortcut);
        }

        // Act
        byte[] bytes = shortcutsFile.ToBytes();

        // Assert - Reload and verify all shortcuts are present
        SteamShortcutsFile loaded = SteamShortcutsFile.FromBytes(bytes);
        Assert.That(loaded.Shortcuts, Has.Count.EqualTo(5));
        for (int i = 0; i < 5; i++)
        {
            Assert.That(loaded.Shortcuts[i].AppName, Is.EqualTo($"App {i}"));
        }
    }

    [Test]
    public void SteamShortcutsFile_RemoveAllShortcuts_ToBytes_WritesEmptyFile()
    {
        // Arrange
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Create();
        shortcutsFile.AddShortcut(new SteamShortcut { AppName = "App 1" });
        shortcutsFile.RemoveShortcutAt(0);

        // Act
        byte[] bytes = shortcutsFile.ToBytes();

        // Assert
        Assert.That(bytes, Is.Not.Null);
        Assert.That(bytes.Length, Is.GreaterThan(0)); // Should still have root structure

        // Reload and verify
        SteamShortcutsFile loaded = SteamShortcutsFile.FromBytes(bytes);
        Assert.That(loaded.Shortcuts, Is.Empty);
    }

    #endregion
}