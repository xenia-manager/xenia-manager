using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace XeniaManager.Core.Converters;

/// <summary>
/// Converts a byte array to an Avalonia Bitmap image.
/// Returns null if the byte array is null or empty.
/// </summary>
public class ByteArrayToImageSourceConverter : IValueConverter
{
    public static readonly ByteArrayToImageSourceConverter Instance = new ByteArrayToImageSourceConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes || bytes.Length == 0)
        {
            return null;
        }

        try
        {
            using MemoryStream ms = new MemoryStream(bytes);
            return new Bitmap(ms);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
