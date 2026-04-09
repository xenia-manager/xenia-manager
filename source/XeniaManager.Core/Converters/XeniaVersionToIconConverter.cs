using System.Globalization;
using Avalonia.Data.Converters;
using FluentIcons.Common;
using XeniaManager.Core.Models;

namespace XeniaManager.Core.Converters;

/// <summary>
/// Converts XeniaVersion enum to appropriate Fluent UI icon symbol.
/// Canary -> XboxController
/// Mousehook -> Keyboard
/// Netplay -> Globe
/// Custom -> AppFolder
/// </summary>
public class XeniaVersionToIconConverter : IValueConverter
{
    public static readonly XeniaVersionToIconConverter Instance = new XeniaVersionToIconConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is XeniaVersion version)
        {
            return version switch
            {
                XeniaVersion.Canary => Symbol.XboxController,
                XeniaVersion.Mousehook => Symbol.Keyboard,
                XeniaVersion.Netplay => Symbol.Globe,
                XeniaVersion.Custom => Symbol.AppFolder,
                _ => Symbol.XboxController
            };
        }
        return Symbol.XboxController;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}