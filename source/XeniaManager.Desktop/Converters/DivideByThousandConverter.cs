using System.Globalization;
using System.Windows.Data;

namespace XeniaManager.Desktop.Converters;

public class DivideByThousandConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double number)
        {
            return (number / 1000).ToString("0.000", culture);
        }
        return "0";
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}