using System.Globalization;
using Avalonia.Data.Converters;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Converters;

public class NetplayStatusValueToStringConverter : IValueConverter
{
    public static readonly NetplayStatusValueToStringConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NetplayStatusValue status)
        {
            return LocalizationHelper.GetText($"NetplayStatus.{status}");
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
