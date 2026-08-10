using System;
using Avalonia.Media;

namespace XeniaManager.BigScreen.Utilities;

/// <summary>
/// Helpers for stepping through enums and colour palettes with wrap-around.
/// </summary>
public static class EnumCycleHelper
{
    /// <summary>
    /// Steps an enum value by the given delta, wrapping around at both ends.
    /// </summary>
    public static T Next<T>(T current, int delta) where T : struct, Enum
    {
        T[] values = Enum.GetValues<T>();
        int index = Array.IndexOf(values, current);
        return values[(index + delta + values.Length) % values.Length];
    }

    /// <summary>
    /// Steps a colour through the given palette by the given delta, wrapping around
    /// at both ends. Falls back to <paramref name="fallbackIndex"/> when the current
    /// colour isn't part of the palette.
    /// </summary>
    public static Color NextColor(Color[] palette, Color current, int delta, int fallbackIndex)
    {
        int index = Array.IndexOf(palette, current);
        if (index < 0)
        {
            index = fallbackIndex;
        }

        return palette[(index + delta + palette.Length) % palette.Length];
    }
}
