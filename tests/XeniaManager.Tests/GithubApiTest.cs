using System.Reflection;

// Imported
using NUnit.Framework.Legacy;
using XeniaManager.Core;

namespace XeniaManager.Tests;

public class GithubApiTest
{
    private FieldInfo? _githubClientField;

    [SetUp]
    public void Setup()
    {
        // Get the private static _githubClient field from the GitHub class
        _githubClientField = typeof(Github).GetField("_githubClient", BindingFlags.Static | BindingFlags.NonPublic);
        ClassicAssert.NotNull(_githubClientField, "_githubClient field not found");
    }

    [Test]
    public async Task CheckRateLimit_Tester()
    {
        bool test = await Github.IsRateLimitAvailableAsync();
        ClassicAssert.IsTrue(test, "Rate limit reached");
    }

    [Test]
    public async Task GrabLatestRelease_Test()
    {
        string releaseUrl = await Github.GetLatestRelease("xenia-canary", "xenia-canary-releases");
        ClassicAssert.NotNull(releaseUrl, "Couldn't retrieve the latest release");
    }
}