using System.Globalization;
using Avalonia.Data.Converters;

namespace XeniaManager.Core.Converters;

/// <summary>
/// Converts a double value to a formatted string using the provided format string.
/// Expects input as an object array where [0] is the double value and [1] is the format string.
/// </summary>
public class DoubleFormatConverter : IMultiValueConverter
{
    public static DoubleFormatConverter Instance { get; } = new DoubleFormatConverter();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 1 && values[0] is double doubleValue)
        {
            string? format = values.Count >= 2 ? values[1] as string : "F0";
            return doubleValue.ToString(format ?? "F0", culture);
        }
        return string.Empty;
    }
}
