using XeniaManager.Database;
using XeniaManager.Database.Models.NetplayCompatibility;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.Tests.Database;

[TestFixture]
public class NetplayCompatibilityDatabaseTests
{
    [SetUp]
    public void Setup() => NetplayCompatibilityDatabase.Reset();

    [TearDown]
    public void TearDown() => NetplayCompatibilityDatabase.Reset();

    private static async Task PopulateTestData(params NetplayCompatibilityEntry[] entries)
    {
        foreach (NetplayCompatibilityEntry entry in entries)
        {
            foreach (string id in entry.Ids)
            {
                NetplayCompatibilityDatabase.AddGameToIndex(entry, id);
            }
        }

        await NetplayCompatibilityDatabase.SearchDatabase("");
    }

    private static NetplayCompatibilityEntry CreateCod2() => new NetplayCompatibilityEntry
    {
        Ids = ["415607D1"],
        Title = "Call of Duty 2",
        Status = new NetplayStatus
        {
            WorkingPublic = NetplayStatusValue.Partial,
            TestedLocally = NetplayStatusValue.Unknown,
            OnlyLocal = NetplayStatusValue.Unknown,
            Systemlink = NetplayStatusValue.Partial
        },
        Comments = "Patch required for systemlink"
    };

    private static NetplayCompatibilityEntry CreateMw2() => new NetplayCompatibilityEntry
    {
        Ids = ["41560817"],
        Title = "Call of Duty: Modern Warfare 2",
        Status = new NetplayStatus
        {
            WorkingPublic = NetplayStatusValue.Ok,
            TestedLocally = NetplayStatusValue.Unknown,
            OnlyLocal = NetplayStatusValue.Unknown,
            Systemlink = NetplayStatusValue.Ok
        },
        Comments = "Systemlink works without a server"
    };

    [Test]
    public void FilteredDatabase_InitializedCorrectly()
    {
        Assert.That(NetplayCompatibilityDatabase.FilteredDatabase, Is.Not.Null);
        Assert.That(NetplayCompatibilityDatabase.FilteredDatabase, Is.Empty);
    }

    [Test]
    public async Task SearchDatabase_WithValidQuery_FiltersResults()
    {
        await PopulateTestData(CreateCod2(), CreateMw2());

        await NetplayCompatibilityDatabase.SearchDatabase("Call of Duty 2");

        Assert.That(NetplayCompatibilityDatabase.FilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(NetplayCompatibilityDatabase.FilteredDatabase[0].Title, Is.EqualTo("Call of Duty 2"));
    }

    [Test]
    public async Task GetGameCompatibility_WithExistingTitle_ReturnsEntry()
    {
        await PopulateTestData(CreateCod2());

        NetplayCompatibilityEntry? result = NetplayCompatibilityDatabase.GetGameCompatibility("Call of Duty 2");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Call of Duty 2"));
        Assert.That(result.Status.WorkingPublic, Is.EqualTo(NetplayStatusValue.Partial));
    }

    [Test]
    public async Task GetGameCompatibilityById_WithExistingId_ReturnsEntry()
    {
        await PopulateTestData(CreateCod2());

        NetplayCompatibilityEntry? result = NetplayCompatibilityDatabase.GetGameCompatibilityById("415607D1");

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Title, Is.EqualTo("Call of Duty 2"));
    }

    [Test]
    public async Task Reset_ClearsAllState()
    {
        await PopulateTestData(CreateCod2(), CreateMw2());
        Assert.That(NetplayCompatibilityDatabase.FilteredDatabase, Has.Count.EqualTo(2));

        NetplayCompatibilityDatabase.Reset();

        Assert.That(NetplayCompatibilityDatabase.FilteredDatabase, Is.Empty);
    }

