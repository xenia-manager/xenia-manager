using System.Collections;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace XeniaManager.Core.Converters;

/// <summary>
/// Converts a collection to a boolean indicating whether it is empty.
/// Returns true if the collection is null or empty, false otherwise.
/// </summary>
public class CollectionIsEmptyConverter : IValueConverter
{
    public static CollectionIsEmptyConverter Instance { get; } = new CollectionIsEmptyConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is IEnumerable collection)
        {
            // Check if the collection is empty
            foreach (object? _ in collection)
            {
                return false; // Has at least one item
            }
            return true; // Empty
        }
        return true; // Null or not IEnumerable
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ConvertBack doesn't make sense for this converter, so return UnsetValue
        return AvaloniaProperty.UnsetValue;
    }
}