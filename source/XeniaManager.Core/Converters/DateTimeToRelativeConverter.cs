using System.Globalization;
using Avalonia.Data.Converters;

namespace XeniaManager.Core.Converters;

/// <summary>
/// Converts a DateTime? to a formatted date and time string (e.g. "2024-01-15 14:30:45").
/// </summary>
public class DateTimeToRelativeConverter : IValueConverter
{
    public static readonly DateTimeToRelativeConverter Instance = new DateTimeToRelativeConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime dateTime)
        {
            return string.Empty;
        }

        return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
