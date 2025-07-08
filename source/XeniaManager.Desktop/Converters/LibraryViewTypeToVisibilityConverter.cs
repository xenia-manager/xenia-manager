using System.Globalization;
using System.Windows;
using System.Windows.Data;

// Imported Libraries
using XeniaManager.Core.Enum;

namespace XeniaManager.Desktop.Converters;
public class LibraryViewTypeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LibraryViewType viewType && parameter is string target)
        {
            LibraryViewType targetView = (LibraryViewType)Enum.Parse(typeof(LibraryViewType), target);
            return viewType == targetView ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}