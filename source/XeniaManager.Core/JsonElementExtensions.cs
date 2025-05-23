using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace XeniaManager.Core;

/// <summary>
/// Extension methods for JsonElement to provide easier conversion to CLR types
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Converts a JsonElement to its corresponding CLR object type
    /// </summary>
    public static object ToObject(this JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out int intValue) ? intValue : 
                                   element.TryGetInt64(out long longValue) ? longValue :
                                   element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(e => e.ToObject()).ToArray(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                prop => prop.Name, 
                prop => prop.Value.ToObject()
            ),
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }

    /// <summary>
    /// Converts a JsonElement to a specific type T
    /// </summary>
    public static T ToObject<T>(this JsonElement element)
    {
        return JsonSerializer.Deserialize<T>(element.GetRawText());
    }

    /// <summary>
    /// Safely gets a string value, returning null if not a string
    /// </summary>
    public static string GetStringOrNull(this JsonElement element)
    {
        return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }

    /// <summary>
    /// Safely gets an integer value, returning null if not a number or not convertible to int
    /// </summary>
    public static int? GetInt32OrNull(this JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out int value) ? value : null;
    }

    /// <summary>
    /// Safely gets a boolean value, returning null if not a boolean
    /// </summary>
    public static bool? GetBooleanOrNull(this JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    /// <summary>
    /// Checks if the JsonElement has a specific property (for objects)
    /// </summary>
    public static bool HasProperty(this JsonElement element, string propertyName)
    {
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out _);
    }

    /// <summary>
    /// Gets a property value as an object, or returns null if property doesn't exist
    /// </summary>
    public static object GetPropertyValue(this JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) ? property.ToObject() : null;
    }

    /// <summary>
    /// Gets a property value as a specific type T, or returns default(T) if property doesn't exist
    /// </summary>
    public static T GetPropertyValue<T>(this JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) ? property.ToObject<T>() : default(T);
    }
}