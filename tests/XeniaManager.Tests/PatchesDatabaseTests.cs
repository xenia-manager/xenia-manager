using XeniaManager.Core.Database;
using XeniaManager.Core.Models.Database.Patches;

namespace XeniaManager.Tests;

[TestFixture]
public class PatchesDatabaseTests
{
    [SetUp]
    public void Setup()
    {
        PatchesDatabase.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        PatchesDatabase.Reset();
    }

    private static async Task PopulateCanaryTestData(params PatchInfo[] patches)
    {
        foreach (PatchInfo patch in patches)
        {
            PatchesDatabase.AddPatchToIndex(patch);
        }
        await PatchesDatabase.SearchCanaryDatabase("");
    }

    private static async Task PopulateNetplayTestData(params PatchInfo[] patches)
    {
        foreach (PatchInfo patch in patches)
        {
            PatchesDatabase.AddPatchToNetplayIndex(patch);
        }
        await PatchesDatabase.SearchNetplayDatabase("");
    }

    private static async Task PopulateAllTestData(params PatchInfo[] patches)
    {
        await PopulateCanaryTestData(patches);
        await PopulateNetplayTestData(patches);
    }

    private static PatchInfo CreateCallOfDuty2Patch() => new()
    {
        Name = "415607D1 - Call of Duty 2.patch.toml",
        Sha = "e000a5895607890e9aae4ca96b5175fb92a6b2e5",
        Size = 396,
        DownloadUrl = "https://raw.githubusercontent.com/AdrianCassar/Xenia-WebServices/main/patches/415607D1%20-%20Call%20of%20Duty%202.patch.toml"
    };

    private static PatchInfo CreateHalo3Patch() => new()
    {
        Name = "4D5307E6 - Halo 3.patch.toml",
        Sha = "abc123def456789012345678901234567890abcd",
        Size = 512,
        DownloadUrl = "https://raw.githubusercontent.com/AdrianCassar/Xenia-WebServices/main/patches/4D5307E6%20-%20Halo%203.patch.toml"
    };

    [Test]
    public void FilteredDatabase_InitializedCorrectly()
    {
        Assert.That(PatchesDatabase.CanaryFilteredDatabase, Is.Not.Null);
        Assert.That(PatchesDatabase.CanaryFilteredDatabase, Is.Empty);
        Assert.That(PatchesDatabase.NetplayFilteredDatabase, Is.Not.Null);
        Assert.That(PatchesDatabase.NetplayFilteredDatabase, Is.Empty);
    }

    #region Canary Database Tests

