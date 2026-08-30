using System.Globalization;
using Avalonia.Data.Converters;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Converters;

public class MousehookSupportRatingToStringConverter : IValueConverter
{
    public static readonly MousehookSupportRatingToStringConverter Instance = new MousehookSupportRatingToStringConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MousehookSupportRating rating)
        {
            return LocalizationHelper.GetText($"MousehookSupportRating.{rating}");
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}