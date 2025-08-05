using System.Globalization;
using System.Windows.Data;

namespace XeniaManager.Desktop.Converters;
public class AchievementDescriptionConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 3)
        {
            return string.Empty;
        }

        bool isUnlocked = values[0] is bool b && b;
        string unlocked = values[1]?.ToString() ?? string.Empty;
        string locked = values[2]?.ToString() ?? string.Empty;

        return isUnlocked ? unlocked : locked;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}