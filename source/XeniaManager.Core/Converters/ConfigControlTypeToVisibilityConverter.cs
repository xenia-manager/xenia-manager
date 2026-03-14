using System.Globalization;
using Avalonia.Data.Converters;
using XeniaManager.Core.Models.Files.Config;

namespace XeniaManager.Core.Converters;

/// <summary>
/// Converts a ConfigControlType to a boolean indicating if it matches the specified type.
/// </summary>
public class ConfigControlTypeToVisibilityConverter : IValueConverter
{
    public static readonly ConfigControlTypeToVisibilityConverter Instance = new ConfigControlTypeToVisibilityConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ConfigControlType controlType)
        {
            return false;
        }

        if (parameter is not string parameterString)
        {
            return false;
        }

        // Parse the parameter to ConfigControlType
        if (Enum.TryParse(parameterString, true, out ConfigControlType targetTypeValue))
        {
            return controlType == targetTypeValue;
        }

        return false;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}