    [Test]
    public async Task SearchCanaryDatabase_WithValidQuery_FiltersResults()
    {
        await PopulateCanaryTestData(CreateCallOfDuty2Patch(), CreateHalo3Patch());
        await PatchesDatabase.SearchCanaryDatabase("Call of Duty");
        Assert.That(PatchesDatabase.CanaryFilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(PatchesDatabase.CanaryFilteredDatabase[0].Name, Does.Contain("Call of Duty 2"));
    }

    [Test]
    public async Task SearchCanaryDatabase_WithShaQuery_FiltersResults()
    {
        await PopulateCanaryTestData(CreateCallOfDuty2Patch(), CreateHalo3Patch());
        await PatchesDatabase.SearchCanaryDatabase("e000a5895607890e9aae4ca96b5175fb92a6b2e5");
        Assert.That(PatchesDatabase.CanaryFilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(PatchesDatabase.CanaryFilteredDatabase[0].Name, Does.Contain("Call of Duty 2"));
    }

    [Test]
    public async Task SearchCanaryDatabase_WithPartialQuery_FiltersResults()
    {
        await PopulateCanaryTestData(CreateCallOfDuty2Patch(), CreateHalo3Patch());
        await PatchesDatabase.SearchCanaryDatabase("Halo");
        Assert.That(PatchesDatabase.CanaryFilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(PatchesDatabase.CanaryFilteredDatabase[0].Name, Does.Contain("Halo 3"));
    }

    [Test]
    public async Task SearchCanaryDatabase_CaseInsensitive_FiltersResults()
    {
        await PopulateCanaryTestData(CreateCallOfDuty2Patch(), CreateHalo3Patch());
        await PatchesDatabase.SearchCanaryDatabase("call of duty");
        Assert.That(PatchesDatabase.CanaryFilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(PatchesDatabase.CanaryFilteredDatabase[0].Name, Does.Contain("Call of Duty 2"));
    }

    [Test]
    public async Task SearchCanaryDatabase_WithNoMatches_ReturnsEmptyList()
    {
        await PopulateCanaryTestData(CreateCallOfDuty2Patch(), CreateHalo3Patch());
        await PatchesDatabase.SearchCanaryDatabase("NonExistentPatch");
        Assert.That(PatchesDatabase.CanaryFilteredDatabase, Is.Empty);
    }

    [Test]
    public async Task SearchCanaryDatabase_WithEmptyQuery_ResetsToFullList()
    {
        await PopulateCanaryTestData(CreateCallOfDuty2Patch(), CreateHalo3Patch());
        await PatchesDatabase.SearchCanaryDatabase("");
        Assert.That(PatchesDatabase.CanaryFilteredDatabase, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(PatchesDatabase.CanaryFilteredDatabase.Select(p => p.Name), Contains.Item("415607D1 - Call of Duty 2.patch.toml"));
            Assert.That(PatchesDatabase.CanaryFilteredDatabase.Select(p => p.Name), Contains.Item("4D5307E6 - Halo 3.patch.toml"));
        });
    }

    [Test]
    public async Task SearchCanaryDatabase_WithWhitespaceQuery_ResetsToFullList()
    {
        await PopulateCanaryTestData(CreateCallOfDuty2Patch(), CreateHalo3Patch());
        await PatchesDatabase.SearchCanaryDatabase("   ");
        Assert.That(PatchesDatabase.CanaryFilteredDatabase, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(PatchesDatabase.CanaryFilteredDatabase.Select(p => p.Name), Contains.Item("415607D1 - Call of Duty 2.patch.toml"));
            Assert.That(PatchesDatabase.CanaryFilteredDatabase.Select(p => p.Name), Contains.Item("4D5307E6 - Halo 3.patch.toml"));
        });
    }

    [Test]
    public async Task GetCanaryPatchInfo_WithExistingName_ReturnsPatchInfo()
    {
        await PopulateCanaryTestData(CreateCallOfDuty2Patch());
        PatchInfo? result = PatchesDatabase.GetCanaryPatchInfo("415607D1 - Call of Duty 2.patch.toml");
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Name, Does.Contain("Call of Duty 2"));
            Assert.That(result.Sha, Is.EqualTo("e000a5895607890e9aae4ca96b5175fb92a6b2e5"));
            Assert.That(result.Size, Is.EqualTo(396));
        });
    }

    [Test]
    public async Task GetCanaryPatchInfo_WithNonExistingName_ReturnsNull()
    {
        await PopulateCanaryTestData(CreateHalo3Patch());
        PatchInfo? result = PatchesDatabase.GetCanaryPatchInfo("415607D1 - Call of Duty 2.patch.toml");
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetCanaryPatchInfo_CaseInsensitive_ReturnsPatchInfo()
    {
        await PopulateCanaryTestData(CreateCallOfDuty2Patch());
        PatchInfo? result = PatchesDatabase.GetCanaryPatchInfo("415607d1 - call of duty 2.patch.toml");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Does.Contain("Call of Duty 2"));
    }

    [Test]
    public void GetCanaryPatchInfo_WithNullName_ReturnsNull()
    {
        PatchesDatabase.Reset();
        PatchInfo? result = PatchesDatabase.GetCanaryPatchInfo(null);
        Assert.That(result, Is.Null);
    }

    #endregion

    #region Netplay Database Tests

    [Test]
    public async Task SearchNetplayDatabase_WithValidQuery_FiltersResults()
    {
        await PopulateNetplayTestData(CreateCallOfDuty2Patch(), CreateHalo3Patch());
        await PatchesDatabase.SearchNetplayDatabase("Call of Duty");
        Assert.That(PatchesDatabase.NetplayFilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(PatchesDatabase.NetplayFilteredDatabase[0].Name, Does.Contain("Call of Duty 2"));
    }

    [Test]
    public async Task SearchNetplayDatabase_WithShaQuery_FiltersResults()
    {
        await PopulateNetplayTestData(CreateCallOfDuty2Patch(), CreateHalo3Patch());
        await PatchesDatabase.SearchNetplayDatabase("e000a5895607890e9aae4ca96b5175fb92a6b2e5");
        Assert.That(PatchesDatabase.NetplayFilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(PatchesDatabase.NetplayFilteredDatabase[0].Name, Does.Contain("Call of Duty 2"));
    }

    [Test]
    public async Task SearchNetplayDatabase_WithPartialQuery_FiltersResults()
    {
        await PopulateNetplayTestData(CreateCallOfDuty2Patch(), CreateHalo3Patch());
        await PatchesDatabase.SearchNetplayDatabase("Halo");
        Assert.That(PatchesDatabase.NetplayFilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(PatchesDatabase.NetplayFilteredDatabase[0].Name, Does.Contain("Halo 3"));
    }

    [Test]
    public async Task SearchNetplayDatabase_CaseInsensitive_FiltersResults()
    {
        await PopulateNetplayTestData(CreateCallOfDuty2Patch(), CreateHalo3Patch());
        await PatchesDatabase.SearchNetplayDatabase("call of duty");
        Assert.That(PatchesDatabase.NetplayFilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(PatchesDatabase.NetplayFilteredDatabase[0].Name, Does.Contain("Call of Duty 2"));
    }

    [Test]
    public async Task SearchNetplayDatabase_WithNoMatches_ReturnsEmptyList()
    {
        await PopulateNetplayTestData(CreateCallOfDuty2Patch(), CreateHalo3Patch());
        await PatchesDatabase.SearchNetplayDatabase("NonExistentPatch");
        Assert.That(PatchesDatabase.NetplayFilteredDatabase, Is.Empty);
    }

    [Test]
    public async Task SearchNetplayDatabase_WithEmptyQuery_ResetsToFullList()
    {
        await PopulateNetplayTestData(CreateCallOfDuty2Patch(), CreateHalo3Patch());
        await PatchesDatabase.SearchNetplayDatabase("");
        Assert.That(PatchesDatabase.NetplayFilteredDatabase, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(PatchesDatabase.NetplayFilteredDatabase.Select(p => p.Name), Contains.Item("415607D1 - Call of Duty 2.patch.toml"));
            Assert.That(PatchesDatabase.NetplayFilteredDatabase.Select(p => p.Name), Contains.Item("4D5307E6 - Halo 3.patch.toml"));
        });
    }

    [Test]
    public async Task SearchNetplayDatabase_WithWhitespaceQuery_ResetsToFullList()
    {
        await PopulateNetplayTestData(CreateCallOfDuty2Patch(), CreateHalo3Patch());
        await PatchesDatabase.SearchNetplayDatabase("   ");
        Assert.That(PatchesDatabase.NetplayFilteredDatabase, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(PatchesDatabase.NetplayFilteredDatabase.Select(p => p.Name), Contains.Item("415607D1 - Call of Duty 2.patch.toml"));
            Assert.That(PatchesDatabase.NetplayFilteredDatabase.Select(p => p.Name), Contains.Item("4D5307E6 - Halo 3.patch.toml"));
        });
    }

    [Test]
    public async Task GetNetplayPatchInfo_WithExistingName_ReturnsPatchInfo()
    {
        await PopulateNetplayTestData(CreateCallOfDuty2Patch());
        PatchInfo? result = PatchesDatabase.GetNetplayPatchInfo("415607D1 - Call of Duty 2.patch.toml");
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Name, Does.Contain("Call of Duty 2"));
            Assert.That(result.Sha, Is.EqualTo("e000a5895607890e9aae4ca96b5175fb92a6b2e5"));
            Assert.That(result.Size, Is.EqualTo(396));
        });
    }

    [Test]
    public async Task GetNetplayPatchInfo_WithNonExistingName_ReturnsNull()
    {
        await PopulateNetplayTestData(CreateHalo3Patch());
        PatchInfo? result = PatchesDatabase.GetNetplayPatchInfo("415607D1 - Call of Duty 2.patch.toml");
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetNetplayPatchInfo_CaseInsensitive_ReturnsPatchInfo()
    {
        await PopulateNetplayTestData(CreateCallOfDuty2Patch());
        PatchInfo? result = PatchesDatabase.GetNetplayPatchInfo("415607d1 - call of duty 2.patch.toml");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Does.Contain("Call of Duty 2"));
    }

    [Test]
    public void GetNetplayPatchInfo_WithNullName_ReturnsNull()
    {
        PatchesDatabase.Reset();
        PatchInfo? result = PatchesDatabase.GetNetplayPatchInfo(null);
        Assert.That(result, Is.Null);
    }

    #endregion

    #region AddPatchToIndex Tests

    [Test]
    public void AddPatchToIndex_WithValidPatch_AddsToIndex()
    {
        PatchesDatabase.AddPatchToIndex(CreateCallOfDuty2Patch());
        PatchInfo? result = PatchesDatabase.GetCanaryPatchInfo("415607D1 - Call of Duty 2.patch.toml");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Sha, Is.EqualTo("e000a5895607890e9aae4ca96b5175fb92a6b2e5"));
    }

    [Test]
    public async Task AddPatchToIndex_NormalizesToUppercase()
    {
        PatchInfo patch = CreateCallOfDuty2Patch();
        PatchesDatabase.AddPatchToIndex(patch);
        await PatchesDatabase.SearchCanaryDatabase("415607D1");
        Assert.That(PatchesDatabase.CanaryFilteredDatabase, Has.Count.EqualTo(1));
        Assert.That(PatchesDatabase.CanaryFilteredDatabase[0].Name, Does.Contain("Call of Duty 2"));
    }

    [Test]
    public void AddPatchToIndex_WithDuplicateName_DoesNotOverwrite()
    {
        PatchInfo patch1 = CreateCallOfDuty2Patch();
        PatchInfo patch2 = new PatchInfo
        {
            Name = "415607D1 - Call of Duty 2.patch.toml",
            Sha = "different_sha",
            Size = 1000,
            DownloadUrl = "https://different-url.com/patch.toml"
        };

        PatchesDatabase.AddPatchToIndex(patch1);
        PatchesDatabase.AddPatchToIndex(patch2);

        PatchInfo? result = PatchesDatabase.GetCanaryPatchInfo("415607D1 - Call of Duty 2.patch.toml");
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Sha, Is.EqualTo("e000a5895607890e9aae4ca96b5175fb92a6b2e5"));
        Assert.That(result.Size, Is.EqualTo(396));
    }

    [Test]
    public void AddPatchToIndex_WithNullName_DoesNotAdd()
    {
        PatchInfo patch = new PatchInfo
        {
            Name = null,
            Sha = "abc123",
            Size = 100,
            DownloadUrl = "https://example.com/patch.toml"
        };

        PatchesDatabase.AddPatchToIndex(patch);

        Assert.That(PatchesDatabase.CanaryFilteredDatabase, Is.Empty);
        Assert.That(PatchesDatabase.NetplayFilteredDatabase, Is.Empty);
    }

    #endregion

    #region Reset Tests

    [Test]
    public async Task Reset_ClearsAllState()
    {
        await PopulateAllTestData(CreateCallOfDuty2Patch(), CreateHalo3Patch());
        Assert.That(PatchesDatabase.CanaryFilteredDatabase, Has.Count.EqualTo(2));
        Assert.That(PatchesDatabase.NetplayFilteredDatabase, Has.Count.EqualTo(2));

        PatchesDatabase.Reset();

        Assert.That(PatchesDatabase.CanaryFilteredDatabase, Is.Empty);
        Assert.That(PatchesDatabase.NetplayFilteredDatabase, Is.Empty);
        Assert.That(PatchesDatabase.GetCanaryPatchInfo("415607D1 - Call of Duty 2.patch.toml"), Is.Null);
        Assert.That(PatchesDatabase.GetNetplayPatchInfo("415607D1 - Call of Duty 2.patch.toml"), Is.Null);
        Assert.That(PatchesDatabase.GetCanaryPatchInfo("4D5307E6 - Halo 3.patch.toml"), Is.Null);
        Assert.That(PatchesDatabase.GetNetplayPatchInfo("4D5307E6 - Halo 3.patch.toml"), Is.Null);
    }

    #endregion
}
