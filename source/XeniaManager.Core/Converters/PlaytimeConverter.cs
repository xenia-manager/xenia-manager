using System.Globalization;
using Avalonia.Data.Converters;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Converters;

/// <summary>
/// Converts playtime values (in minutes) to localized human-readable strings.
/// </summary>
public class PlaytimeConverter : IValueConverter
{
    public static readonly PlaytimeConverter Instance = new PlaytimeConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value is double playtime)
        {
            return PlaytimeFormatter.Format(playtime, culture);
        }

        return value.ToString() ?? string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
