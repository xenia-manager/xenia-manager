using System.Reflection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Models.Gpd;

namespace XeniaManager.Tests;

[TestFixture]
public class GpdFileTests
{
    private string _assetsFolder = string.Empty;

    // GPD file paths - update these if test files change
    private string _corruptedAchievementsPath = string.Empty;
    private string _achievementWithoutImagesPath = string.Empty;
    private string _achievementWithImagesPath = string.Empty;
    private string _dashboardGpdPath = string.Empty;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Generate test asset files before any tests run
        GenerateTestAssetFiles();
    }

    [SetUp]
    public void Setup()
    {
        // Get the path to the Assets directory
        string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        _assetsFolder = Path.Combine(assemblyLocation, "Assets");

        // Initialize GPD file paths
        // File descriptions:
        // - Corrupted Achievements Test.gpd: Contains broken/corrupted achievement entries
        // - Achievement File - Without Images.gpd: Achievement file without image entries
        // - Achievement File - With Images.gpd: Achievement file with valid PNG images
        // - Dashboard GPD File.gpd: Dashboard GPD with title sync information
        _corruptedAchievementsPath = Path.Combine(_assetsFolder, "Corrupted Achievements Test.gpd");
        _achievementWithoutImagesPath = Path.Combine(_assetsFolder, "Achievement File - Without Images.gpd");
        _achievementWithImagesPath = Path.Combine(_assetsFolder, "Achievement File - With Images.gpd");
        _dashboardGpdPath = Path.Combine(_assetsFolder, "Dashboard GPD File.gpd");
    }

    #region Asset Generation

    /// <summary>
    /// Generates test GPD asset files for use in other tests.
    /// This is called once at the start of all tests to ensure assets exist.
    /// </summary>
    private void GenerateTestAssetFiles()
    {
        string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        _assetsFolder = Path.Combine(assemblyLocation, "Assets");

        // Ensure the assets folder exists
        if (!Directory.Exists(_assetsFolder))
        {
            Directory.CreateDirectory(_assetsFolder);
            TestContext.Out.WriteLine($"Created assets folder at {_assetsFolder}");
        }

        // Load the embedded icon resource
        byte[] iconPngData = LoadEmbeddedIcon();
        Assume.That(iconPngData, Is.Not.Null.And.Length.GreaterThan(0), "Failed to load embedded icon resource");
        TestContext.Out.WriteLine($"Loaded embedded icon: {iconPngData.Length} bytes");

        // Generate each test file
        GenerateCorruptedAchievementsTestFile(_assetsFolder);
        GenerateAchievementFileWithoutImages(_assetsFolder);
        GenerateAchievementFileWithImages(_assetsFolder, iconPngData);
        GenerateDashboardGpdFile(_assetsFolder);

        TestContext.Out.WriteLine($"Successfully generated all test asset files in {_assetsFolder}");
    }

    /// <summary>
    /// Loads the embedded Icon.png resource from XeniaManager.Core.
    /// </summary>
    private static byte[] LoadEmbeddedIcon()
    {
        Assembly coreAssembly = typeof(GpdFile).Assembly;
        using Stream? stream = coreAssembly.GetManifestResourceStream("XeniaManager.Core.Assets.Artwork.Icon.png");
        Assume.That(stream, Is.Not.Null, "Failed to find embedded resource: XeniaManager.Core.Assets.Artwork.Icon.png");

        using MemoryStream memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Generates "Corrupted Achievements Test.gpd" with invalid achievement entries.
    /// </summary>
    private static void GenerateCorruptedAchievementsTestFile(string assetsFolder)
    {
        string filePath = Path.Combine(assetsFolder, "Corrupted Achievements Test.gpd");
        TestContext.Out.WriteLine($"Generating: {Path.GetFileName(filePath)}");

        using GpdFile gpd = GpdFile.Create();

        // Add some valid achievements
        gpd.AddAchievement(new AchievementEntry
        {
            AchievementId = 0x0001,
            ImageId = 0x0001,
            Gamerscore = 10,
            Name = "Valid Achievement 1",
            UnlockedDescription = "This is valid",
            LockedDescription = "Still locked"
        });

        gpd.AddAchievement(new AchievementEntry
        {
            AchievementId = 0x0002,
            ImageId = 0x0002,
            Gamerscore = 20,
            Name = "Valid Achievement 2",
            UnlockedDescription = "Also valid",
            LockedDescription = "Locked"
        });

        // Add corrupted achievement entries by manually manipulating the data
        // This simulates a corrupted GPD file
        EntryTableEntry corruptedEntry = new EntryTableEntry
        {
            Namespace = EntryNamespace.Achievement,
            Id = 0x0003,
            OffsetSpecifier = (uint)gpd.Data.Length,
            Length = 4 // Intentionally too short to cause corruption
        };
        gpd.Entries.Add(corruptedEntry);

        // Add minimal corrupted data using reflection
        byte[] corruptedData = [0x00, 0x00, 0x00, 0x00];
        byte[] newData = new byte[gpd.Data.Length + corruptedData.Length];
        gpd.Data.CopyTo(newData, 0);
        corruptedData.CopyTo(newData, gpd.Data.Length);

        PropertyInfo? dataProperty = typeof(GpdFile).GetProperty("Data", BindingFlags.Public | BindingFlags.Instance);
        dataProperty?.SetValue(gpd, newData);

        // Update header count using reflection
        PropertyInfo? headerProperty = typeof(GpdFile).GetProperty("Header", BindingFlags.Public | BindingFlags.Instance);
        XdbfHeader currentHeader = gpd.Header;
        currentHeader.EntryCount = (uint)gpd.Entries.Count;
        headerProperty?.SetValue(gpd, currentHeader);

        gpd.Save(filePath);
        TestContext.Out.WriteLine($"  - Added 2 valid achievements and 1 corrupted entry");
    }

    /// <summary>
    /// Generates "Achievement File - Without Images.gpd" with achievements but no image entries.
    /// </summary>
    private static void GenerateAchievementFileWithoutImages(string assetsFolder)
    {
        string filePath = Path.Combine(assetsFolder, "Achievement File - Without Images.gpd");
        TestContext.Out.WriteLine($"Generating: {Path.GetFileName(filePath)}");

        using GpdFile gpd = GpdFile.Create();

        // Add achievements without corresponding image entries
        for (uint i = 1; i <= 5; i++)
        {
            gpd.AddAchievement(new AchievementEntry
            {
                AchievementId = 0x1000 + i,
                ImageId = 0x2000 + i, // Reference to non-existent images
                Gamerscore = (int)i * 5,
                Name = $"Achievement {i}",
                UnlockedDescription = $"Unlocked description for achievement {i}",
                LockedDescription = $"Locked description for achievement {i}"
            });
        }

        gpd.Save(filePath);
        TestContext.Out.WriteLine($"  - Added 5 achievements without image entries");
    }

    /// <summary>
    /// Generates "Achievement File - With Images.gpd" with achievements and PNG images.
    /// </summary>
    private static void GenerateAchievementFileWithImages(string assetsFolder, byte[] iconPngData)
    {
        string filePath = Path.Combine(assetsFolder, "Achievement File - With Images.gpd");
        TestContext.Out.WriteLine($"Generating: {Path.GetFileName(filePath)}");

        using GpdFile gpd = GpdFile.Create();

        // Add images first
        for (uint i = 1; i <= 3; i++)
        {
            gpd.AddImage(0x1000 + i, iconPngData);
        }

        // Add achievements that reference the images
        for (uint i = 1; i <= 3; i++)
        {
            gpd.AddAchievement(new AchievementEntry
            {
                AchievementId = 0x2000 + i,
                ImageId = 0x1000 + i,
                Gamerscore = (int)i * 10,
                Name = $"Achievement {i} with Image",
                UnlockedDescription = $"Unlocked description for achievement {i}",
                LockedDescription = $"Locked description for achievement {i}",
                IsEarned = i <= 2 // The first two are unlocked
            });
        }

        gpd.Save(filePath);
        TestContext.Out.WriteLine($"  - Added 3 images and 3 achievements with images");
    }

    /// <summary>
    /// Generates "Dashboard GPD File.gpd" with title and setting entries.
    /// </summary>
    private static void GenerateDashboardGpdFile(string assetsFolder)
    {
        string filePath = Path.Combine(assetsFolder, "Dashboard GPD File.gpd");
        TestContext.Out.WriteLine($"Generating: {Path.GetFileName(filePath)}");

        using GpdFile gpd = GpdFile.Create();

        // Add title entries
        gpd.AddTitle(new TitleEntry
        {
            TitleId = 0x415607D1,
            AchievementCount = 50,
            AchievementUnlockedCount = 25,
            GamerscoreTotal = 1000,
            GamerscoreUnlocked = 500
        });

        gpd.AddTitle(new TitleEntry
        {
            TitleId = 0x415607D2,
            AchievementCount = 30,
            AchievementUnlockedCount = 10,
            GamerscoreTotal = 500,
            GamerscoreUnlocked = 150
        });

        // Add setting entries
        gpd.AddSetting(new SettingEntry
        {
            SettingId = 0x0001,
            DataType = SettingDataType.Int32,
            Data = BitConverter.GetBytes(1)
        });

        gpd.AddSetting(new SettingEntry
        {
            SettingId = 0x0002,
            DataType = SettingDataType.String,
            Data = System.Text.Encoding.Unicode.GetBytes("TestSetting\0")
        });

        gpd.Save(filePath);
        TestContext.Out.WriteLine($"  - Added 2 titles and 2 settings");
    }

    #endregion

    #region Load Tests

    [Test]
    public void Load_AchievementFileWithBrokenEntries_SuccessfullyLoads()
    {
        // Arrange - Corrupted Achievements Test.gpd contains broken/corrupted achievement entries
        Assume.That(File.Exists(_corruptedAchievementsPath), Is.True, $"Test asset not found: {_corruptedAchievementsPath}");

        // Act
        using GpdFile gpd = GpdFile.Load(_corruptedAchievementsPath);

        // Assert
        Assert.That(gpd, Is.Not.Null);
        Assert.That(gpd.Entries.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Load_AchievementFileWithoutImages_SuccessfullyLoads()
    {
        // Arrange - Achievement File - Without Images.gpd has achievements but no images
        Assume.That(File.Exists(_achievementWithoutImagesPath), Is.True, $"Test asset not found: {_achievementWithoutImagesPath}");

        // Act
        using GpdFile gpd = GpdFile.Load(_achievementWithoutImagesPath);

        // Assert
        Assert.That(gpd, Is.Not.Null);
        Assert.That(gpd.Entries.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Load_AchievementFileWithImages_SuccessfullyLoads()
    {
        // Arrange - Achievement File - With Images.gpd has achievements and images
        Assume.That(File.Exists(_achievementWithImagesPath), Is.True, $"Test asset not found: {_achievementWithImagesPath}");

        // Act
        using GpdFile gpd = GpdFile.Load(_achievementWithImagesPath);

        // Assert
        Assert.That(gpd, Is.Not.Null);
        Assert.That(gpd.Entries.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Load_DashboardGpd_SuccessfullyLoads()
    {
        // Arrange - Dashboard GPD File.gpd is the dashboard GPD with title sync information
        Assume.That(File.Exists(_dashboardGpdPath), Is.True, $"Test asset not found: {_dashboardGpdPath}");

        // Act
        using GpdFile gpd = GpdFile.Load(_dashboardGpdPath);

        // Assert
        Assert.That(gpd, Is.Not.Null);
        Assert.That(gpd.Entries.Count, Is.GreaterThan(0));
    }

    [Test]
    public void Load_NonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        string nonexistentPath = Path.Combine(_assetsFolder, "nonexistent.gpd");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => GpdFile.Load(nonexistentPath));
    }

    #endregion

    #region Achievement Tests

    [Test]
    public void Load_AchievementFile_HasValidAchievements()
    {
        // Arrange
        Assume.That(File.Exists(_achievementWithImagesPath), Is.True, $"Test asset not found: {_achievementWithImagesPath}");

        // Act
        using GpdFile gpd = GpdFile.Load(_achievementWithImagesPath);
        List<AchievementEntry> achievements = gpd.Achievements.ToList();

        // Assert
        Assert.That(achievements.Count, Is.GreaterThan(0));

        // Verify achievements have valid data
        foreach (AchievementEntry achievement in achievements.Take(5))
        {
            Assert.That(achievement.IsValid, Is.True);
            Assert.That(achievement.AchievementId, Is.GreaterThan(0));
        }
    }

    [Test]
    public void Load_AchievementFile_HasBrokenAchievements()
    {
        // Arrange - Corrupted Achievements Test.gpd contains broken achievement entries
        Assume.That(File.Exists(_corruptedAchievementsPath), Is.True, $"Test asset not found: {_corruptedAchievementsPath}");

        // Act
        using GpdFile gpd = GpdFile.Load(_corruptedAchievementsPath);
        List<AchievementEntry> validAchievements = gpd.Achievements.ToList();
        List<AchievementEntry> invalidAchievements = gpd.GetInvalidAchievements();

        // Assert - File should have both valid and invalid achievements
        Assert.That(validAchievements.Count, Is.GreaterThan(0), "Should have valid achievements");
        Assert.That(invalidAchievements.Count, Is.GreaterThan(0), "Should have broken achievements");

        // Verify invalid achievements have error messages
        foreach (AchievementEntry invalid in invalidAchievements)
        {
            Assert.That(invalid.IsValid, Is.False);
            Assert.That(invalid.ValidationError, Is.Not.Null.And.Not.Empty);
        }
    }

    [Test]
    public void Load_FileWithoutImages_HasNoImageEntries()
    {
        // Arrange
        Assume.That(File.Exists(_achievementWithoutImagesPath), Is.True, $"Test asset not found: {_achievementWithoutImagesPath}");
        Assume.That(File.Exists(_achievementWithImagesPath), Is.True, $"Test asset not found: {_achievementWithImagesPath}");

        // Act
        using GpdFile gpdWithout = GpdFile.Load(_achievementWithoutImagesPath);
        using GpdFile gpdWith = GpdFile.Load(_achievementWithImagesPath);

        List<ImageEntry> imagesWithout = gpdWithout.Images.ToList();
        List<ImageEntry> imagesWith = gpdWith.Images.ToList();

        // Assert - Without Images version should have fewer images
        Assert.That(imagesWithout.Count, Is.LessThan(imagesWith.Count));
    }

    [Test]
    public void Load_FileWithImages_HasValidPngImages()
    {
        // Arrange
        Assume.That(File.Exists(_achievementWithImagesPath), Is.True, $"Test asset not found: {_achievementWithImagesPath}");

        // Act
        using GpdFile gpd = GpdFile.Load(_achievementWithImagesPath);
        List<ImageEntry> images = gpd.Images.ToList();

        // Assert
        Assert.That(images.Count, Is.GreaterThan(0));

        // Verify images are valid PNGs
        foreach (ImageEntry image in images.Take(3))
        {
            Assert.That(image.IsValid, Is.True);
            Assert.That(image.IsValidPng, Is.True, "Image should be valid PNG");
        }
    }

    #endregion

    #region Dashboard GPD Tests

    [Test]
    public void Load_DashboardGpd_HasTitleEntries()
    {
        // Arrange - Dashboard GPD contains title information
        Assume.That(File.Exists(_dashboardGpdPath), Is.True, $"Test asset not found: {_dashboardGpdPath}");

        // Act
        using GpdFile gpd = GpdFile.Load(_dashboardGpdPath);
        List<TitleEntry> titles = gpd.Titles.ToList();

        // Assert
        Assert.That(titles.Count, Is.GreaterThan(0), "Dashboard GPD should have title entries");
    }

    [Test]
    public void Load_DashboardGpd_HasSettings()
    {
        // Arrange
        Assume.That(File.Exists(_dashboardGpdPath), Is.True, $"Test asset not found: {_dashboardGpdPath}");

        // Act
        using GpdFile gpd = GpdFile.Load(_dashboardGpdPath);
        List<SettingEntry> settings = gpd.Settings.ToList();

        // Assert
        Assert.That(settings.Count, Is.GreaterThan(0), "Dashboard GPD should have settings");
    }

    #endregion

    #region Achievement Management Tests

    [Test]
    public void GetAchievement_ExistingAchievement_ReturnsAchievement()
    {
        // Arrange
        Assume.That(File.Exists(_achievementWithImagesPath), Is.True, $"Test asset not found: {_achievementWithImagesPath}");
        using GpdFile gpd = GpdFile.Load(_achievementWithImagesPath);
        AchievementEntry? firstAchievement = gpd.Achievements.FirstOrDefault();
        Assume.That(firstAchievement, Is.Not.Null);

        // Act
        AchievementEntry? foundAchievement = gpd.GetAchievement(firstAchievement.AchievementId);

        // Assert
        Assert.That(foundAchievement, Is.Not.Null);
        Assert.That(foundAchievement!.AchievementId, Is.EqualTo(firstAchievement.AchievementId));
    }

    [Test]
    public void GetAchievement_NonExistent_ReturnsNull()
    {
        // Arrange
        Assume.That(File.Exists(_achievementWithImagesPath), Is.True, $"Test asset not found: {_achievementWithImagesPath}");
        using GpdFile gpd = GpdFile.Load(_achievementWithImagesPath);

        // Act
        AchievementEntry? foundAchievement = gpd.GetAchievement(0xFFFFFFFF);

        // Assert
        Assert.That(foundAchievement, Is.Null);
    }

    [Test]
    public void UnlockAchievement_ExistingAchievement_SuccessfullyUnlocks()
    {
        // Arrange
        Assume.That(File.Exists(_achievementWithImagesPath), Is.True, $"Test asset not found: {_achievementWithImagesPath}");
        using GpdFile gpd = GpdFile.Load(_achievementWithImagesPath);
        AchievementEntry? firstAchievement = gpd.Achievements.FirstOrDefault();
        Assume.That(firstAchievement, Is.Not.Null);

        // Act
        bool result = gpd.UnlockAchievement(firstAchievement.AchievementId);

        // Assert
        Assert.That(result, Is.True);

        AchievementEntry? updatedAchievement = gpd.GetAchievement(firstAchievement.AchievementId);
        Assert.That(updatedAchievement, Is.Not.Null);
        Assert.That(updatedAchievement!.IsEarned, Is.True);
    }

    [Test]
    public void LockAchievement_ExistingAchievement_SuccessfullyLocks()
    {
        // Arrange
        Assume.That(File.Exists(_achievementWithImagesPath), Is.True, $"Test asset not found: {_achievementWithImagesPath}");
        using GpdFile gpd = GpdFile.Load(_achievementWithImagesPath);
        AchievementEntry? firstAchievement = gpd.Achievements.FirstOrDefault();
        Assume.That(firstAchievement, Is.Not.Null);

        // First, unlock it
        gpd.UnlockAchievement(firstAchievement.AchievementId);

        // Act
        bool result = gpd.LockAchievement(firstAchievement.AchievementId);

        // Assert
        Assert.That(result, Is.True);

        AchievementEntry? updatedAchievement = gpd.GetAchievement(firstAchievement.AchievementId);
        Assert.That(updatedAchievement, Is.Not.Null);
        Assert.That(updatedAchievement!.IsEarned, Is.False);
    }

    [Test]
    public void UnlockAchievement_NonExistent_ReturnsFalse()
    {
        // Arrange
        Assume.That(File.Exists(_achievementWithImagesPath), Is.True, $"Test asset not found: {_achievementWithImagesPath}");
        using GpdFile gpd = GpdFile.Load(_achievementWithImagesPath);

        // Act
        bool result = gpd.UnlockAchievement(0xFFFFFFFF);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region Gamerscore Tests

    [Test]
    public void GetTotalGamerscore_ReturnsValidScore()
    {
        // Arrange
        Assume.That(File.Exists(_achievementWithImagesPath), Is.True, $"Test asset not found: {_achievementWithImagesPath}");
        using GpdFile gpd = GpdFile.Load(_achievementWithImagesPath);

        // Act
        int totalScore = gpd.GetTotalGamerscore();

        // Assert
        Assert.That(totalScore, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void GetUnlockedAchievementCount_ReturnsValidCount()
    {
        // Arrange
        Assume.That(File.Exists(_achievementWithImagesPath), Is.True, $"Test asset not found: {_achievementWithImagesPath}");
        using GpdFile gpd = GpdFile.Load(_achievementWithImagesPath);

        // Act
        int unlockedCount = gpd.GetUnlockedAchievementCount();

        // Assert
        Assert.That(unlockedCount, Is.GreaterThanOrEqualTo(0));
        Assert.That(unlockedCount, Is.LessThanOrEqualTo(gpd.GetTotalAchievementCount()));
    }

    [Test]
    public void GetTotalPossibleGamerscore_ReturnsValidScore()
    {
        // Arrange
        Assume.That(File.Exists(_achievementWithImagesPath), Is.True, $"Test asset not found: {_achievementWithImagesPath}");
        using GpdFile gpd = GpdFile.Load(_achievementWithImagesPath);

        // Act
        int totalPossible = gpd.GetTotalPossibleGamerscore();

        // Assert
        Assert.That(totalPossible, Is.GreaterThan(0));
    }

    #endregion

    #region Invalid Entry Handling Tests

    [Test]
    public void Load_AllFiles_HandlesInvalidEntriesGracefully()
    {
        // Arrange
        string[] gpdFiles =
        [
            _corruptedAchievementsPath,
            _achievementWithImagesPath,
            _achievementWithoutImagesPath,
            _dashboardGpdPath
        ];

        // Act & Assert - All files should load without throwing exceptions
        foreach (string gpdPath in gpdFiles)
        {
            Assume.That(File.Exists(gpdPath), Is.True, $"Test asset not found: {gpdPath}");

            using GpdFile gpd = GpdFile.Load(gpdPath);

            Assert.That(gpd, Is.Not.Null);
            Assert.That(gpd.Entries.Count, Is.GreaterThan(0));

            // Get invalid entries
            List<AchievementEntry> invalidAchievements = gpd.GetInvalidAchievements();
            List<ImageEntry> invalidImages = gpd.GetInvalidImages();
            List<SettingEntry> invalidSettings = gpd.GetInvalidSettings();
            List<TitleEntry> invalidTitles = gpd.GetInvalidTitles();
            List<StringEntry> invalidStrings = gpd.GetInvalidStrings();

            // Log invalid entries for debugging
            if (invalidAchievements.Any())
            {
                TestContext.Out.WriteLine($"{Path.GetFileName(gpdPath)} has {invalidAchievements.Count} invalid achievements");
            }
        }
    }

    [Test]
    public void GetAllInvalidEntries_ReturnsProperDictionary()
    {
        // Arrange
        Assume.That(File.Exists(_corruptedAchievementsPath), Is.True, $"Test asset not found: {_corruptedAchievementsPath}");
        using GpdFile gpd = GpdFile.Load(_corruptedAchievementsPath);

        // Act
        Dictionary<string, List<object>> invalidEntries = gpd.GetAllInvalidEntries();

        // Assert
        Assert.That(invalidEntries, Is.Not.Null);
        Assert.That(invalidEntries.ContainsKey("Achievements"), Is.True);
        Assert.That(invalidEntries.ContainsKey("Images"), Is.True);
        Assert.That(invalidEntries.ContainsKey("Settings"), Is.True);
        Assert.That(invalidEntries.ContainsKey("Titles"), Is.True);
        Assert.That(invalidEntries.ContainsKey("Strings"), Is.True);
    }

    #endregion

    #region Save and Load Round-Trip Tests

    [Test]
    public void Save_AndLoad_RoundTrip_PreservesData()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_roundtrip_{Guid.NewGuid()}.gpd");
        try
        {
            using (GpdFile gpd = GpdFile.Create())
            {
                AchievementEntry achievement = new AchievementEntry
                {
                    AchievementId = 0x0001,
                    ImageId = 0x0001,
                    Gamerscore = 10,
                    Name = "Test Achievement",
                    UnlockedDescription = "Unlocked Description",
                    LockedDescription = "Locked Description"
                };
                gpd.AddAchievement(achievement);
                gpd.Save(tempPath);
            }

            // Act
            using (GpdFile loadedGpd = GpdFile.Load(tempPath))
            {
                // Assert
                Assert.That(loadedGpd, Is.Not.Null);
                List<AchievementEntry> achievements = loadedGpd.Achievements.ToList();
                Assert.That(achievements.Count, Is.EqualTo(1));
                Assert.That(achievements[0].Name, Is.EqualTo("Test Achievement"));
                Assert.That(achievements[0].Gamerscore, Is.EqualTo(10));
            }
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
    public void Save_ModifiedFile_PreservesChanges()
    {
        // Arrange
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_modified_{Guid.NewGuid()}.gpd");
        Assume.That(File.Exists(_achievementWithImagesPath), Is.True, $"Test asset not found: {_achievementWithImagesPath}");

        try
        {
            File.Copy(_achievementWithImagesPath, tempPath, true);

            using (GpdFile gpd = GpdFile.Load(tempPath))
            {
                AchievementEntry? firstAchievement = gpd.Achievements.FirstOrDefault();
                Assume.That(firstAchievement, Is.Not.Null);

                gpd.UnlockAchievement(firstAchievement.AchievementId);
                gpd.Save(tempPath);
            }

            // Act - Reload and verify
            using (GpdFile loadedGpd = GpdFile.Load(tempPath))
            {
                AchievementEntry? updatedAchievement = loadedGpd.GetAchievement(
                    loadedGpd.Achievements.First().AchievementId);

                Assert.That(updatedAchievement, Is.Not.Null);
                Assert.That(updatedAchievement!.IsEarned, Is.True);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    #endregion

    #region Creation Tests

    [Test]
    public void Create_NewGpdFile_ReturnsValidInstance()
    {
        // Act
        using GpdFile gpd = GpdFile.Create();

        // Assert
        Assert.That(gpd, Is.Not.Null);
        Assert.That(gpd.IsBigEndian, Is.True);
        Assert.That(gpd.Entries, Is.Empty);
        Assert.That(gpd.Header.Version, Is.EqualTo(0x10000));
    }

    [Test]
    public void Create_LittleEndianGpdFile_ReturnsValidInstance()
    {
        // Act
        using GpdFile gpd = GpdFile.Create(isBigEndian: false);

        // Assert
        Assert.That(gpd, Is.Not.Null);
        Assert.That(gpd.IsLittleEndian, Is.True);
    }

    #endregion

    #region Entry Type Tests

    [Test]
    public void AchievementEntry_ValidData_MarksEntryAsValid()
    {
        // Arrange
        byte[] validData =
        [
            0x00, 0x00, 0x00, 0x1C, // Struct size (28)
            0x00, 0x00, 0x00, 0x01, // Achievement ID
            0x00, 0x00, 0x00, 0x01, // Image ID
            0x00, 0x00, 0x00, 0x0A, // Gamerscore (10)
            0x00, 0x00, 0x00, 0x00, // Flags
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // Unlock Time
            0x00, 0x54, 0x00, 0x65, 0x00, 0x73, 0x00, 0x74, 0x00, 0x00, // "Test"
            0x00, 0x00 // Empty descriptions
        ];

        // Act
        AchievementEntry entry = AchievementEntry.FromBytes(validData, 0, (uint)validData.Length);

        // Assert
        Assert.That(entry.IsValid, Is.True);
        Assert.That(entry.AchievementId, Is.EqualTo(1));
        Assert.That(entry.Gamerscore, Is.EqualTo(10));
        Assert.That(entry.Name, Is.EqualTo("Test"));
    }

    [Test]
    public void AchievementEntry_InvalidData_MarksEntryAsInvalid()
    {
        // Arrange
        byte[] invalidData = [0x00, 0x00, 0x00, 0x00];

        // Act
        AchievementEntry entry = AchievementEntry.FromBytes(invalidData, 0, (uint)invalidData.Length);

        // Assert
        Assert.That(entry.IsValid, Is.False);
        Assert.That(entry.ValidationError, Is.Not.Null.And.Not.Empty);
    }

    #endregion

    #region Sync Entry Tests

    [Test]
    public void SyncDataEntry_GetNextSyncId_IncrementsCounter()
    {
        // Arrange
        SyncDataEntry syncData = new SyncDataEntry { NextSyncId = 100 };

        // Act
        ulong id1 = syncData.GetNextSyncId();
        ulong id2 = syncData.GetNextSyncId();

        // Assert
        Assert.That(id1, Is.EqualTo(100));
        Assert.That(id2, Is.EqualTo(101));
        Assert.That(syncData.NextSyncId, Is.EqualTo(102));
    }

    [Test]
    public void SyncDataEntry_UpdateLastSynced_UpdatesFields()
    {
        // Arrange
        SyncDataEntry syncData = new SyncDataEntry();
        DateTime testTime = new DateTime(2024, 1, 15, 10, 30, 0);

        // Act
        syncData.UpdateLastSynced(12345, testTime);

        // Assert
        Assert.That(syncData.LastSyncedId, Is.EqualTo(12345));
        Assert.That(syncData.LastSyncedDateTime, Is.EqualTo(testTime));
    }

    [Test]
    public void SyncListEntry_AddItem_AddsToList()
    {
        // Arrange
        SyncListEntry syncList = new SyncListEntry();

        // Act
        syncList.AddItem(0x0001, 0x1000);
        syncList.AddItem(0x0002, 0x1001);

        // Assert
        Assert.That(syncList.Items.Count, Is.EqualTo(2));
        Assert.That(syncList.TotalSyncItems, Is.EqualTo(1));
    }

    #endregion

    #region Endianness Tests

    [Test]
    public void XdbfHeader_BigEndian_WritesCorrectly()
    {
        // Arrange
        XdbfHeader header = new XdbfHeader
        {
            Magic = 0x58444246,
            Version = 0x10000,
            EntryTableLength = 512,
            EntryCount = 10,
            FreeSpaceTableLength = 512,
            FreeSpaceTableEntryCount = 5
        };

        // Act
        byte[] bytes = header.ToBytes();
        XdbfHeader loaded = XdbfHeader.FromBytes(bytes);

        // Assert
        Assert.That(loaded.Magic, Is.EqualTo(header.Magic));
        Assert.That(loaded.Version, Is.EqualTo(header.Version));
        Assert.That(loaded.EntryCount, Is.EqualTo(header.EntryCount));
        Assert.That(loaded.IsBigEndian, Is.True);
    }

    [Test]
    public void EntryTableEntry_BigEndian_WritesCorrectly()
    {
        // Arrange
        EntryTableEntry entry = new EntryTableEntry
        {
            Namespace = EntryNamespace.Achievement,
            Id = 0x0001,
            OffsetSpecifier = 0x1000,
            Length = 100
        };

        // Act
        byte[] bytes = entry.ToBytes(isBigEndian: true);
        EntryTableEntry loaded = EntryTableEntry.FromBytes(bytes, 0, isBigEndian: true);

        // Assert
        Assert.That(loaded.Namespace, Is.EqualTo(entry.Namespace));
        Assert.That(loaded.Id, Is.EqualTo(entry.Id));
        Assert.That(loaded.OffsetSpecifier, Is.EqualTo(entry.OffsetSpecifier));
        Assert.That(loaded.Length, Is.EqualTo(entry.Length));
    }

    [Test]
    public void EntryTableEntry_LittleEndian_WritesCorrectly()
    {
        // Arrange
        EntryTableEntry entry = new EntryTableEntry
        {
            Namespace = EntryNamespace.Achievement,
            Id = 0x0001,
            OffsetSpecifier = 0x1000,
            Length = 100
        };

        // Act
        byte[] bytes = entry.ToBytes(isBigEndian: false);
        EntryTableEntry loaded = EntryTableEntry.FromBytes(bytes, 0, isBigEndian: false);

        // Assert
        Assert.That(loaded.Namespace, Is.EqualTo(entry.Namespace));
        Assert.That(loaded.Id, Is.EqualTo(entry.Id));
        Assert.That(loaded.OffsetSpecifier, Is.EqualTo(entry.OffsetSpecifier));
        Assert.That(loaded.Length, Is.EqualTo(entry.Length));
    }

    #endregion

    #region Multiple Achievements Tests

    [Test]
    public void AddMultipleAchievements_AllAreRetrieved()
    {
        // Arrange
        using GpdFile gpd = GpdFile.Create();

        // Act
        for (uint i = 1; i <= 10; i++)
        {
            gpd.AddAchievement(new AchievementEntry
            {
                AchievementId = i,
                ImageId = i,
                Gamerscore = (int)i * 10,
                Name = $"Achievement {i}"
            });
        }

        // Assert
        List<AchievementEntry> achievements = gpd.Achievements.ToList();
        Assert.That(achievements.Count, Is.EqualTo(10));
        Assert.That(achievements.All(a => a.Name.StartsWith("Achievement ")), Is.True);
    }

    [Test]
    public void UnlockMultipleAchievements_AllAreUnlocked()
    {
        // Arrange
        using GpdFile gpd = GpdFile.Create();
        for (uint i = 1; i <= 5; i++)
        {
            gpd.AddAchievement(new AchievementEntry
            {
                AchievementId = i,
                Gamerscore = 10,
                Name = $"Achievement {i}"
            });
        }

        // Act
        for (uint i = 1; i <= 5; i++)
        {
            gpd.UnlockAchievement(i);
        }

        // Assert
        Assert.That(gpd.GetUnlockedAchievementCount(), Is.EqualTo(5));
        Assert.That(gpd.GetTotalGamerscore(), Is.EqualTo(50));
    }

    #endregion
}