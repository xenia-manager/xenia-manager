using System.Reflection;

namespace XeniaManager.Core.Models.Files.Bindings;

/// <summary>
/// Attribute to specify the string representation of a binding key or value.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class BindingNameAttribute : Attribute
{
    public string Name { get; }
    public string[] Alternatives { get; }

    public BindingNameAttribute(string name, params string[] alternatives)
    {
        Name = name;
        Alternatives = alternatives;
    }
}

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