using System.Globalization;
using Avalonia.Data.Converters;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Converters;

public class CompatibilityRatingToStringConverter : IValueConverter
{
    public static readonly CompatibilityRatingToStringConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CompatibilityRating rating)
        {
            return LocalizationHelper.GetText($"CompatibilityRating.{rating}");
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}