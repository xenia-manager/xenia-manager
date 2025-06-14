using System.Globalization;
using System.Windows.Data;

// Imported Libraries
using Wpf.Ui.Controls;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Converters;
public class LibraryViewTypeToSymbolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            LibraryViewType.Grid => SymbolRegular.Grid24,
            LibraryViewType.List => SymbolRegular.AppsListDetail24,
            _ => SymbolRegular.Question24,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}