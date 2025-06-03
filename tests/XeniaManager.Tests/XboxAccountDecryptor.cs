using XeniaManager.Core;

namespace XeniaManager.Tests;

public class XboxAccountDecryptorTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        byte[] account = File.ReadAllBytes("Account");
        GamerProfile profile = new GamerProfile();
        if (XboxProfileManager.TryDecryptAccountFile(account, ref profile))
        {
            Logger.Info($"{profile.Name}");
            Assert.Pass();
        }
        else
        {
            Assert.Fail();
        }
    }
}