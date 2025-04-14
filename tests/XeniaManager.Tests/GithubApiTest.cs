using System.Reflection;

// Imported
using NUnit.Framework.Legacy;
using Octokit;
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
            Release release = await Github.GetLatestRelease(XeniaVersion.Canary);
            ClassicAssert.NotNull(release, "Couldn't retrieve the latest release");
        }
        catch (Exception ex)
        {
            Assert.Fail(ex.Message);
        }
    }
    
    [Test]
    public async Task GrabLatestReleaseFailTest()
    {
        var ex = Assert.ThrowsAsync<NotImplementedException>(async () => 
        {
            await Github.GetLatestRelease(XeniaVersion.Custom);
        });

        Assert.That(ex.Message, Is.EqualTo($"Xenia {XeniaVersion.Custom} is not implemented."));
    }
    
    /// <summary>
    /// NUnit test for the GetPatchesFolderContentsAsync function.
    /// This test verifies that the GitHub API returns a non-null, non-empty list of contents from the 'patches' directory.
    /// </summary>
    [Test]
    public async Task GetCanaryGamePatchesTest()
    {
        try
        {
            IReadOnlyList<RepositoryContent> contents = await Github.GetGamePatches(XeniaVersion.Canary);
                
            ClassicAssert.NotNull(contents, "Patches folder contents should not be null.");
            ClassicAssert.IsNotEmpty(contents, "Patches folder contents should not be empty.");
                
            // Optionally, log the names of the files/directories retrieved.
            foreach (var item in contents)
            {
                Logger.Info($"Found item in patches: {item.Name}");
            }
        }
        catch (Exception ex)
        {
            Assert.Fail("Error retrieving patches folder contents: " + ex.Message);
        }
    }
    
    /// <summary>
    /// NUnit test for the GetPatchesFolderContentsAsync function.
    /// This test verifies that the GitHub API returns a non-null, non-empty list of contents from the 'patches' directory.
    /// </summary>
    [Test]
    public async Task GetNetplayGamePatchesTest()
    {
        try
        {
            IReadOnlyList<RepositoryContent> contents = await Github.GetGamePatches(XeniaVersion.Netplay);
                
            ClassicAssert.NotNull(contents, "Patches folder contents should not be null.");
            ClassicAssert.IsNotEmpty(contents, "Patches folder contents should not be empty.");
                
            // Optionally, log the names of the files/directories retrieved.
            foreach (var item in contents)
            {
                Logger.Info($"Found item in patches: {item.Name}");
            }
        }
        catch (Exception ex)
        {
            Assert.Fail("Error retrieving patches folder contents: " + ex.Message);
        }
    }
}