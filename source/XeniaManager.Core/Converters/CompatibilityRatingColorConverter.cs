using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.Core.Converters;

public class CompatibilityRatingColorConverter : IValueConverter
{
    public static readonly CompatibilityRatingColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is CompatibilityRating rating)
        {
            return rating switch
            {
                CompatibilityRating.Unknown => new SolidColorBrush(Color.Parse("#A9A9A9")), // DarkGray
                CompatibilityRating.Unplayable => new SolidColorBrush(Color.Parse("#FF0000")), // Red
                CompatibilityRating.Loads => new SolidColorBrush(Color.Parse("#FFFF00")), // Yellow
                CompatibilityRating.Gameplay => new SolidColorBrush(Color.Parse("#ADFF2F")), // GreenYellow
                CompatibilityRating.Playable => new SolidColorBrush(Color.Parse("#228B22")), // ForestGreen
                _ => new SolidColorBrush(Color.Parse("#A9A9A9"))
            };
        }

        return new SolidColorBrush(Color.Parse("#A9A9A9"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
