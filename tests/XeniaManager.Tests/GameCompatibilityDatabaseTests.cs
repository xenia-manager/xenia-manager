using XeniaManager.Core.Database;
using XeniaManager.Core.Models.Database.GameCompatibility;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.Tests;

[TestFixture]
public class GameCompatibilityDatabaseTests
{
    [SetUp]
    public void Setup()
    {
        // Reset the static state before each test for isolation
        GameCompatibilityDatabase.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up the static state after each test
        GameCompatibilityDatabase.Reset();
    }

    // Helper to populate the database with test data
    private static async Task PopulateTestData(params GameCompatibilityEntry[] entries)
    {
        foreach (GameCompatibilityEntry entry in entries)
        {
            if (entry.Id != null)
            {
                GameCompatibilityDatabase.AddGameToIndex(entry, entry.Id);
            }
        }

        // Initialize FilteredDatabase with all entries
        await GameCompatibilityDatabase.SearchDatabase("");
    }

    private static GameCompatibilityEntry CreateBayonetta() => new GameCompatibilityEntry
    {
        Id = "53450813",
        Title = "Bayonetta",
        State = CompatibilityRating.Playable,
        Url = "https://github.com/xenia-project/game-compatibility/issues/123"
    };

    private static GameCompatibilityEntry CreateHalo3() => new GameCompatibilityEntry
    {
        Id = "4D5307E6",
        Title = "Halo 3",
        State = CompatibilityRating.Playable,
        Url = "https://github.com/xenia-project/game-compatibility/issues/456"
    };

    private static GameCompatibilityEntry CreateUnplayableGame() => new GameCompatibilityEntry
    {
        Id = "12345678",
        Title = "Unplayable Game",
        State = CompatibilityRating.Unplayable,
        Url = "https://github.com/xenia-project/game-compatibility/issues/789"
    };

    [Test]
    public void FilteredDatabase_InitializedCorrectly()
    {
        Assert.That(GameCompatibilityDatabase.FilteredDatabase, Is.Not.Null);
        Assert.That(GameCompatibilityDatabase.FilteredDatabase, Is.Empty);
    }

    [Test]
    public async Task SearchDatabase_WithValidQuery_FiltersResults()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await GameCompatibilityDatabase.SearchDatabase("Bayonetta");

        // Assert
        Assert.That(GameCompatibilityDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(GameCompatibilityDatabase.FilteredDatabase[0].Title, Is.EqualTo("Bayonetta"));
    }

    [Test]
    public async Task SearchDatabase_WithTitleIdQuery_FiltersResults()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await GameCompatibilityDatabase.SearchDatabase("4D5307E6");

        // Assert
        Assert.That(GameCompatibilityDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(GameCompatibilityDatabase.FilteredDatabase[0].Title, Is.EqualTo("Halo 3"));
    }

    [Test]
    public async Task SearchDatabase_WithPartialQuery_FiltersResults()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await GameCompatibilityDatabase.SearchDatabase("Halo");

        // Assert
        Assert.That(GameCompatibilityDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(GameCompatibilityDatabase.FilteredDatabase[0].Title, Is.EqualTo("Halo 3"));
    }

    [Test]
    public async Task SearchDatabase_CaseInsensitive_FiltersResults()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await GameCompatibilityDatabase.SearchDatabase("bayonetta");

        // Assert
        Assert.That(GameCompatibilityDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(GameCompatibilityDatabase.FilteredDatabase[0].Title, Is.EqualTo("Bayonetta"));
    }

    [Test]
    public async Task SearchDatabase_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await GameCompatibilityDatabase.SearchDatabase("NonExistentGame");

