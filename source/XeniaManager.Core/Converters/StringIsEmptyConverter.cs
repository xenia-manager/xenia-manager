using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace XeniaManager.Core.Converters;

/// <summary>
/// Converts a string to a boolean indicating whether it is null or empty.
/// Returns true if the string is null or empty, false otherwise.
/// </summary>
public class StringIsEmptyConverter : IValueConverter
{
    public static StringIsEmptyConverter Instance { get; } = new StringIsEmptyConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str)
        {
            return string.IsNullOrEmpty(str);
        }
        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ConvertBack doesn't make sense for this converter, so return UnsetValue
        return AvaloniaProperty.UnsetValue;
    }
}