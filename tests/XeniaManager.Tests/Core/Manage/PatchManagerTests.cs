using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;
using XeniaManager.Files;
using XeniaManager.Files.Models.Patches;

namespace XeniaManager.Tests.Core.Manage;

[TestFixture]
public class PatchManagerTests
{
    private string _originalBaseDirectory = string.Empty;
    private string _temporaryBaseDirectory = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _originalBaseDirectory = AppPathResolver.BaseDirectory();
        _temporaryBaseDirectory = Path.Combine(Path.GetTempPath(), $"XeniaManager_PatchManagerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_temporaryBaseDirectory);
        AppPathResolver.SetBaseDirectory(_temporaryBaseDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        PatchManager.RestorePatches();
        AppPathResolver.SetBaseDirectory(_originalBaseDirectory);
        if (Directory.Exists(_temporaryBaseDirectory))
        {
            Directory.Delete(_temporaryBaseDirectory, true);
        }
    }

    [Test]
    public void DisablePatches_WhenNoPatchIsTracked_DoesNotDisableMatchingPatchFile()
    {
        string patchPath = CreatePatchFile("545107D1 - Saints Row.patch.toml");
        Game game = CreateGame();

        PatchManager.DisablePatches(game, XeniaVersion.Canary);

        Assert.That(File.Exists(patchPath), Is.True,
            "A patch file must stay enabled when the library has no tracked patch to prefer");
        Assert.That(File.Exists(patchPath + ".disabled"), Is.False);
    }

    [Test]
    public async Task DisablePatchesAsync_WhenNoPatchIsTracked_DoesNotDisableMatchingPatchFile()
    {
        string patchPath = CreatePatchFile("545107D1 - Saints Row.patch.toml");
        Game game = CreateGame();

        await PatchManager.DisablePatchesAsync(game, XeniaVersion.Canary);

        Assert.That(File.Exists(patchPath), Is.True,
            "A patch file must stay enabled when the library has no tracked patch to prefer");
        Assert.That(File.Exists(patchPath + ".disabled"), Is.False);
    }

    [Test]
    public void DisablePatches_WhenTrackedPatchFileIsMissing_DoesNotDisableMatchingPatchFile()
    {
        string patchPath = CreatePatchFile("545107D1 - Saints Row.patch.toml");
        Game game = CreateGame(Path.Combine(GetPatchDirectory(), "545107D1 - Missing.patch.toml"));

        PatchManager.DisablePatches(game, XeniaVersion.Canary);

        Assert.That(File.Exists(patchPath), Is.True,
            "A missing tracked file must not cause the game's patched file to be disabled");
        Assert.That(File.Exists(patchPath + ".disabled"), Is.False);
    }

    [Test]
    public void DisablePatches_WithTrackedPatch_KeepsActivePatchAndDisablesOtherMatchingPatches()
    {
        string activePatchPath = CreatePatchFile("545107D1 - Saints Row.patch.toml");
        string otherPatchPath = CreatePatchFile("545107D1 - Saints Row (TU1).patch.toml");
        string unrelatedPatchPath = CreatePatchFile("4D5307DF - Blue Dragon.patch.toml");
        Game game = CreateGame(activePatchPath);

        PatchManager.DisablePatches(game, XeniaVersion.Canary);

        Assert.That(File.Exists(activePatchPath), Is.True, "The active patch must stay enabled");
        Assert.That(File.Exists(otherPatchPath), Is.False, "Other patches for the same title must be disabled");
        Assert.That(File.Exists(otherPatchPath + ".disabled"), Is.True);
        Assert.That(File.Exists(unrelatedPatchPath), Is.True, "Patches for other titles must not be touched");
    }

    [Test]
    public async Task AddAdditionalPatches_CopiesAllPatchAndCommandMetadata()
    {
        string currentPatchPath = CreatePatchFile("545107D1 - Saints Row.patch.toml");
        string sourcePatchPath = WritePatchFile("545107D1 - Saints Row Extra.patch.toml", """
                                                                                          title_name = "Saints Row"
                                                                                          title_id = "545107D1"
                                                                                          hash = "0123456789ABCDEF"

                                                                                          [[patch]] # added header
                                                                                              name = "Added Patch"
                                                                                              note = "Keep this note"
                                                                                              desc = "Keep this description"
                                                                                              author = "TestAuthor" # author comment
                                                                                              is_enabled = false

                                                                                              [[patch.array]] # type comment
                                                                                                  address = 0x82000000 # address comment
                                                                                                  value = "abc" # value comment
                                                                                          """);

        PatchFile currentPatchFile = PatchFile.Load(currentPatchPath);
        List<string> added = await PatchManager.AddAdditionalPatchesAsync(CreateGame(currentPatchPath), currentPatchFile, sourcePatchPath);

        Assert.That(added, Is.EqualTo(new[]
        {
            "Added Patch"
        }));
        PatchEntry patch = currentPatchFile.Patches.Single(entry => entry.Name == "Added Patch");
        Assert.That(patch.Note, Is.EqualTo("Keep this note"));
        Assert.That(patch.Description, Is.EqualTo("Keep this description"));
        Assert.That(patch.HeaderComment, Is.EqualTo("added header"));
        Assert.That(patch.AuthorLineSuffix, Is.EqualTo("\"TestAuthor\" # author comment"));

        PatchCommand command = patch.Commands.Single();
        Assert.That(command.TypeComment, Is.EqualTo("type comment"));
        Assert.That(command.AddressComment, Is.EqualTo("address comment"));
        Assert.That(command.ValueComment, Is.EqualTo("value comment"));
        Assert.That(command.UseArrayPrefix, Is.False);
        Assert.That(command.Value, Is.EqualTo(new byte[]
        {
            0xAB, 0x0C
        }));
    }

    private static string GetPatchDirectory() =>
        AppPathResolver.GetFullPath(XeniaVersionInfo.GetXeniaVersionInfo(XeniaVersion.Canary).PatchFolderLocation);

    private static string CreatePatchFile(string fileName) =>
        WritePatchFile(fileName,
            "title_name = \"Saints Row\"\ntitle_id = \"545107D1\"\nhash = \"0123456789ABCDEF\"\n");

    private static string WritePatchFile(string fileName, string content)
    {
        string patchDirectory = GetPatchDirectory();
        Directory.CreateDirectory(patchDirectory);
        string patchPath = Path.Combine(patchDirectory, fileName);
        File.WriteAllText(patchPath, content);
        return patchPath;
    }

    private static Game CreateGame(string? trackedPatchPath = null)
    {
        Game game = new Game
        {
            GameId = "545107D1",
            Title = "Saints Row",
            XeniaVersion = XeniaVersion.Canary
        };

        if (trackedPatchPath != null)
        {
            game.FileLocations.Patch = trackedPatchPath;
        }

        return game;
    }
}