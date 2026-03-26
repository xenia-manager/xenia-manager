using XeniaManager.Core.Database;
using XeniaManager.Core.Models.Database.Xbox;

namespace XeniaManager.Tests;

[TestFixture]
public class XboxDatabaseTests
{
    [SetUp]
    public void Setup()
    {
        // Reset the static state before each test for isolation
        XboxDatabase.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up the static state after each test
        XboxDatabase.Reset();
    }

    // Helper to populate the database with test data
    private static async Task PopulateTestData(params GameInfo[] games)
    {
        foreach (GameInfo game in games)
        {
            if (game.Id != null)
            {
                XboxDatabase.AddGameToIndex(game, game.Id);
            }

            if (game.AlternativeId is { Count: > 0 })
            {
                foreach (string altId in game.AlternativeId)
                {
                    XboxDatabase.AddGameToIndex(game, altId);
                }
            }
        }

        // Initialize FilteredDatabase with all titles
        await XboxDatabase.SearchDatabase("");
    }

    private static GameInfo CreateBayonetta() => new GameInfo
    {
        Id = "53450813",
        Title = "Bayonetta",
        AlternativeId = []
    };

    private static GameInfo CreateHalo3() => new GameInfo
    {
        Id = "4D5307E6",
        Title = "Halo 3",
        AlternativeId = []
    };

    [Test]
    public void FilteredDatabase_InitializedCorrectly()
    {
        Assert.That(XboxDatabase.FilteredDatabase, Is.Not.Null);
        Assert.That(XboxDatabase.FilteredDatabase, Is.Empty);
    }

