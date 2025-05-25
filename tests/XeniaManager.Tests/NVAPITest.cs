using XeniaManager.Core.GPULibrary;

namespace XeniaManager.Tests;

public class NvApiTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        try
        {
            DrsWrapper.Initialize();
            
        }
        catch (Exception e)
        {
            Assert.Fail();
        }
    }
}