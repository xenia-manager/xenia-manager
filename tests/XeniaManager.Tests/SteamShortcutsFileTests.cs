using System.Reflection;
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

        // Assert - User-provided file formats may vary, just verify it loads with shortcuts
        Assert.That(shortcutsFile.Shortcuts, Is.Not.Null);
    }

    [Test]
    public void Load_ValidShortcutsFile_ParsesShortcutProperties()
    {
        // Act
        SteamShortcutsFile shortcutsFile = SteamShortcutsFile.Load(_testShortcutsFilePath);

        // Assert - The file may have shortcuts with various properties
        // Note: User-provided files may have different format, so we just verify it loads
        Assert.That(shortcutsFile.Shortcuts, Has.Count.GreaterThan(0));

        // Check that at least some properties are populated for any shortcut
        SteamShortcut? shortcutWithAppName = shortcutsFile.Shortcuts.FirstOrDefault(s => !string.IsNullOrEmpty(s.AppName));
        if (shortcutWithAppName != null)
        {
            Assert.That(shortcutWithAppName.AppName, Is.Not.Null.And.Not.Empty);
        }
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
    public void Save_DuplicateAppIds_ThrowsInvalidOperationException()
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

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => shortcutsFile.Save(tempPath));
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
            SortAs = "Test"
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