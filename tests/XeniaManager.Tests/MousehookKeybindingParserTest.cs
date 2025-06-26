using XeniaManager.Core.Mousehook;

namespace XeniaManager.Tests;

public class MousehookKeybindingParserTest
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        BindingsParser.Parse(@"bindings.ini");
        //Assert.Pass();
        //Assert.Fail();
    }
}