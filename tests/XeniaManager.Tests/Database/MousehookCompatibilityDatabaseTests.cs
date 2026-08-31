using XeniaManager.Database;
using XeniaManager.Database.Models.MousehookCompatibility;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.Tests.Database;

[TestFixture]
public class MousehookCompatibilityDatabaseTests
{
    [SetUp]
    public void Setup() => MousehookCompatibilityDatabase.Reset();

    [TearDown]
    public void TearDown() => MousehookCompatibilityDatabase.Reset();

    private static async Task PopulateTestData(params MousehookCompatibilityEntry[] entries)
    {
        foreach (MousehookCompatibilityEntry entry in entries)
        {
            foreach (string id in entry.Ids)
            {
                MousehookCompatibilityDatabase.AddGameToIndex(entry, id);
            }
        }

        await MousehookCompatibilityDatabase.SearchDatabase("");
    }

    private static MousehookCompatibilityEntry CreateHalo3() => new MousehookCompatibilityEntry
    {
        Ids = ["4D5307E6"],
        Title = "Halo 3",
        MouseSupport = MousehookSupportRating.Fair,
        Notes = "Test notes"
    };

    private static MousehookCompatibilityEntry CreateRedDeadRedemption() => new MousehookCompatibilityEntry
    {
        Ids = ["5454082B"],
        Title = "Red Dead Redemption",
        MouseSupport = MousehookSupportRating.Good,
        Notes = "Good support"
    };

    [Test]
    public void FilteredDatabase_InitializedCorrectly()
    {
        Assert.That(MousehookCompatibilityDatabase.FilteredDatabase, Is.Not.Null);
        Assert.That(MousehookCompatibilityDatabase.FilteredDatabase, Is.Empty);
    }

