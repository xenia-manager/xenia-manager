using System.Globalization;
using System.Windows.Data;

// Imported Libraries
using XeniaManager.Desktop.Utilities;

namespace XeniaManager.Desktop.Converters;
public static class PlaytimeFormatter
{
    public static string Format(double playtime, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        if (playtime == 0)
        {
            return LocalizationHelper.GetUiText("LibraryGameButton_PlaytimeNeverPlayed");
        }
        else if (playtime < 60)
        {
            return string.Format(
                LocalizationHelper.GetUiText("LibraryGameButton_PlaytimeMinutes"),
                playtime.ToString("N0", culture)
            );
        }
        else
        {
            return string.Format(
                LocalizationHelper.GetUiText("LibraryGameButton_PlaytimeHours"),
                (playtime / 60).ToString("N1", culture)
            );
        }
    }
}

public class PlaytimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value is double playtime)
        {
            return PlaytimeFormatter.Format(playtime, culture);
        }

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}