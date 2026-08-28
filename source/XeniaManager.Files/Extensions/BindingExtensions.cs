using System.Reflection;
using XeniaManager.Core.Models.Files.Bindings;

namespace XeniaManager.Core.Extensions;

/// <summary>
/// Extension methods for binding enums.
/// </summary>
public static class BindingExtensions
{
    /// <summary>
    /// Gets the string representation of a binding enum value.
    /// </summary>
    public static string ToBindingString(this Enum value)
    {
        FieldInfo? field = value.GetType().GetField(value.ToString());
        BindingNameAttribute? attribute = field?.GetCustomAttributes(typeof(BindingNameAttribute), false)
            .FirstOrDefault() as BindingNameAttribute;
        return attribute?.Name ?? value.ToString();
    }

    /// <summary>
    /// Converts a VirtualKeyCode to its Xenia key string representation.
    /// Returns the primary binding name or null if the key is not valid.
    /// </summary>
    /// <param name="keyCode">The virtual key code to convert.</param>
    /// <returns>The Xenia key string representation, or null if the key is None.</returns>
    public static string? ToXeniaKey(this VirtualKeyCode keyCode)
    {
        if (keyCode == VirtualKeyCode.None)
        {
            return null;
        }

        FieldInfo? field = keyCode.GetType().GetField(keyCode.ToString());
        BindingNameAttribute? attribute = field?.GetCustomAttributes(typeof(BindingNameAttribute), false)
            .FirstOrDefault() as BindingNameAttribute;
        return attribute?.Name ?? keyCode.ToString();
    }

    /// <summary>
    /// Parses a string to the specified enum type using BindingNameAttribute.
    /// </summary>
    public static T? ParseFromBindingString<T>(string value) where T : struct, Enum
    {
        foreach (FieldInfo field in typeof(T).GetFields())
        {
            BindingNameAttribute? attribute = field.GetCustomAttributes(typeof(BindingNameAttribute), false)
                .FirstOrDefault() as BindingNameAttribute;
            if (attribute == null)
            {
                continue;
            }

            // Check the main name
            if (attribute.Name == value)
            {
                return (T)field.GetValue(null)!;
            }

            // Check alternatives
            foreach (string alternative in attribute.Alternatives)
            {
                if (alternative == value)
                {
                    return (T)field.GetValue(null)!;
                }
            }
        }
        return null;
    }
}
