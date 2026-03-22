using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace XeniaManager.Core.Converters;

/// <summary>
/// Converts a string to a boolean indicating whether it is not empty.
/// Returns true if the string is not null or empty, false otherwise.
/// </summary>
public class StringIsNotEmptyConverter : IValueConverter
{
    public static StringIsNotEmptyConverter Instance { get; } = new StringIsNotEmptyConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return !string.IsNullOrEmpty(str);
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ConvertBack doesn't make sense for this converter, so return UnsetValue
        return AvaloniaProperty.UnsetValue;
    }
}