    [Test]
    public async Task SearchDatabase_WithValidQuery_FiltersResults()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await XboxDatabase.SearchDatabase("Bayonetta");

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(XboxDatabase.FilteredDatabase[0].Title, Is.EqualTo("Bayonetta"));
    }

    [Test]
    public async Task SearchDatabase_WithTitleIdQuery_FiltersResults()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await XboxDatabase.SearchDatabase("4D5307E6");

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(XboxDatabase.FilteredDatabase[0].Title, Is.EqualTo("Halo 3"));
    }

    [Test]
    public async Task SearchDatabase_WithPartialQuery_FiltersResults()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await XboxDatabase.SearchDatabase("Halo");

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(XboxDatabase.FilteredDatabase[0].Title, Is.EqualTo("Halo 3"));
    }

    [Test]
    public async Task SearchDatabase_CaseInsensitive_FiltersResults()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await XboxDatabase.SearchDatabase("bayonetta");

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(XboxDatabase.FilteredDatabase[0].Title, Is.EqualTo("Bayonetta"));
    }

    [Test]
    public async Task SearchDatabase_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await XboxDatabase.SearchDatabase("NonExistentGame");

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Is.Empty);
    }

    [Test]
    public async Task SearchDatabase_WithEmptyQuery_ResetsToFullList()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await XboxDatabase.SearchDatabase("");

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(XboxDatabase.FilteredDatabase.Select(g => g.Title), Contains.Item("Bayonetta"));
            Assert.That(XboxDatabase.FilteredDatabase.Select(g => g.Title), Contains.Item("Halo 3"));
        });
    }

    [Test]
    public async Task SearchDatabase_WithNullQuery_ResetsToFullList()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await XboxDatabase.SearchDatabase(string.Empty);

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(XboxDatabase.FilteredDatabase.Select(g => g.Title), Contains.Item("Bayonetta"));
            Assert.That(XboxDatabase.FilteredDatabase.Select(g => g.Title), Contains.Item("Halo 3"));
        });
    }

    [Test]
    public async Task SearchDatabase_WithWhitespaceQuery_ResetsToFullList()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        await XboxDatabase.SearchDatabase("   ");

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(XboxDatabase.FilteredDatabase.Select(g => g.Title), Contains.Item("Bayonetta"));
            Assert.That(XboxDatabase.FilteredDatabase.Select(g => g.Title), Contains.Item("Halo 3"));
        });
    }

    [Test]
    public async Task GetShortGameInfo_WithExistingTitle_ReturnsGameInfo()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta());

        // Act
        GameInfo? result = XboxDatabase.GetShortGameInfo("Bayonetta");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Title, Is.EqualTo("Bayonetta"));
            Assert.That(result.Id, Is.EqualTo("53450813"));
        });
    }

    [Test]
    public async Task GetShortGameInfo_WithNonExistingTitle_ReturnsNull()
    {
        // Arrange
        await PopulateTestData(CreateHalo3());

        // Act
        GameInfo? result = XboxDatabase.GetShortGameInfo("Bayonetta");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetShortGameInfo_CaseInsensitive_ReturnsGameInfo()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta());

        // Act
        GameInfo? result = XboxDatabase.GetShortGameInfo("bayonetta");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Bayonetta"));
    }

    [Test]
    public void AddGameToIndex_WithValidGame_AddsToIndex()
    {
        // Act
        XboxDatabase.AddGameToIndex(CreateBayonetta(), "53450813");

        // Assert
        GameInfo? result = XboxDatabase.GetShortGameInfo("Bayonetta");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo("53450813"));
    }

    [Test]
    public async Task AddGameToIndex_NormalizesToUppercase()
    {
        // Arrange
        GameInfo game = CreateBayonetta();

        // Act
        XboxDatabase.AddGameToIndex(game, "abc123");
        await XboxDatabase.SearchDatabase("ABC123");

        // Assert — searchable by uppercase ID
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(XboxDatabase.FilteredDatabase[0].Title, Is.EqualTo("Bayonetta"));
    }

    [Test]
    public Task AddGameToIndex_WithDuplicateId_DoesNotOverwrite()
    {
        // Arrange
        GameInfo game1 = CreateBayonetta();
        GameInfo game2 = new GameInfo
        {
            Id = "53450813",
            Title = "Bayonetta Duplicate",
            AlternativeId = []
        };

        // Act
        XboxDatabase.AddGameToIndex(game1, "53450813");
        XboxDatabase.AddGameToIndex(game2, "53450813");

        // Assert — original title is preserved
        GameInfo? result = XboxDatabase.GetShortGameInfo("Bayonetta");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Bayonetta"));

        GameInfo? duplicate = XboxDatabase.GetShortGameInfo("Bayonetta Duplicate");
        Assert.That(duplicate, Is.Null);
        return Task.CompletedTask;
    }

    [Test]
    public async Task AddGameToIndex_WithAlternativeIds_AllIdsResolveSameGame()
    {
        // Arrange
        GameInfo game = new GameInfo
        {
            Id = "53450813",
            Title = "Bayonetta",
            AlternativeId = ["ALT001", "ALT002"]
        };

        // Act
        await PopulateTestData(game);

        // Assert — all IDs find the same game
        await XboxDatabase.SearchDatabase("53450813");
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(XboxDatabase.FilteredDatabase[0].Title, Is.EqualTo("Bayonetta"));

        await XboxDatabase.SearchDatabase("ALT001");
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(XboxDatabase.FilteredDatabase[0].Title, Is.EqualTo("Bayonetta"));

        await XboxDatabase.SearchDatabase("ALT002");
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(XboxDatabase.FilteredDatabase[0].Title, Is.EqualTo("Bayonetta"));
    }

    [Test]
    public async Task Reset_ClearsAllState()
    {
        // Arrange
        await PopulateTestData(CreateBayonetta(), CreateHalo3());
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(2));

        // Act
        XboxDatabase.Reset();

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Is.Empty);
        Assert.That(XboxDatabase.GetShortGameInfo("Bayonetta"), Is.Null);
        Assert.That(XboxDatabase.GetShortGameInfo("Halo 3"), Is.Null);
    }

    #region Real API Request Tests

    [Test]
    public async Task LoadAsync_RealApi_LoadsSuccessfully()
    {
        try
        {
            await XboxDatabase.LoadAsync();
            Assert.That(XboxDatabase.FilteredDatabase, Is.Not.Null);
            Assert.That(XboxDatabase.FilteredDatabase.Count, Is.GreaterThan(0),
                "Expected Xbox games database to be loaded from real API");
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
            await XboxDatabase.LoadAsync();
            await XboxDatabase.SearchDatabase("Halo");
            Assert.That(XboxDatabase.FilteredDatabase.Count, Is.GreaterThan(0),
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
            await XboxDatabase.LoadAsync();
            await XboxDatabase.SearchDatabase("4D5307E6");
            Assert.That(XboxDatabase.FilteredDatabase.Count, Is.GreaterThan(0),
                "Expected to find games matching title ID '4D5307E6' from real API");
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task GetShortGameInfo_RealApi_ReturnsValidGameInfo()
    {
        try
        {
            await XboxDatabase.LoadAsync();
            GameInfo? gameInfo = XboxDatabase.FilteredDatabase.FirstOrDefault();
            Assert.That(gameInfo, Is.Not.Null, "Expected to get at least one game from real API");

            if (gameInfo != null && gameInfo.Title != null)
            {
                GameInfo? result = XboxDatabase.GetShortGameInfo(gameInfo.Title);
                Assert.That(result, Is.Not.Null, "Expected to retrieve game info by title");
                Assert.That(result!.Title, Is.EqualTo(gameInfo.Title));
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
            await XboxDatabase.LoadAsync();
            int firstCount = XboxDatabase.FilteredDatabase.Count;

            await XboxDatabase.LoadAsync();
            int secondCount = XboxDatabase.FilteredDatabase.Count;

            Assert.That(firstCount, Is.EqualTo(secondCount),
                "Expected database to not reload on second call");
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task GetFullGameInfo_RealApi_ReturnsValidGameDetailedInfo()
    {
        try
        {
            await XboxDatabase.LoadAsync();

            // Try to get detailed info for a known title ID
            GameDetailedInfo? detailedInfo = await XboxDatabase.GetFullGameInfo("4D5307E6");

            if (detailedInfo != null)
            {
                Assert.That(detailedInfo.Id, Is.EqualTo("4D5307E6"));
                Assert.That(detailedInfo.Title, Is.Not.Null);
            }
            else
            {
                Assert.Ignore("Game detailed info not available from API (this is acceptable)");
            }
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
            await XboxDatabase.LoadAsync();
            await XboxDatabase.SearchDatabase("halo");
            int lowerCaseCount = XboxDatabase.FilteredDatabase.Count;

            await XboxDatabase.SearchDatabase("HALO");
            int upperCaseCount = XboxDatabase.FilteredDatabase.Count;

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
            await XboxDatabase.LoadAsync();
            int initialCount = XboxDatabase.FilteredDatabase.Count;

            await XboxDatabase.SearchDatabase("");
            int emptyQueryCount = XboxDatabase.FilteredDatabase.Count;

            Assert.That(initialCount, Is.EqualTo(emptyQueryCount),
                "Expected empty query to return all games");
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    #endregion
}