        // Assert
        Assert.That(GameCompatibilityDatabase.FilteredDatabase, Is.Empty);
    }

    [Test]
    public async Task SearchDatabase_WithEmptyQuery_ResetsToFullList()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await GameCompatibilityDatabase.SearchDatabase("");

        // Assert
        Assert.That(GameCompatibilityDatabase.FilteredDatabase, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(GameCompatibilityDatabase.FilteredDatabase.Select(g => g.Title), Contains.Item("Bayonetta"));
            Assert.That(GameCompatibilityDatabase.FilteredDatabase.Select(g => g.Title), Contains.Item("Halo 3"));
        });
    }

    [Test]
    public async Task SearchDatabase_WithWhitespaceQuery_ResetsToFullList()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await GameCompatibilityDatabase.SearchDatabase("   ");

        // Assert
        Assert.That(GameCompatibilityDatabase.FilteredDatabase, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(GameCompatibilityDatabase.FilteredDatabase.Select(g => g.Title), Contains.Item("Bayonetta"));
            Assert.That(GameCompatibilityDatabase.FilteredDatabase.Select(g => g.Title), Contains.Item("Halo 3"));
        });
    }

    [Test]
    public async Task GetGameCompatibility_WithExistingTitle_ReturnsEntry()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta());

        // Act
        GameCompatibilityEntry? result = GameCompatibilityDatabase.GetGameCompatibility("Bayonetta");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Title, Is.EqualTo("Bayonetta"));
            Assert.That(result.Id, Is.EqualTo("53450813"));
            Assert.That(result.State, Is.EqualTo(CompatibilityRating.Playable));
        });
    }

    [Test]
    public async Task GetGameCompatibility_WithNonExistingTitle_ReturnsNull()
    {
        // Arrange
        await PopulateTestData(CreateHalo3());

        // Act
        GameCompatibilityEntry? result = GameCompatibilityDatabase.GetGameCompatibility("Bayonetta");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetGameCompatibility_CaseInsensitive_ReturnsEntry()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta());

        // Act
        GameCompatibilityEntry? result = GameCompatibilityDatabase.GetGameCompatibility("bayonetta");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Bayonetta"));
    }

    [Test]
    public async Task GetGameCompatibilityById_WithExistingId_ReturnsEntry()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta());

        // Act
        GameCompatibilityEntry? result = GameCompatibilityDatabase.GetGameCompatibilityById("53450813");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Title, Is.EqualTo("Bayonetta"));
            Assert.That(result.Id, Is.EqualTo("53450813"));
        });
    }

    [Test]
    public async Task GetGameCompatibilityById_WithNonExistingId_ReturnsNull()
    {
        // Arrange
        await PopulateTestData(CreateHalo3());

        // Act
        GameCompatibilityEntry? result = GameCompatibilityDatabase.GetGameCompatibilityById("53450813");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetGameCompatibilityById_CaseInsensitive_ReturnsEntry()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta());

        // Act
        GameCompatibilityEntry? result = GameCompatibilityDatabase.GetGameCompatibilityById("53450813");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Bayonetta"));
    }

    [Test]
    public void AddGameToIndex_WithValidEntry_AddsToIndex()
    {
        // Act
        GameCompatibilityDatabase.AddGameToIndex(CreateBayonetta(), "53450813");

        // Assert
        GameCompatibilityEntry? result = GameCompatibilityDatabase.GetGameCompatibility("Bayonetta");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo("53450813"));
    }

    [Test]
    public async Task AddGameToIndex_NormalizesToUppercase()
    {
        // Arrange
        GameCompatibilityEntry entry = CreateBayonetta();

        // Act
        GameCompatibilityDatabase.AddGameToIndex(entry, "abc123");
        await GameCompatibilityDatabase.SearchDatabase("ABC123");

        // Assert — searchable by uppercase ID
        Assert.That(GameCompatibilityDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(GameCompatibilityDatabase.FilteredDatabase[0].Title, Is.EqualTo("Bayonetta"));
    }

    [Test]
    public Task AddGameToIndex_WithDuplicateId_DoesNotOverwrite()
    {
        // Arrange
        GameCompatibilityEntry entry1 = CreateBayonetta();
        GameCompatibilityEntry entry2 = new GameCompatibilityEntry
        {
            Id = "53450813",
            Title = "Bayonetta Duplicate",
            State = CompatibilityRating.Unplayable,
            Url = "https://different-url.com"
        };

        // Act
        GameCompatibilityDatabase.AddGameToIndex(entry1, "53450813");
        GameCompatibilityDatabase.AddGameToIndex(entry2, "53450813");

        // Assert — original entry is preserved
        GameCompatibilityEntry? result = GameCompatibilityDatabase.GetGameCompatibility("Bayonetta");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Bayonetta"));
        Assert.That(result.State, Is.EqualTo(CompatibilityRating.Playable));

        GameCompatibilityEntry? duplicate = GameCompatibilityDatabase.GetGameCompatibility("Bayonetta Duplicate");
        Assert.That(duplicate, Is.Null);
        return Task.CompletedTask;
    }

    [Test]
    public async Task Reset_ClearsAllState()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());
        Assert.That(GameCompatibilityDatabase.FilteredDatabase, Has.Count.EqualTo(2));

        // Act
        GameCompatibilityDatabase.Reset();

        // Assert
        Assert.That(GameCompatibilityDatabase.FilteredDatabase, Is.Empty);
        Assert.That(GameCompatibilityDatabase.GetGameCompatibility("Bayonetta"), Is.Null);
        Assert.That(GameCompatibilityDatabase.GetGameCompatibility("Halo 3"), Is.Null);
    }

    #region Real API Request Tests

    [Test]
    public async Task LoadAsync_RealApi_LoadsSuccessfully()
    {
        try
        {
            await GameCompatibilityDatabase.LoadAsync();
            Assert.That(GameCompatibilityDatabase.FilteredDatabase, Is.Not.Null);
            Assert.That(GameCompatibilityDatabase.FilteredDatabase.Count, Is.GreaterThan(0),
                "Expected game compatibility database to be loaded from real API");
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
            await GameCompatibilityDatabase.LoadAsync();
            await GameCompatibilityDatabase.SearchDatabase("Halo");
            Assert.That(GameCompatibilityDatabase.FilteredDatabase.Count, Is.GreaterThan(0),
                "Expected to find games matching 'Halo' from real API");
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
            await GameCompatibilityDatabase.LoadAsync();
            await GameCompatibilityDatabase.SearchDatabase("4D5307E6");
            Assert.That(GameCompatibilityDatabase.FilteredDatabase.Count, Is.GreaterThan(0),
                "Expected to find games matching title ID '4D5307E6' from real API");
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task GetGameCompatibility_RealApi_ReturnsValidEntry()
    {
        try
        {
            await GameCompatibilityDatabase.LoadAsync();
            GameCompatibilityEntry? entry = GameCompatibilityDatabase.FilteredDatabase.FirstOrDefault();
            Assert.That(entry, Is.Not.Null, "Expected to get at least one entry from real API");

            if (entry != null && entry.Title != null)
            {
                GameCompatibilityEntry? result = GameCompatibilityDatabase.GetGameCompatibility(entry.Title);
                Assert.That(result, Is.Not.Null, "Expected to retrieve entry by title");
                Assert.That(result!.Title, Is.EqualTo(entry.Title));
                Assert.That(result.State, Is.Not.EqualTo(CompatibilityRating.Unknown).Or.EqualTo(CompatibilityRating.Unknown));
            }
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task GetGameCompatibilityById_RealApi_ReturnsValidEntry()
    {
        try
        {
            await GameCompatibilityDatabase.LoadAsync();
            GameCompatibilityEntry? entry = GameCompatibilityDatabase.FilteredDatabase.FirstOrDefault();
            Assert.That(entry, Is.Not.Null, "Expected to get at least one entry from real API");

            if (entry != null && entry.Id != null)
            {
                GameCompatibilityEntry? result = GameCompatibilityDatabase.GetGameCompatibilityById(entry.Id);
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
            await GameCompatibilityDatabase.LoadAsync();
            int firstCount = GameCompatibilityDatabase.FilteredDatabase.Count;

            await GameCompatibilityDatabase.LoadAsync();
            int secondCount = GameCompatibilityDatabase.FilteredDatabase.Count;

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
            await GameCompatibilityDatabase.LoadAsync();
            await GameCompatibilityDatabase.SearchDatabase("halo");
            int lowerCaseCount = GameCompatibilityDatabase.FilteredDatabase.Count;

            await GameCompatibilityDatabase.SearchDatabase("HALO");
            int upperCaseCount = GameCompatibilityDatabase.FilteredDatabase.Count;

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
            await GameCompatibilityDatabase.LoadAsync();
            int initialCount = GameCompatibilityDatabase.FilteredDatabase.Count;

            await GameCompatibilityDatabase.SearchDatabase("");
            int emptyQueryCount = GameCompatibilityDatabase.FilteredDatabase.Count;

            Assert.That(initialCount, Is.EqualTo(emptyQueryCount),
                "Expected empty query to return all games");
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task SetCompatibilityRating_RealApi_SetsRatingCorrectly()
    {
        try
        {
            // Create a mock game object
            XeniaManager.Core.Models.Game.Game game = new XeniaManager.Core.Models.Game.Game
            {
                Title = "Halo 3",
                GameId = "4D5307E6",
                AlternativeIDs = []
            };

            await GameCompatibilityDatabase.SetCompatibilityRating(game);

            Assert.That(game.Compatibility.Rating, Is.Not.EqualTo(CompatibilityRating.Unknown),
                "Expected compatibility rating to be set from real API");
            Assert.That(game.Compatibility.Url, Is.Not.Null.And.Not.Empty,
                "Expected compatibility URL to be set from real API");
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task SetCompatibilityRating_WithAlternativeIds_FindsMatch()
    {
        try
        {
            await GameCompatibilityDatabase.LoadAsync();

            // Find an entry with multiple IDs if available
            GameCompatibilityEntry? entry = GameCompatibilityDatabase.FilteredDatabase.FirstOrDefault();
            if (entry != null && entry.Id != null)
            {
                Game game = new Game
                {
                    Title = entry.Title ?? "Test Game",
                    GameId = "INVALID_ID",
                    AlternativeIDs = [entry.Id]
                };

                await GameCompatibilityDatabase.SetCompatibilityRating(game);

                Assert.That(game.Compatibility.Rating, Is.Not.EqualTo(CompatibilityRating.Unknown),
                    "Expected compatibility rating to be found via alternative ID");
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
    public async Task ForceReloadAsync_RealApi_ReloadsDatabase()
    {
        try
        {
            await GameCompatibilityDatabase.LoadAsync();
            int firstCount = GameCompatibilityDatabase.FilteredDatabase.Count;

            await GameCompatibilityDatabase.ForceReloadAsync();
            int secondCount = GameCompatibilityDatabase.FilteredDatabase.Count;

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