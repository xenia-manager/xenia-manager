using System.Globalization;
using Avalonia.Data.Converters;
using XeniaManager.Core.Utilities;
using XeniaManager.Database.Models.Game;

namespace XeniaManager.Core.Converters;

public class CompatibilityRatingToStringConverter : IValueConverter
{
    public static readonly CompatibilityRatingToStringConverter Instance = new CompatibilityRatingToStringConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CompatibilityRating rating)
        {
            return LocalizationHelper.GetText($"CompatibilityRating.{rating}");
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}