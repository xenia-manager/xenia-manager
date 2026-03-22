using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using XeniaManager.Core.Models;

namespace XeniaManager.Core.Converters;

/// <summary>
/// Converts a XeniaVersion to a boolean indicating if it equals the specified version.
/// </summary>
public class XeniaVersionEqualsConverter : IValueConverter
{
    public static XeniaVersionEqualsConverter Instance { get; } = new XeniaVersionEqualsConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is XeniaVersion xeniaVersion && parameter is string parameterString)
        {
            if (Enum.TryParse<XeniaVersion>(parameterString, out XeniaVersion targetVersion))
            {
                return xeniaVersion == targetVersion;
            }
        }

        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // ConvertBack doesn't make sense for this converter, so return UnsetValue
        return AvaloniaProperty.UnsetValue;
    }
}