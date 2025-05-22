using System.Globalization;
using System.Windows.Data;

namespace XeniaManager.Desktop.Converters;

public class FrameRateToTextConverter: IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double frameRate)
        {
            if (frameRate <= 0)
            {
                return "Off";
            }
            return $"{(int)frameRate} FPS";
        }
        return "Off";
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}