using System.Text.Json;
using XeniaManager.Core.Converters;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.Tests.Database.Converters;

public class NetplayStatusValueConverterTests
{
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        Converters =
        {
            new NetplayStatusValueConverter()
        }
    };

    private class Wrapper
    {
        public NetplayStatusValue Status { get; set; }
    }

    [Test]
    public void Read_StringOk_ReturnsOk()
    {
        string json = """{"Status":"ok"}""";
        Wrapper result = JsonSerializer.Deserialize<Wrapper>(json, _options)!;
        Assert.That(result.Status, Is.EqualTo(NetplayStatusValue.Ok));
    }

    [Test]
    public void Read_StringPartial_ReturnsPartial()
    {
        string json = """{"Status":"partial"}""";
        Assert.That(JsonSerializer.Deserialize<Wrapper>(json, _options)!.Status, Is.EqualTo(NetplayStatusValue.Partial));
    }

    [Test]
    public void Read_StringFail_ReturnsFail()
    {
        string json = """{"Status":"fail"}""";
        Assert.That(JsonSerializer.Deserialize<Wrapper>(json, _options)!.Status, Is.EqualTo(NetplayStatusValue.Fail));
    }

    [Test]
    public void Read_StringCaseInsensitive_ReturnsCorrect()
    {
        string json = """{"Status":"OK"}""";
        Assert.That(JsonSerializer.Deserialize<Wrapper>(json, _options)!.Status, Is.EqualTo(NetplayStatusValue.Ok));
        json = """{"Status":"Partial"}""";
        Assert.That(JsonSerializer.Deserialize<Wrapper>(json, _options)!.Status, Is.EqualTo(NetplayStatusValue.Partial));
    }

    [Test]
    public void Read_UnknownString_ReturnsUnknown()
    {
        string json = """{"Status":"notvalid"}""";
        Assert.That(JsonSerializer.Deserialize<Wrapper>(json, _options)!.Status, Is.EqualTo(NetplayStatusValue.Unknown));
    }

    [Test]
    public void Read_Null_ReturnsUnknown()
    {
        string json = """{"Status":null}""";
        Assert.That(JsonSerializer.Deserialize<Wrapper>(json, _options)!.Status, Is.EqualTo(NetplayStatusValue.Unknown));
    }

    [Test]
    public void Read_Number_ReturnsUnknown()
    {
        string json = """{"Status":123}""";
        Assert.That(JsonSerializer.Deserialize<Wrapper>(json, _options)!.Status, Is.EqualTo(NetplayStatusValue.Unknown));
    }

    [Test]
    public void Write_Ok_WritesLowercaseString()
    {
        Wrapper wrapper = new Wrapper
        {
            Status = NetplayStatusValue.Ok
        };
        string json = JsonSerializer.Serialize(wrapper, _options);
        Assert.That(json, Is.EqualTo("""{"Status":"ok"}"""));
    }

    [Test]
    public void Write_Unknown_WritesNull()
    {
        Wrapper wrapper = new Wrapper
        {
            Status = NetplayStatusValue.Unknown
        };
        string json = JsonSerializer.Serialize(wrapper, _options);
        Assert.That(json, Is.EqualTo("""{"Status":null}"""));
    }

    [TestCase(NetplayStatusValue.Ok, "ok")]
    [TestCase(NetplayStatusValue.Partial, "partial")]
    [TestCase(NetplayStatusValue.Fail, "fail")]
    public void Write_Various_WritesCorrectString(NetplayStatusValue value, string expected)
    {
        Wrapper wrapper = new Wrapper
        {
            Status = value
        };
        string json = JsonSerializer.Serialize(wrapper, _options);
        Assert.That(json, Is.EqualTo($$$"""{"Status":"{{{expected}}}"}"""));
    }

    [Test]
    public void RoundTrip_Preserves()
    {
        foreach (NetplayStatusValue val in new[]
                 {
                     NetplayStatusValue.Ok, NetplayStatusValue.Partial, NetplayStatusValue.Fail, NetplayStatusValue.Unknown
                 })
        {
            Wrapper original = new Wrapper
            {
                Status = val
            };
            string json = JsonSerializer.Serialize(original, _options);
            Wrapper round = JsonSerializer.Deserialize<Wrapper>(json, _options)!;
            Assert.That(round.Status, Is.EqualTo(val));
        }
    }
}