using System.Text.Json;
using System.Text.Json.Serialization;
using XeniaManager.Database.Models.Game;

namespace XeniaManager.Database.Converters;

/// <summary>
/// Custom JSON converter for NetplayStatusValue that maps null to Unknown
/// </summary>
public class NetplayStatusValueConverter : JsonConverter<NetplayStatusValue>
{
    public override NetplayStatusValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return NetplayStatusValue.Unknown;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();
            return value?.ToLowerInvariant() switch
            {
                "ok" => NetplayStatusValue.Ok,
                "partial" => NetplayStatusValue.Partial,
                "fail" => NetplayStatusValue.Fail,
                _ => NetplayStatusValue.Unknown
            };
        }

        return NetplayStatusValue.Unknown;
    }

    public override void Write(Utf8JsonWriter writer, NetplayStatusValue value, JsonSerializerOptions options)
    {
        if (value == NetplayStatusValue.Unknown)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.ToString().ToLowerInvariant());
    }
}