using System.Text.RegularExpressions;

namespace XeniaManager.Core.Models;

/// <summary>
/// Represents an enum value paired with a formatted display name.
/// </summary>
/// <typeparam name="T">The enum type.</typeparam>
public class EnumDisplayItem<T> where T : Enum
{
    /// <summary>
    /// The enum value.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// The formatted display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumDisplayItem{T}"/> class.
    /// </summary>
    /// <param name="value">The enum value.</param>
    public EnumDisplayItem(T value)
    {
        Value = value;
        DisplayName = FormatEnumName(value.ToString());
    }

    /// <summary>
    /// Formats an enum name by inserting spaces before capital letters.
    /// </summary>
    /// <param name="name">The enum name to format.</param>
    /// <returns>The formatted name with spaces.</returns>
    private static string FormatEnumName(string name)
    {
        // Insert spaces before capital letters (except the first letter)
        return Regex.Replace(name, "(?<!^)([A-Z])", " $1");
    }

    /// <summary>
    /// Returns the display name.
    /// </summary>
    /// <returns>The formatted display name.</returns>
    public override string ToString() => DisplayName;
}