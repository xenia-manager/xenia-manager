using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace XeniaManager.Core.Converters;

/// <summary>
/// Converts a byte array to an Avalonia Bitmap image, with caching by content hash.
/// Returns null if the byte array is null or empty.
/// </summary>
public class ByteArrayToImageSourceConverter : IValueConverter
{
    public static readonly ByteArrayToImageSourceConverter Instance = new ByteArrayToImageSourceConverter();

    private static readonly ConcurrentDictionary<string, WeakReference<Bitmap>> _cache = new ConcurrentDictionary<string, WeakReference<Bitmap>>();
    private const int MaxCacheSize = 200;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] bytes || bytes.Length == 0)
        {
            return null;
        }

        try
        {
            string hash = System.Convert.ToHexString(MD5.HashData(bytes));

            if (_cache.TryGetValue(hash, out WeakReference<Bitmap>? weakRef) &&
                weakRef.TryGetTarget(out Bitmap? cached))
            {
                return cached;
            }

            using MemoryStream ms = new MemoryStream(bytes);
            Bitmap bitmap = new Bitmap(ms);
            _cache[hash] = new WeakReference<Bitmap>(bitmap);

            if (_cache.Count > MaxCacheSize)
            {
                CleanupStaleEntries();
            }

            return bitmap;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void CleanupStaleEntries()
    {
        List<string> staleKeys = [];
        foreach (KeyValuePair<string, WeakReference<Bitmap>> entry in _cache)
        {
            if (!entry.Value.TryGetTarget(out _))
            {
                staleKeys.Add(entry.Key);
            }
        }

        foreach (string key in staleKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}