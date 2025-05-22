using System.Globalization;
using System.Windows.Data;

namespace XeniaManager.Desktop.Converters;

public class DivideByConverter : IValueConverter
{
    private double _divisor { get; set; } = 100;
    private string _format { get; set; } = "0.0";
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double number)
        {
            if (parameter is string stringParameter)
            {
                string[] split = stringParameter.Split(':');
                if (split.Length >= 1 && double.TryParse(split[0], out double parsedDivisor))
                {
                    _divisor = parsedDivisor;
                }

                if (split.Length >= 2 && !string.IsNullOrEmpty(split[1]))
                {
                    _format = split[1];
                }
            }
            
            return (number / _divisor).ToString(_format, culture);
        }
        return "0";
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}