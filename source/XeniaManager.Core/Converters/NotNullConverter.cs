using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace XeniaManager.Core.Converters;

public class NotNullConverter : IValueConverter
{
    public static NotNullConverter Instance { get; } = new NotNullConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ConvertBack doesn't make sense for this converter, so return UnsetValue
        return AvaloniaProperty.UnsetValue;
    }
}