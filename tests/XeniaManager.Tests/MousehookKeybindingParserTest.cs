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
        KeyBindingsParser.Parse(@"F:\Xenia Manager\Xenia Manager (UI Rewrite)\source\XeniaManager.Desktop\bin\Debug\net9.0-windows\Emulators\Xenia Mousehook\bindings.ini");
        //Assert.Pass();
        //Assert.Fail();
    }
}