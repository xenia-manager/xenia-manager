using System.Globalization;
using Avalonia.Data.Converters;
using XeniaManager.Core.Models;

namespace XeniaManager.Core.Converters;

/// <summary>
/// Converts XeniaVersion enum to a display string for tooltips.
/// </summary>
public class XeniaVersionToStringConverter : IValueConverter
{
    public static readonly XeniaVersionToStringConverter Instance = new XeniaVersionToStringConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is XeniaVersion version)
        {
            return version switch
            {
                XeniaVersion.Canary => "Xenia Canary",
                XeniaVersion.Mousehook => "Xenia Mousehook",
                XeniaVersion.Netplay => "Xenia Netplay",
                XeniaVersion.Custom => "Custom Xenia",
                _ => "Unknown"
            };
        }
        return "Unknown";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}