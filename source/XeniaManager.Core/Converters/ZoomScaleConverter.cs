using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace XeniaManager.Core.Converters;

/// <summary>
/// Converts a base size value by scaling it with the zoom percentage.
/// Expects ConverterParameter to be the base size value.
/// </summary>
public class ZoomScaleConverter : IValueConverter
{
    public static readonly ZoomScaleConverter Instance = new ZoomScaleConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double zoomValue && parameter is string parameterStr && double.TryParse(parameterStr, out double baseSize))
        {
            return baseSize * (zoomValue / 100.0);
        }

        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
