using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace XeniaManager.Core.Converters;

public class EnumToValueConverter : IValueConverter
{
    public static readonly EnumToValueConverter Instance = new EnumToValueConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Enum enumValue && parameter is string paramName)
        {
            return enumValue.ToString() == paramName;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
