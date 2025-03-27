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
    public async Task CheckRateLimitTest()
    {
        bool test = await Github.IsRateLimitAvailableAsync();
        ClassicAssert.IsTrue(test, "Rate limit reached");
    }

    [Test]
    public async Task GrabLatestCanaryReleaseTest()
    {
        try
        {
            string releaseUrl = await Github.GetLatestRelease(Xenia.Canary);
            ClassicAssert.NotNull(releaseUrl, "Couldn't retrieve the latest release");
        }
        catch (Exception ex)
        {
            Assert.Fail(ex.Message);
        }
    }
    
    [Test]
    public async Task GrabLatestReleaseFailTest()
    {
        Exception ex = Assert.ThrowsAsync<Exception>(async () =>
        {
            await Github.GetLatestRelease(Xenia.Custom);
        });

        Assert.That(ex.Message, Is.EqualTo("Unknown Xenia release type."));
    }
}