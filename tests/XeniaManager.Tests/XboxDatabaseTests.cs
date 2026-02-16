using XeniaManager.Core.Database;
using XeniaManager.Core.Models.Database.Xbox;

namespace XeniaManager.Tests;

[TestFixture]
public class XboxDatabaseTests
{
    [SetUp]
    public void Setup()
    {
        // Reset static state before each test for isolation
        XboxDatabase.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up static state after each test
        XboxDatabase.Reset();
    }

    // Helper to populate the database with test data
    private static void PopulateTestData(params GameInfo[] games)
    {
        foreach (var game in games)
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
        XboxDatabase.SearchDatabase("");
    }

    private static GameInfo CreateBayonetta() => new()
    {
        Id = "53450813",
        Title = "Bayonetta",
        AlternativeId = []
    };

    private static GameInfo CreateHalo3() => new()
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
    public void SearchDatabase_WithValidQuery_FiltersResults()
    {
        // Arrange
        PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        XboxDatabase.SearchDatabase("Bayonetta");

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(XboxDatabase.FilteredDatabase[0], Is.EqualTo("Bayonetta"));
    }

    [Test]
    public void SearchDatabase_WithTitleIdQuery_FiltersResults()
    {
        // Arrange
        PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        XboxDatabase.SearchDatabase("4D5307E6");

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(XboxDatabase.FilteredDatabase[0], Is.EqualTo("Halo 3"));
    }

    [Test]
    public void SearchDatabase_WithPartialQuery_FiltersResults()
    {
        // Arrange
        PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        XboxDatabase.SearchDatabase("Halo");

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(XboxDatabase.FilteredDatabase[0], Is.EqualTo("Halo 3"));
    }

    [Test]
    public void SearchDatabase_CaseInsensitive_FiltersResults()
    {
        // Arrange
        PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        XboxDatabase.SearchDatabase("bayonetta");

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(XboxDatabase.FilteredDatabase[0], Is.EqualTo("Bayonetta"));
    }

    [Test]
    public void SearchDatabase_WithNoMatches_ReturnsEmptyList()
    {
        // Arrange
        PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        XboxDatabase.SearchDatabase("NonExistentGame");

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Is.Empty);
    }

    [Test]
    public void SearchDatabase_WithEmptyQuery_ResetsToFullList()
    {
        // Arrange
        PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        XboxDatabase.SearchDatabase("");

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(XboxDatabase.FilteredDatabase, Contains.Item("Bayonetta"));
            Assert.That(XboxDatabase.FilteredDatabase, Contains.Item("Halo 3"));
        });
    }

    [Test]
    public void SearchDatabase_WithNullQuery_ResetsToFullList()
    {
        // Arrange
        PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        XboxDatabase.SearchDatabase(string.Empty);

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(XboxDatabase.FilteredDatabase, Contains.Item("Bayonetta"));
            Assert.That(XboxDatabase.FilteredDatabase, Contains.Item("Halo 3"));
        });
    }

    [Test]
    public void SearchDatabase_WithWhitespaceQuery_ResetsToFullList()
    {
        // Arrange
        PopulateTestData(CreateBayonetta(), CreateHalo3());

        // Act
        XboxDatabase.SearchDatabase("   ");

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(XboxDatabase.FilteredDatabase, Contains.Item("Bayonetta"));
            Assert.That(XboxDatabase.FilteredDatabase, Contains.Item("Halo 3"));
        });
    }

    [Test]
    public void GetShortGameInfo_WithExistingTitle_ReturnsGameInfo()
    {
        // Arrange
        PopulateTestData(CreateBayonetta());

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
    public void GetShortGameInfo_WithNonExistingTitle_ReturnsNull()
    {
        // Arrange
        PopulateTestData(CreateHalo3());

        // Act
        GameInfo? result = XboxDatabase.GetShortGameInfo("Bayonetta");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetShortGameInfo_CaseInsensitive_ReturnsGameInfo()
    {
        // Arrange
        PopulateTestData(CreateBayonetta());

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
    public void AddGameToIndex_NormalizesToUppercase()
    {
        // Arrange
        GameInfo game = CreateBayonetta();

        // Act
        XboxDatabase.AddGameToIndex(game, "abc123");

        // Assert — searchable by uppercase ID
        XboxDatabase.SearchDatabase("ABC123");
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(XboxDatabase.FilteredDatabase[0], Is.EqualTo("Bayonetta"));
    }

    [Test]
    public void AddGameToIndex_WithDuplicateId_DoesNotOverwrite()
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
    }

    [Test]
    public void AddGameToIndex_WithAlternativeIds_AllIdsResolveSameGame()
    {
        // Arrange
        GameInfo game = new GameInfo
        {
            Id = "53450813",
            Title = "Bayonetta",
            AlternativeId = ["ALT001", "ALT002"]
        };

        // Act
        PopulateTestData(game);

        // Assert — all IDs find the same game
        XboxDatabase.SearchDatabase("53450813");
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(XboxDatabase.FilteredDatabase[0], Is.EqualTo("Bayonetta"));

        XboxDatabase.SearchDatabase("ALT001");
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(XboxDatabase.FilteredDatabase[0], Is.EqualTo("Bayonetta"));

        XboxDatabase.SearchDatabase("ALT002");
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(XboxDatabase.FilteredDatabase[0], Is.EqualTo("Bayonetta"));
    }

    [Test]
    public void Reset_ClearsAllState()
    {
        // Arrange
        PopulateTestData(CreateBayonetta(), CreateHalo3());
        Assert.That(XboxDatabase.FilteredDatabase, Has.Count.EqualTo(2));

        // Act
        XboxDatabase.Reset();

        // Assert
        Assert.That(XboxDatabase.FilteredDatabase, Is.Empty);
        Assert.That(XboxDatabase.GetShortGameInfo("Bayonetta"), Is.Null);
        Assert.That(XboxDatabase.GetShortGameInfo("Halo 3"), Is.Null);
    }
}