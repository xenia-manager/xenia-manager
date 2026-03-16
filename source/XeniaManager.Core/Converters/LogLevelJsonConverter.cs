using System.Text.Json;
using System.Text.Json.Serialization;
using NLog;

namespace XeniaManager.Core.Converters;

/// <summary>
/// JSON converter for NLog LogLevel to handle serialization/deserialization using integer values
/// </summary>
public class LogLevelJsonConverter : JsonConverter<LogLevel>
{
    public override LogLevel Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        int value = reader.GetInt32();
        return value switch
        {
            0 => LogLevel.Trace,
            1 => LogLevel.Debug,
            2 => LogLevel.Info,
            3 => LogLevel.Warn,
            4 => LogLevel.Error,
            5 => LogLevel.Fatal,
            6 => LogLevel.Off,
            _ => LogLevel.Info
        };
    }

    public override void Write(Utf8JsonWriter writer, LogLevel value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Ordinal);
    }
}