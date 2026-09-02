using System.Globalization;
using Avalonia.Data.Converters;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Converters;

/// <summary>
/// Converts a disc count (int) to a localized "N Discs" label, shown on the
/// library card tooltip for multi-disc games.
/// </summary>
public class DiscCountConverter : IValueConverter
{
    public static readonly DiscCountConverter Instance = new DiscCountConverter();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int discCount)
        {
            return string.Format(LocalizationHelper.GetText("LibraryPage.GameButton.DiscCount"), discCount);
        }

        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
}