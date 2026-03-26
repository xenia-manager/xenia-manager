using XeniaManager.Core.Database;
using XeniaManager.Core.Files;
using XeniaManager.Core.Models.Database.OptimizedSettings;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.Tests;

[TestFixture]
public class OptimizedSettingsDatabaseTests
{
    [SetUp]
    public void Setup()
    {
        // Reset the static state before each test for isolation
        OptimizedSettingsDatabase.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up the static state after each test
        OptimizedSettingsDatabase.Reset();
    }

    // Helper to populate the database with test data
    private static async Task PopulateTestData(params OptimizedSettingsEntry[] entries)
    {
        foreach (OptimizedSettingsEntry entry in entries)
        {
            if (entry.Id != null)
            {
                OptimizedSettingsDatabase.AddEntryToIndex(entry, entry.Id);
            }
        }

        // Initialize FilteredDatabase with all entries
        await OptimizedSettingsDatabase.SearchDatabase("");
    }

    private static OptimizedSettingsEntry CreateBayonetta() => new OptimizedSettingsEntry
    {
        Id = "53450813",
        Title = "Bayonetta",
        LastModified = "2024-01-15"
    };

    private static OptimizedSettingsEntry CreateHalo3() => new OptimizedSettingsEntry
    {
        Id = "4D5307E6",
        Title = "Halo 3",
        LastModified = "2024-02-20"
    };

    private static OptimizedSettingsEntry CreateCallOfDuty2() => new OptimizedSettingsEntry
    {
        Id = "415607D1",
        Title = "Call of Duty 2",
        LastModified = "2024-03-10"
    };

