using System.Globalization;
using System.Windows.Data;

namespace XeniaManager.Desktop.Converters;

public class FirstLetterUpperConverter: IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string s && s.Length > 0)
        {
            return char.ToUpper(s[0]) + s.Substring(1);
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}