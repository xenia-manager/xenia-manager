using XeniaManager.Files.Models.Patches;
using XeniaManager.Files.Utilities;

namespace XeniaManager.Tests.Files;

[TestFixture]
public class PatchValueParserTests
{
    [Test]
    public void TryParse_F32HexInteger_ReturnsFloat()
    {
        bool success = PatchValueParser.TryParse("0x60000000", PatchType.F32, out object? value);

        Assert.That(success, Is.True);
        Assert.That(value, Is.TypeOf<float>());
        Assert.That(value, Is.EqualTo(1610612736f));
    }

    [TestCase("inf", float.PositiveInfinity)]
    [TestCase("-inf", float.NegativeInfinity)]
    public void TryParse_F32SpecialValues_ReturnsFloat(string text, float expected)
    {
        bool success = PatchValueParser.TryParse(text, PatchType.F32, out object? value);

        Assert.That(success, Is.True);
        Assert.That(value, Is.TypeOf<float>());
        Assert.That(value, Is.EqualTo(expected));
    }

    [TestCase("0b1010", 10u)]
    [TestCase("0o17", 15u)]
    [TestCase("0x10", 16u)]
    [TestCase("16", 16u)]
    public void TryParse_Be32IntegerForms_MatchesToml(string text, uint expected)
    {
        bool success = PatchValueParser.TryParse(text, PatchType.Be32, out object? value);

        Assert.That(success, Is.True);
        Assert.That(value, Is.EqualTo(expected));
    }

    [Test]
    public void TryParse_Be64HighBit_ReturnsUInt64()
    {
        bool success = PatchValueParser.TryParse("0xFFFFFFFFFFFFFFFF", PatchType.Be64, out object? value);

        Assert.That(success, Is.True);
        Assert.That(value, Is.EqualTo(ulong.MaxValue));
    }

    [Test]
    public void TryParse_ArrayOddLength_UsesTrailingNibble()
    {
        bool success = PatchValueParser.TryParse("0xabc", PatchType.Array, out object? value);

        Assert.That(success, Is.True);
        Assert.That(value, Is.EqualTo(new byte[]
        {
            0xAB, 0x0C
        }));
    }

    [TestCase("0xFFFFFFFF", true)]
    [TestCase("0x7FFFFFFFFFFFFFFF", true)]
    [TestCase("0x8000000000000000", false)]
    [TestCase("0xFFFFFFFFFFFFFFFF", false)]
    [TestCase("-1", true)]
    [TestCase("9223372036854775807", true)]
    [TestCase("9223372036854775808", false)]
    public void IsTomlRepresentableLiteral_RespectsSigned64BitRange(string text, bool expected) =>
        Assert.That(PatchValueParser.IsTomlRepresentableLiteral(text, PatchType.Be64), Is.EqualTo(expected));
}