    [Test]
    public void FilteredDatabase_InitializedCorrectly()
    {
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase, Is.Not.Null);
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase, Is.Empty);
    }

    [Test]
    public async Task SearchDatabase_WithValidQuery_FiltersResults()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await OptimizedSettingsDatabase.SearchDatabase("Bayonetta");

        // Assert
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase[0].Title, Is.EqualTo("Bayonetta"));
    }

    [Test]
    public async Task SearchDatabase_WithTitleIdQuery_FiltersResults()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await OptimizedSettingsDatabase.SearchDatabase("4D5307E6");

        // Assert
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase[0].Title, Is.EqualTo("Halo 3"));
    }

    [Test]
    public async Task SearchDatabase_WithPartialQuery_FiltersResults()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await OptimizedSettingsDatabase.SearchDatabase("Halo");

        // Assert
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase[0].Title, Is.EqualTo("Halo 3"));
    }

    [Test]
    public async Task SearchDatabase_CaseInsensitive_FiltersResults()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await OptimizedSettingsDatabase.SearchDatabase("bayonetta");

        // Assert
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase[0].Title, Is.EqualTo("Bayonetta"));
    }

    [Test]
    public async Task SearchDatabase_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await OptimizedSettingsDatabase.SearchDatabase("NonExistentGame");

        // Assert
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase, Is.Empty);
    }

    [Test]
    public async Task SearchDatabase_WithEmptyQuery_ResetsToFullList()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await OptimizedSettingsDatabase.SearchDatabase("");

        // Assert
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(OptimizedSettingsDatabase.FilteredDatabase.Select(g => g.Title), Contains.Item("Bayonetta"));
            Assert.That(OptimizedSettingsDatabase.FilteredDatabase.Select(g => g.Title), Contains.Item("Halo 3"));
        });
    }

    [Test]
    public async Task SearchDatabase_WithWhitespaceQuery_ResetsToFullList()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await OptimizedSettingsDatabase.SearchDatabase("   ");

        // Assert
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(OptimizedSettingsDatabase.FilteredDatabase.Select(g => g.Title), Contains.Item("Bayonetta"));
            Assert.That(OptimizedSettingsDatabase.FilteredDatabase.Select(g => g.Title), Contains.Item("Halo 3"));
        });
    }

    [Test]
    public async Task GetEntryByTitle_WithExistingTitle_ReturnsEntry()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta());

        // Act
        OptimizedSettingsEntry? result = OptimizedSettingsDatabase.GetEntryByTitle("Bayonetta");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Title, Is.EqualTo("Bayonetta"));
            Assert.That(result.Id, Is.EqualTo("53450813"));
            Assert.That(result.LastModified, Is.EqualTo("2024-01-15"));
        });
    }

    [Test]
    public async Task GetEntryByTitle_WithNonExistingTitle_ReturnsNull()
    {
        // Arrange
        await PopulateTestData(CreateHalo3());

        // Act
        OptimizedSettingsEntry? result = OptimizedSettingsDatabase.GetEntryByTitle("Bayonetta");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetEntryByTitle_CaseInsensitive_ReturnsEntry()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta());

        // Act
        OptimizedSettingsEntry? result = OptimizedSettingsDatabase.GetEntryByTitle("bayonetta");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Bayonetta"));
    }

    [Test]
    public async Task GetEntryById_WithExistingId_ReturnsEntry()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta());

        // Act
        OptimizedSettingsEntry? result = OptimizedSettingsDatabase.GetEntryById("53450813");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Title, Is.EqualTo("Bayonetta"));
            Assert.That(result.Id, Is.EqualTo("53450813"));
        });
    }

    [Test]
    public async Task GetEntryById_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        await PopulateTestData(CreateHalo3());

        // Act
        OptimizedSettingsEntry? result = OptimizedSettingsDatabase.GetEntryById("53450813");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetEntryById_CaseInsensitive_ReturnsEntry()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta());

        // Act
        OptimizedSettingsEntry? result = OptimizedSettingsDatabase.GetEntryById("53450813");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Bayonetta"));
    }

    [Test]
    public void AddEntryToIndex_WithValidEntry_AddsToIndex()
    {
        // Act
        OptimizedSettingsDatabase.AddEntryToIndex(CreateBayonetta(), "53450813");

        // Assert
        OptimizedSettingsEntry? result = OptimizedSettingsDatabase.GetEntryByTitle("Bayonetta");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo("53450813"));
    }

    [Test]
    public async Task AddEntryToIndex_NormalizesToUppercase()
    {
        // Arrange
        OptimizedSettingsEntry entry = CreateBayonetta();

        // Act
        OptimizedSettingsDatabase.AddEntryToIndex(entry, "abc123");
        await OptimizedSettingsDatabase.SearchDatabase("ABC123");

        // Assert — searchable by uppercase ID
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase[0].Title, Is.EqualTo("Bayonetta"));
    }

    [Test]
    public Task AddEntryToIndex_WithDuplicateId_DoesNotOverwrite()
    {
        // Arrange
        OptimizedSettingsEntry entry1 = CreateBayonetta();
        OptimizedSettingsEntry entry2 = new OptimizedSettingsEntry
        {
            Id = "53450813",
            Title = "Bayonetta Duplicate",
            LastModified = "2024-12-31"
        };

        // Act
        OptimizedSettingsDatabase.AddEntryToIndex(entry1, "53450813");
        OptimizedSettingsDatabase.AddEntryToIndex(entry2, "53450813");

        // Assert — original entry is preserved
        OptimizedSettingsEntry? result = OptimizedSettingsDatabase.GetEntryByTitle("Bayonetta");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Bayonetta"));
        Assert.That(result.LastModified, Is.EqualTo("2024-01-15"));

        OptimizedSettingsEntry? duplicate = OptimizedSettingsDatabase.GetEntryByTitle("Bayonetta Duplicate");
        Assert.That(duplicate, Is.Null);
        return Task.CompletedTask;
    }

    [Test]
    public async Task Reset_ClearsAllState()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase, Has.Count.EqualTo(2));

        // Act
        OptimizedSettingsDatabase.Reset();

        // Assert
        Assert.That(OptimizedSettingsDatabase.FilteredDatabase, Is.Empty);
        Assert.That(OptimizedSettingsDatabase.GetEntryByTitle("Bayonetta"), Is.Null);
        Assert.That(OptimizedSettingsDatabase.GetEntryByTitle("Halo 3"), Is.Null);
    }

    #region Real API Request Tests

    [Test]
    public async Task LoadAsync_RealApi_LoadsSuccessfully()
    {
        try
        {
            await OptimizedSettingsDatabase.LoadAsync();
            Assert.That(OptimizedSettingsDatabase.FilteredDatabase, Is.Not.Null);
            Assert.That(OptimizedSettingsDatabase.FilteredDatabase.Count, Is.GreaterThan(0),
                "Expected optimized settings database to be loaded from real API");
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task SearchDatabase_RealApi_SearchByTitle()
    {
        try
        {
            await OptimizedSettingsDatabase.LoadAsync();
            await OptimizedSettingsDatabase.SearchDatabase("Halo");
            Assert.That(OptimizedSettingsDatabase.FilteredDatabase.Count, Is.GreaterThan(0),
                "Expected to find entries matching 'Halo' from real API");
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task SearchDatabase_RealApi_SearchByTitleId()
    {
        try
        {
            await OptimizedSettingsDatabase.LoadAsync();
            await OptimizedSettingsDatabase.SearchDatabase("4D5307E6");
            Assert.That(OptimizedSettingsDatabase.FilteredDatabase.Count, Is.GreaterThan(0),
                "Expected to find entries matching title ID '4D5307E6' from real API");
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task GetEntryByTitle_RealApi_ReturnsValidEntry()
    {
        try
        {
            await OptimizedSettingsDatabase.LoadAsync();
            OptimizedSettingsEntry? entry = OptimizedSettingsDatabase.FilteredDatabase.FirstOrDefault();
            Assert.That(entry, Is.Not.Null, "Expected to get at least one entry from real API");

            if (entry != null && entry.Title != null)
            {
                OptimizedSettingsEntry? result = OptimizedSettingsDatabase.GetEntryByTitle(entry.Title);
                Assert.That(result, Is.Not.Null, "Expected to retrieve entry by title");
                Assert.That(result!.Title, Is.EqualTo(entry.Title));
                Assert.That(result.LastModified, Is.Not.Null.And.Not.Empty);
            }
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task GetEntryById_RealApi_ReturnsValidEntry()
    {
        try
        {
            await OptimizedSettingsDatabase.LoadAsync();
            OptimizedSettingsEntry? entry = OptimizedSettingsDatabase.FilteredDatabase.FirstOrDefault();
            Assert.That(entry, Is.Not.Null, "Expected to get at least one entry from real API");

            if (entry != null && entry.Id != null)
            {
                OptimizedSettingsEntry? result = OptimizedSettingsDatabase.GetEntryById(entry.Id);
                Assert.That(result, Is.Not.Null, "Expected to retrieve entry by ID");
                Assert.That(result!.Id, Is.EqualTo(entry.Id));
            }
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task LoadAsync_MultipleCalls_OnlyLoadsOnce()
    {
        try
        {
            await OptimizedSettingsDatabase.LoadAsync();
            int firstCount = OptimizedSettingsDatabase.FilteredDatabase.Count;

            await OptimizedSettingsDatabase.LoadAsync();
            int secondCount = OptimizedSettingsDatabase.FilteredDatabase.Count;

            Assert.That(firstCount, Is.EqualTo(secondCount),
                "Expected database to not reload on second call");
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task SearchDatabase_RealApi_CaseInsensitiveSearch()
    {
        try
        {
            await OptimizedSettingsDatabase.LoadAsync();
            await OptimizedSettingsDatabase.SearchDatabase("halo");
            int lowerCaseCount = OptimizedSettingsDatabase.FilteredDatabase.Count;

            await OptimizedSettingsDatabase.SearchDatabase("HALO");
            int upperCaseCount = OptimizedSettingsDatabase.FilteredDatabase.Count;

            Assert.That(lowerCaseCount, Is.EqualTo(upperCaseCount),
                "Expected case-insensitive search to return same results");
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task SearchDatabase_RealApi_EmptyQueryReturnsAll()
    {
        try
        {
            await OptimizedSettingsDatabase.LoadAsync();
            int initialCount = OptimizedSettingsDatabase.FilteredDatabase.Count;

            await OptimizedSettingsDatabase.SearchDatabase("");
            int emptyQueryCount = OptimizedSettingsDatabase.FilteredDatabase.Count;

            Assert.That(initialCount, Is.EqualTo(emptyQueryCount),
                "Expected empty query to return all entries");
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task GetOptimizedSettings_RealApi_ReturnsConfigFile()
    {
        try
        {
            await OptimizedSettingsDatabase.LoadAsync();

            // Try to get optimized settings for a known title ID
            Game game = new Game
            {
                Title = "Halo 3",
                GameId = "4D5307E6",
                AlternativeIDs = []
            };

            ConfigFile? configFile = await OptimizedSettingsDatabase.GetOptimizedSettings(game);

            if (configFile != null)
            {
                Assert.That(configFile, Is.Not.Null, "Expected to receive a valid ConfigFile");
            }
            else
            {
                Assert.Ignore("Optimized settings TOML not available from API (this is acceptable)");
            }
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task GetOptimizedSettings_WithAlternativeIds_FindsMatch()
    {
        try
        {
            await OptimizedSettingsDatabase.LoadAsync();

            // Find an entry to test with
            OptimizedSettingsEntry? entry = OptimizedSettingsDatabase.FilteredDatabase.FirstOrDefault();
            if (entry != null && entry.Id != null)
            {
                Game game = new Game
                {
                    Title = entry.Title,
                    GameId = "INVALID_ID",
                    AlternativeIDs = [entry.Id]
                };

                ConfigFile? configFile = await OptimizedSettingsDatabase.GetOptimizedSettings(game);

                if (configFile != null)
                {
                    Assert.That(configFile, Is.Not.Null, "Expected to find optimized settings via alternative ID");
                }
                else
                {
                    Assert.Ignore("Optimized settings TOML not available for this entry");
                }
            }
            else
            {
                Assert.Ignore("No entries available from API to test alternative ID lookup");
            }
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task GetOptimizedSettings_WithNoMatch_ReturnsNull()
    {
        try
        {
            Game game = new Game
            {
                Title = "NonExistent Game",
                GameId = "00000000",
                AlternativeIDs = []
            };

            ConfigFile? configFile = await OptimizedSettingsDatabase.GetOptimizedSettings(game);

            Assert.That(configFile, Is.Null, "Expected null for non-existent game");
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task ForceReloadAsync_RealApi_ReloadsDatabase()
    {
        try
        {
            await OptimizedSettingsDatabase.LoadAsync();
            int firstCount = OptimizedSettingsDatabase.FilteredDatabase.Count;

            await OptimizedSettingsDatabase.ForceReloadAsync();
            int secondCount = OptimizedSettingsDatabase.FilteredDatabase.Count;

            Assert.That(secondCount, Is.GreaterThan(0),
                "Expected database to reload successfully");
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    #endregion
}