using XeniaManager.Core.Manage;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Tests.Core.Utilities;

[TestFixture]
public class ArgumentParserTests
{
    private string _tempIsoPath;
    private Game _testGame;

    [SetUp]
    public void Setup()
    {
        // Create a temporary dummy file to satisfy IsGamePathValid
        _tempIsoPath = Path.GetTempFileName();
        _testGame = new Game
        {
            Title = "TestGame",
            GameId = "test-id",
            FileLocations = new GameFiles
            {
                Game = _tempIsoPath
            }
        };
        GameManager.Games = [_testGame];
    }

    [TearDown]
    public void Teardown()
    {
        // Clean up temp file
        if (File.Exists(_tempIsoPath))
        {
            File.Delete(_tempIsoPath);
        }

        GameManager.Games = [];
    }

    [Test]
    public void GetGameFromArgs_Flag_ReturnsGame()
    {
        string[] args = ["--game", "\"TestGame\""];
        Game? result = ArgumentParser.GetGameFromArgs(args);
        Assert.IsNotNull(result);
        Assert.AreEqual(_testGame.Title, result!.Title);
    }

    [Test]
    public void GetGameFromArgs_Positional_ReturnsGame()
    {
        string[] args = ["TestGame", "--fullscreen"];
        Game? result = ArgumentParser.GetGameFromArgs(args);
        Assert.IsNotNull(result);
        Assert.AreEqual(_testGame.Title, result!.Title);
    }

    [Test]
    public void GetConfigOverridesFromArgs_Flag_ReturnsOverrides()
    {
        string[] args = ["--xenia_args", "\"--fullscreen --debug\""];
        string? result = ArgumentParser.GetConfigOverridesFromArgs(args);
        Assert.IsNotNull(result);
        Assert.AreEqual("--fullscreen --debug", result);
    }

    [Test]
    public void GetConfigOverridesFromArgs_Positional_ReturnsOverrides()
    {
        string[] args = ["TestGame", "--fullscreen", "--debug"];
        string? result = ArgumentParser.GetConfigOverridesFromArgs(args);
        Assert.IsNotNull(result);
        // Result may have trailing space; trim for assertion
        Assert.AreEqual("--fullscreen --debug", result!.Trim());
    }

    [Test]
    public void GetDiscFromArgs_Flag_ReturnsDiscNumber()
    {
        string[] args = ["--game", "\"TestGame\"", "--disc", "2"];
        int? result = ArgumentParser.GetDiscFromArgs(args);
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result);
    }

    [Test]
    public void GetDiscFromArgs_ShortAlias_ReturnsDiscNumber()
    {
        string[] args = ["-d", "3"];
        int? result = ArgumentParser.GetDiscFromArgs(args);
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result);
    }

    [Test]
    public void GetDiscFromArgs_Missing_ReturnsNull()
    {
        string[] args = ["--game", "\"TestGame\""];
        int? result = ArgumentParser.GetDiscFromArgs(args);
        Assert.IsNull(result);
    }

    [Test]
    public void GetDiscFromArgs_InvalidValue_ReturnsNull()
    {
        string[] args = ["--disc", "notanumber"];
        int? result = ArgumentParser.GetDiscFromArgs(args);
        Assert.IsNull(result);
    }

    [Test]
    public void GetDiscFromArgs_ZeroOrNegative_ReturnsNull()
    {
        string[] args = ["--disc", "0"];
        int? result = ArgumentParser.GetDiscFromArgs(args);
        Assert.IsNull(result);
    }
}