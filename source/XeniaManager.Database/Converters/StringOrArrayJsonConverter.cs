using System.Text.Json;
using System.Text.Json.Serialization;

namespace XeniaManager.Database.Converters;

/// <summary>
/// Custom JSON converter that handles JSON values that can be either a string or an array of strings.
/// Always deserializes to a List&lt;string&gt;.
/// </summary>
public class StringOrArrayJsonConverter : JsonConverter<List<string>>
{
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? value = reader.GetString();
            return string.IsNullOrEmpty(value) ? [] : [value];
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            List<string> result = [];
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    string? item = reader.GetString();
                    if (!string.IsNullOrEmpty(item))
                    {
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        return [];
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        if (value.Count == 1)
        {
            writer.WriteStringValue(value[0]);
        }
        else
        {
            writer.WriteStartArray();
            foreach (string item in value)
            {
                writer.WriteStringValue(item);
            }

            writer.WriteEndArray();
        }
    }
}