    [Test]
    public async Task SearchDatabase_WithValidQuery_FiltersResults()
    {
        await PopulateTestData(CreateHalo3(), CreateRedDeadRedemption());

        await MousehookCompatibilityDatabase.SearchDatabase("Halo");

        Assert.That(MousehookCompatibilityDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(MousehookCompatibilityDatabase.FilteredDatabase[0].Title, Is.EqualTo("Halo 3"));
    }

    [Test]
    public async Task GetGameCompatibility_WithExistingTitle_ReturnsEntry()
    {
        await PopulateTestData(CreateHalo3());

        MousehookCompatibilityEntry? result = MousehookCompatibilityDatabase.GetGameCompatibility("Halo 3");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Halo 3"));
        Assert.That(result.MouseSupport, Is.EqualTo(MousehookSupportRating.Fair));
    }

    [Test]
    public async Task GetGameCompatibilityById_WithExistingId_ReturnsEntry()
    {
        await PopulateTestData(CreateHalo3());

        MousehookCompatibilityEntry? result = MousehookCompatibilityDatabase.GetGameCompatibilityById("4D5307E6");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Halo 3"));
    }

    [Test]
    public async Task AddGameToIndex_WithArrayIds_IndexesAllIds()
    {
        MousehookCompatibilityEntry entry = new MousehookCompatibilityEntry
        {
            Ids = ["545107D1", "545107F8"],
            Title = "Saints Row 1",
            MouseSupport = MousehookSupportRating.Fair,
            Notes = ""
        };

        MousehookCompatibilityDatabase.AddGameToIndex(entry, "545107D1");
        MousehookCompatibilityDatabase.AddGameToIndex(entry, "545107F8");
        await MousehookCompatibilityDatabase.SearchDatabase("");

        Assert.That(MousehookCompatibilityDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(MousehookCompatibilityDatabase.GetGameCompatibilityById("545107D1"), Is.Not.Null);
        Assert.That(MousehookCompatibilityDatabase.GetGameCompatibilityById("545107F8"), Is.Not.Null);
    }

    [Test]
    public async Task Reset_ClearsAllState()
    {
        await PopulateTestData(CreateHalo3(), CreateRedDeadRedemption());
        Assert.That(MousehookCompatibilityDatabase.FilteredDatabase, Has.Count.EqualTo(2));

        MousehookCompatibilityDatabase.Reset();

        Assert.That(MousehookCompatibilityDatabase.FilteredDatabase, Is.Empty);
    }

    [Test]
    public async Task SetMousehookCompatibility_WithMatchingGame_SetsRating()
    {
        await PopulateTestData(CreateHalo3());

        Game game = new Game
        {
            Title = "Halo 3",
            GameId = "4D5307E6",
            AlternativeIDs = []
        };

        MousehookCompatibilityEntry? mousehookEntry = await MousehookCompatibilityDatabase.ResolveAsync(game.GameId, game.AlternativeIDs, game.Title);
        if (mousehookEntry != null)
        {
            game.Compatibility.Mousehook.Rating = mousehookEntry.MouseSupport;
            game.Compatibility.Mousehook.Notes = mousehookEntry.Notes ?? string.Empty;
        }
        else
        {
            game.Compatibility.Mousehook.Rating = MousehookSupportRating.Unknown;
            game.Compatibility.Mousehook.Notes = string.Empty;
        }

        Assert.That(game.Compatibility.Mousehook.Rating, Is.EqualTo(MousehookSupportRating.Fair));
        Assert.That(game.Compatibility.Mousehook.Notes, Is.EqualTo("Test notes"));
    }

    [Test]
    public async Task SetMousehookCompatibility_WithNoMatch_SetsUnknown()
    {
        await PopulateTestData(CreateHalo3());

        Game game = new Game
        {
            Title = "Unknown Game",
            GameId = "00000000",
            AlternativeIDs = []
        };

        MousehookCompatibilityEntry? mousehookEntry = await MousehookCompatibilityDatabase.ResolveAsync(game.GameId, game.AlternativeIDs, game.Title);
        if (mousehookEntry != null)
        {
            game.Compatibility.Mousehook.Rating = mousehookEntry.MouseSupport;
            game.Compatibility.Mousehook.Notes = mousehookEntry.Notes ?? string.Empty;
        }
        else
        {
            game.Compatibility.Mousehook.Rating = MousehookSupportRating.Unknown;
            game.Compatibility.Mousehook.Notes = string.Empty;
        }

        Assert.That(game.Compatibility.Mousehook.Rating, Is.EqualTo(MousehookSupportRating.Unknown));
        Assert.That(game.Compatibility.Mousehook.Notes, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task SetMousehookCompatibility_WithAlternativeId_FindsMatch()
    {
        await PopulateTestData(CreateHalo3());

        Game game = new Game
        {
            Title = "Halo 3",
            GameId = "INVALID_ID",
            AlternativeIDs = ["4D5307E6"]
        };

        MousehookCompatibilityEntry? mousehookEntry = await MousehookCompatibilityDatabase.ResolveAsync(game.GameId, game.AlternativeIDs, game.Title);
        if (mousehookEntry != null)
        {
            game.Compatibility.Mousehook.Rating = mousehookEntry.MouseSupport;
            game.Compatibility.Mousehook.Notes = mousehookEntry.Notes ?? string.Empty;
        }
        else
        {
            game.Compatibility.Mousehook.Rating = MousehookSupportRating.Unknown;
            game.Compatibility.Mousehook.Notes = string.Empty;
        }

        Assert.That(game.Compatibility.Mousehook.Rating, Is.EqualTo(MousehookSupportRating.Fair));
    }

    #region Real API Request Tests

    [Test]
    public async Task LoadAsync_RealApi_LoadsSuccessfully()
    {
        try
        {
            await MousehookCompatibilityDatabase.LoadAsync();
            Assert.That(MousehookCompatibilityDatabase.FilteredDatabase, Is.Not.Null);
            Assert.That(MousehookCompatibilityDatabase.FilteredDatabase.Count, Is.GreaterThan(0));
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task SetMousehookCompatibility_RealApi_SetsRatingCorrectly()
    {
        try
        {
            Game game = new Game
            {
                Title = "Halo 3",
                GameId = "4D5307E6",
                AlternativeIDs = []
            };

            MousehookCompatibilityEntry? mousehookEntry = await MousehookCompatibilityDatabase.ResolveAsync(game.GameId, game.AlternativeIDs, game.Title);
            if (mousehookEntry != null)
            {
                game.Compatibility.Mousehook.Rating = mousehookEntry.MouseSupport;
                game.Compatibility.Mousehook.Notes = mousehookEntry.Notes ?? string.Empty;
            }
            else
            {
                game.Compatibility.Mousehook.Rating = MousehookSupportRating.Unknown;
                game.Compatibility.Mousehook.Notes = string.Empty;
            }

            Assert.That(game.Compatibility.Mousehook.Rating, Is.Not.EqualTo(MousehookSupportRating.Unknown));
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    #endregion
}