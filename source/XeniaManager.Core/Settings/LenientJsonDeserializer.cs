using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Settings;

/// <summary>
/// Provides error-tolerant JSON deserialization for settings objects.
/// </summary>
public class LenientJsonDeserializer
{
    /// <summary>
    /// Deserializes JSON with error recovery, replacing invalid property values with defaults.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="options">The JsonSerializerOptions to use.</param>
    /// <returns>The deserialized object with invalid properties replaced by defaults.</returns>
    public static T? Deserialize<T>(string json, JsonSerializerOptions options) where T : class, new()
    {
        try
        {
            // First, try normal deserialization
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (JsonException ex)
        {
            Logger.Debug<LenientJsonDeserializer>($"Initial deserialization failed: {ex.Message}");
            Logger.Debug<LenientJsonDeserializer>($"Error path: {ex.Path}, Line: {ex.LineNumber}, Position: {ex.BytePositionInLine}");

            // Try to fix the JSON and deserialize again
            try
            {
                string fixedJson = FixInvalidProperties<T>(json, options);
                return JsonSerializer.Deserialize<T>(fixedJson, options);
            }
            catch (JsonException ex2)
            {
                Logger.Warning<LenientJsonDeserializer>($"Property-level recovery failed: {ex2.Message}");

                // If all else fails, try to deserialize with a completely fresh approach
                try
                {
                    return DeserializeWithNodeApproach<T>(json, options);
                }
                catch (JsonException ex3)
                {
                    Logger.Warning<LenientJsonDeserializer>($"Node-based recovery also failed: {ex3.Message}");
                    return new T(); // Return default instance
                }
            }
        }
    }

    /// <summary>
    /// Attempts to fix invalid properties in the JSON by using JsonNode to parse and reconstruct.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="json">The original JSON.</param>
    /// <param name="options">Serializer options.</param>
    /// <returns>Fixed JSON string.</returns>
    private static string FixInvalidProperties<T>(string json, JsonSerializerOptions options)
    {
        try
        {
            JsonNode? node = JsonNode.Parse(json);
            if (node == null)
            {
                return json;
            }

            // Get all properties of the target type
            PropertyInfo[] properties = typeof(T).GetProperties();

            // Process each property
            foreach (PropertyInfo property in properties)
            {
                string jsonPropName = property.Name; // Default to property name

                // Check for JsonPropertyName attribute
                JsonPropertyNameAttribute? jsonPropAttr = property.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
                    .Cast<JsonPropertyNameAttribute>()
                    .FirstOrDefault();

                if (jsonPropAttr != null)
                {
                    jsonPropName = jsonPropAttr.Name;
                }

                // If the property exists in the JSON, validate it
                if (node[jsonPropName] != null)
                {
                    JsonNode? propertyNode = node[jsonPropName];

                    // Check if it's an enum property
                    if (property.PropertyType.IsEnum)
                    {
                        if (propertyNode is JsonValue jsonValue)
                        {
                            string valueStr = jsonValue.ToString();

                            // Try to parse the enum value
                            if (!Enum.TryParse(property.PropertyType, valueStr, true, out _))
                            {
                                // Invalid enum value, replace with default
                                object defaultValue = Enum.ToObject(property.PropertyType, 0);
                                node[jsonPropName] = JsonSerializer.SerializeToNode(defaultValue, property.PropertyType, options);
                                Logger.Debug<LenientJsonDeserializer>($"Replaced invalid enum value '{valueStr}' with default '{defaultValue}' for property '{jsonPropName}'");
                            }
                        }
                    }
                }
            }

            return node.ToJsonString(options);
        }
        catch (Exception ex)
        {
            Logger.Debug<LenientJsonDeserializer>($"Failed to fix properties: {ex.Message}");
            return json;
        }
    }

    /// <summary>
    /// Deserializes using a node-based approach that's more tolerant of errors.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="json">The JSON to deserialize.</param>
    /// <param name="options">Serializer options.</param>
    /// <returns>The deserialized object.</returns>
    private static T? DeserializeWithNodeApproach<T>(string json, JsonSerializerOptions options) where T : class, new()
    {
        try
        {
            // Parse as JsonNode first
            JsonNode? rootNode = JsonNode.Parse(json);
            if (rootNode == null)
            {
                return new T();
            }

            // Create a new instance
            T result = new T();

            // Get all properties
            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (!property.CanWrite)
                {
                    continue;
                }

                string jsonPropName = property.Name;

                // Check for JsonPropertyName attribute
                JsonPropertyNameAttribute? jsonPropAttr = property.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
                    .Cast<JsonPropertyNameAttribute>()
                    .FirstOrDefault();

                if (jsonPropAttr != null)
                {
                    jsonPropName = jsonPropAttr.Name;
                }

                try
                {
                    if (rootNode[jsonPropName] != null)
                    {
                        JsonNode? propertyNode = rootNode[jsonPropName];

                        // Handle enum properties specially
                        if (property.PropertyType.IsEnum)
                        {
                            string? valueStr = propertyNode?.ToString();
                            if (Enum.TryParse(property.PropertyType, valueStr, true, out object? enumValue))
                            {
                                property.SetValue(result, enumValue);
                            }
                            else
                            {
                                // Set default enum value
                                object defaultValue = Enum.ToObject(property.PropertyType, 0);
                                property.SetValue(result, defaultValue);
                                Logger.Debug<LenientJsonDeserializer>($"Using default value '{defaultValue}' for invalid enum '{jsonPropName}'");
                            }
                        }
                        else
                        {
                            // Try to deserialize the property value
                            string propertyJson = propertyNode?.ToJsonString(options) ?? "{}";
                            object? propertyValue = JsonSerializer.Deserialize(propertyJson, property.PropertyType, options);
                            if (propertyValue != null)
                            {
                                property.SetValue(result, propertyValue);
                            }
                        }
                    }
                    else
                    {
                        // Property not in JSON, use default value
                        if (property.PropertyType.IsValueType)
                        {
                            property.SetValue(result, Activator.CreateInstance(property.PropertyType));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Debug<LenientJsonDeserializer>($"Failed to deserialize property '{jsonPropName}': {ex.Message}, using default");

                    // Set the default value for the property
                    if (property.PropertyType.IsValueType)
                    {
                        property.SetValue(result, Activator.CreateInstance(property.PropertyType));
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.Debug<LenientJsonDeserializer>($"Node-based deserialization failed: {ex.Message}");
            return new T();
        }
    }
}