using System.Text.Json;
using XeniaManager.Core.Converters;

namespace XeniaManager.Tests.Database.Converters;

public class StringOrArrayJsonConverterTests
{
    private readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        Converters =
        {
            new StringOrArrayJsonConverter()
        }
    };

    private class Wrapper
    {
        public List<string> Values { get; set; } = [];
    }

    [Test]
    public void Read_String_ReturnsSingleElementList()
    {
        string json = """{"Values":"hello"}""";
        Wrapper result = JsonSerializer.Deserialize<Wrapper>(json, _options)!;
        Assert.That(result.Values, Is.EqualTo(new List<string>
        {
            "hello"
        }));
    }

    [Test]
    public void Read_Array_ReturnsAllElements()
    {
        string json = """{"Values":["a","b","c"]}""";
        Wrapper result = JsonSerializer.Deserialize<Wrapper>(json, _options)!;
        Assert.That(result.Values, Is.EqualTo(new List<string>
        {
            "a",
            "b",
            "c"
        }));
    }

    [Test]
    public void Read_EmptyString_ReturnsEmptyList()
    {
        string json = """{"Values":""}""";
        Wrapper result = JsonSerializer.Deserialize<Wrapper>(json, _options)!;
        Assert.That(result.Values, Is.Empty);
    }

    [Test]
    public void Read_EmptyArray_ReturnsEmptyList()
    {
        string json = """{"Values":[]}""";
        Wrapper result = JsonSerializer.Deserialize<Wrapper>(json, _options)!;
        Assert.That(result.Values, Is.Empty);
    }

    [Test]
    public void Read_ArrayWithEmptyStrings_FiltersEmpty()
    {
        string json = """{"Values":["a","","b",null]}""";
        Wrapper result = JsonSerializer.Deserialize<Wrapper>(json, _options)!;
        Assert.That(result.Values, Is.EqualTo(new List<string>
        {
            "a",
            "b"
        }));
    }

    [Test]
    public void Read_NullToken_ReturnsEmptyList()
    {
        // Number token falls through to return []
        string json = """{"Values":123}""";
        Wrapper result = JsonSerializer.Deserialize<Wrapper>(json, _options)!;
        Assert.That(result.Values, Is.Empty);
    }

    [Test]
    public void Write_SingleElement_WritesString()
    {
        Wrapper wrapper = new Wrapper
        {
            Values = ["only"]
        };
        string json = JsonSerializer.Serialize(wrapper, _options);
        Assert.That(json, Does.Contain("\"only\""));
        Assert.That(json, Does.Not.Contain("["));
    }

    [Test]
    public void Write_MultipleElements_WritesArray()
    {
        Wrapper wrapper = new Wrapper
        {
            Values = ["a", "b"]
        };
        string json = JsonSerializer.Serialize(wrapper, _options);
        Assert.That(json, Is.EqualTo("""{"Values":["a","b"]}"""));
    }

    [Test]
    public void Write_EmptyList_WritesEmptyArray()
    {
        Wrapper wrapper = new Wrapper
        {
            Values = []
        };
        string json = JsonSerializer.Serialize(wrapper, _options);
        Assert.That(json, Is.EqualTo("""{"Values":[]}"""));
    }

    [Test]
    public void RoundTrip_SingleString_Preserves()
    {
        Wrapper original = new Wrapper
        {
            Values = ["hello"]
        };
        string json = JsonSerializer.Serialize(original, _options);
        Wrapper round = JsonSerializer.Deserialize<Wrapper>(json, _options)!;
        Assert.That(round.Values, Is.EqualTo(original.Values));
    }

    [Test]
    public void RoundTrip_Array_Preserves()
    {
        Wrapper original = new Wrapper
        {
            Values = ["a", "b", "c"]
        };
        string json = JsonSerializer.Serialize(original, _options);
        Wrapper round = JsonSerializer.Deserialize<Wrapper>(json, _options)!;
        Assert.That(round.Values, Is.EqualTo(original.Values));
    }
}