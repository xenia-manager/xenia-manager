using System.Globalization;
using Avalonia.Data.Converters;
using XeniaManager.Core.Models;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Converters;

public class ThemeToStringConverter : IValueConverter
{
    public static readonly ThemeToStringConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Theme theme)
        {
            return LocalizationHelper.GetText($"SettingsPage.Ui.Theme.Option.{theme}");
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
