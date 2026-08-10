using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Serializes <see cref="Color"/> as an ARGB hex string (e.g. "#FF1C1F25").
/// </summary>
public class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? hex = reader.GetString();
        return !string.IsNullOrEmpty(hex) && hex.StartsWith('#')
            ? Color.Parse(hex)
            : default;
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}");
    }
}