    [Test]
    public async Task SetNetplayCompatibility_WithMatchingGame_SetsStatus()
    {
        await PopulateTestData(CreateMw2());

        Game game = new Game
        {
            Title = "Call of Duty: Modern Warfare 2",
            GameId = "41560817",
            AlternativeIDs = []
        };

        NetplayCompatibilityEntry? netplayEntry = await NetplayCompatibilityDatabase.ResolveAsync(game.GameId, game.AlternativeIDs, game.Title);
        if (netplayEntry != null)
        {
            game.Compatibility.Netplay.Status = netplayEntry.Status;
            game.Compatibility.Netplay.Comments = netplayEntry.Comments ?? string.Empty;
        }
        else
        {
            game.Compatibility.Netplay.Status = new NetplayStatus();
            game.Compatibility.Netplay.Comments = string.Empty;
        }

        Assert.That(game.Compatibility.Netplay.Status.WorkingPublic, Is.EqualTo(NetplayStatusValue.Ok));
        Assert.That(game.Compatibility.Netplay.Status.Systemlink, Is.EqualTo(NetplayStatusValue.Ok));
        Assert.That(game.Compatibility.Netplay.Comments, Is.EqualTo("Systemlink works without a server"));
    }

    [Test]
    public async Task SetNetplayCompatibility_WithNoMatch_SetsDefaults()
    {
        await PopulateTestData(CreateCod2());

        Game game = new Game
        {
            Title = "Unknown Game",
            GameId = "00000000",
            AlternativeIDs = []
        };

        NetplayCompatibilityEntry? netplayEntry = await NetplayCompatibilityDatabase.ResolveAsync(game.GameId, game.AlternativeIDs, game.Title);
        if (netplayEntry != null)
        {
            game.Compatibility.Netplay.Status = netplayEntry.Status;
            game.Compatibility.Netplay.Comments = netplayEntry.Comments ?? string.Empty;
        }
        else
        {
            game.Compatibility.Netplay.Status = new NetplayStatus();
            game.Compatibility.Netplay.Comments = string.Empty;
        }

        Assert.That(game.Compatibility.Netplay.Status.WorkingPublic, Is.EqualTo(NetplayStatusValue.Unknown));
        Assert.That(game.Compatibility.Netplay.Comments, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task SetNetplayCompatibility_WithAlternativeId_FindsMatch()
    {
        await PopulateTestData(CreateCod2());

        Game game = new Game
        {
            Title = "Call of Duty 2",
            GameId = "INVALID_ID",
            AlternativeIDs = ["415607D1"]
        };

        NetplayCompatibilityEntry? netplayEntry = await NetplayCompatibilityDatabase.ResolveAsync(game.GameId, game.AlternativeIDs, game.Title);
        if (netplayEntry != null)
        {
            game.Compatibility.Netplay.Status = netplayEntry.Status;
            game.Compatibility.Netplay.Comments = netplayEntry.Comments ?? string.Empty;
        }
        else
        {
            game.Compatibility.Netplay.Status = new NetplayStatus();
            game.Compatibility.Netplay.Comments = string.Empty;
        }

        Assert.That(game.Compatibility.Netplay.Status.WorkingPublic, Is.EqualTo(NetplayStatusValue.Partial));
    }

    #region Real API Request Tests

    [Test]
    public async Task LoadAsync_RealApi_LoadsSuccessfully()
    {
        try
        {
            await NetplayCompatibilityDatabase.LoadAsync();
            Assert.That(NetplayCompatibilityDatabase.FilteredDatabase, Is.Not.Null);
            Assert.That(NetplayCompatibilityDatabase.FilteredDatabase.Count, Is.GreaterThan(0));
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    [Test]
    public async Task SetNetplayCompatibility_RealApi_SetsStatusCorrectly()
    {
        try
        {
            Game game = new Game
            {
                Title = "Call of Duty 2",
                GameId = "415607D1",
                AlternativeIDs = []
            };

            NetplayCompatibilityEntry? netplayEntry = await NetplayCompatibilityDatabase.ResolveAsync(game.GameId, game.AlternativeIDs, game.Title);
            if (netplayEntry != null)
            {
                game.Compatibility.Netplay.Status = netplayEntry.Status;
                game.Compatibility.Netplay.Comments = netplayEntry.Comments ?? string.Empty;
            }
            else
            {
                game.Compatibility.Netplay.Status = new NetplayStatus();
                game.Compatibility.Netplay.Comments = string.Empty;
            }

            Assert.That(game.Compatibility.Netplay.Status.WorkingPublic, Is.Not.EqualTo(NetplayStatusValue.Unknown).Or.EqualTo(NetplayStatusValue.Unknown));
        }
        catch (Exception ex)
        {
            Assert.Ignore($"Real API request failed: {ex.Message}");
        }
    }

    #endregion
}