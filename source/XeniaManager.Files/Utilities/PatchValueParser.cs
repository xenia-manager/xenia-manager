using System.Globalization;
using XeniaManager.Files.Models.Patches;

namespace XeniaManager.Files.Utilities;

/// <summary>
/// Parses and validates the TOML value formats supported by Xenia patch
/// commands. This is the single source of truth for both patch-file parsing
/// and the patch configuration UI.
/// </summary>
public static class PatchValueParser
{
    /// <summary>
    /// Parses a TOML integer literal (decimal, hexadecimal, octal or binary,
    /// with optional sign and underscores) into a signed 64-bit value.
    /// </summary>
    public static bool TryParseIntegerLiteral(string text, out long value)
    {
        value = 0;
        string normalized = text.Trim().Replace("_", string.Empty);
        if (normalized.Length == 0)
        {
            return false;
        }

        bool negative = normalized[0] == '-';
        if (negative || normalized[0] == '+')
        {
            normalized = normalized[1..];
        }

        int numberBase = 10;
        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            numberBase = 16;
            normalized = normalized[2..];
        }
        else if (normalized.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
        {
            numberBase = 8;
            normalized = normalized[2..];
        }
        else if (normalized.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
        {
            numberBase = 2;
            normalized = normalized[2..];
        }

        ulong rawValue;
        try
        {
            rawValue = Convert.ToUInt64(normalized, numberBase);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or OverflowException)
        {
            return false;
        }

        long signedValue = unchecked((long)rawValue);
        value = negative ? unchecked(-signedValue) : signedValue;
        return true;
    }

    /// <summary>
    /// Parses a command value for the given patch type. String values are
    /// returned unchanged; array values are returned as bytes.
    /// </summary>
    public static bool TryParse(string text, PatchType type, out object? value)
    {
        value = null;

        if (type is PatchType.String or PatchType.U16String)
        {
            value = text;
            return true;
        }

        string trimmed = text.Trim();
        if (type is PatchType.Array)
        {
            if (!TryParseArray(trimmed, out byte[]? bytes, out _) || bytes == null)
            {
                return false;
            }

            value = bytes;
            return true;
        }

        if (type is PatchType.Be8 or PatchType.Be16 or PatchType.Be32 or PatchType.Be64)
        {
            if (!TryParseIntegerLiteral(trimmed, out long integerValue))
            {
                return false;
            }

            ulong rawValue = unchecked((ulong)integerValue);
            value = type switch
            {
                PatchType.Be8 => (byte)rawValue,
                PatchType.Be16 => (ushort)rawValue,
                PatchType.Be32 => (uint)rawValue,
                PatchType.Be64 => rawValue,
                _ => null
            };
            return value != null;
        }

        if (type is PatchType.F32 or PatchType.F64)
        {
            if (!TryParseFloat(trimmed, type == PatchType.F64, out value))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses a hex byte array (with or without a "0x" prefix). Xenia reads
    /// the final nibble of an odd-length value as one byte.
    /// </summary>
    public static bool TryParseArray(string text, out byte[]? bytes, out bool usedPrefix)
    {
        bytes = null;
        usedPrefix = false;
        string normalized = text.Trim();

        if (normalized.Length >= 2 &&
            ((normalized[0] == '"' && normalized[^1] == '"') ||
             (normalized[0] == '\'' && normalized[^1] == '\'')))
        {
            normalized = normalized[1..^1];
        }

        if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[2..];
            usedPrefix = true;
        }

        normalized = normalized.Trim();
        if (normalized.Length == 0)
        {
            bytes = [];
            return true;
        }

        int byteCount = (normalized.Length + 1) / 2;
        byte[] result = new byte[byteCount];
        for (int i = 0; i < byteCount; i++)
        {
            int start = i * 2;
            int length = Math.Min(2, normalized.Length - start);
            if (!byte.TryParse(normalized.AsSpan(start, length), NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out result[i]))
            {
                return false;
            }
        }

        bytes = result;
        return true;
    }

    /// <summary>
    /// Returns true when a numeric literal can be represented in TOML without
    /// exceeding the signed 64-bit integer range. TOML has no unsigned
    /// integers, so e.g. 0xFFFFFFFFFFFFFFFF must be written as -1.
    /// </summary>
    public static bool IsTomlRepresentableLiteral(string text, PatchType type)
    {
        if (type is not (PatchType.Be8 or PatchType.Be16 or PatchType.Be32 or PatchType.Be64 or
            PatchType.F32 or PatchType.F64))
        {
            return true;
        }

        string normalized = text.Trim().Replace("_", string.Empty);
        bool hasBasePrefix = normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
                             normalized.StartsWith("0o", StringComparison.OrdinalIgnoreCase) ||
                             normalized.StartsWith("0b", StringComparison.OrdinalIgnoreCase);

        if (hasBasePrefix)
        {
            // toml++ stores integers as int64. A prefixed literal with the
            // high bit set (or an explicit sign) is not representable.
            return !normalized.StartsWith('+') &&
                   !normalized.StartsWith('-') &&
                   TryParseIntegerLiteral(normalized, out long integerValue) &&
                   integerValue >= 0;
        }

        if (type is PatchType.F32 or PatchType.F64)
        {
            string lower = normalized.ToLowerInvariant();
            if (lower is "inf" or "+inf" or "-inf" or "nan" or "+nan" or "-nan")
            {
                return true;
            }

            return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
        }

        return long.TryParse(normalized, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _);
    }

    private static bool TryParseFloat(string text, bool asDouble, out object? value)
    {
        value = null;
        string normalized = text.Replace("_", string.Empty);
        if (normalized.Length == 0)
        {
            return false;
        }

        if (TryParseIntegerLiteral(normalized, out long integerValue))
        {
            value = asDouble ? (double)integerValue : (object)(float)integerValue;
            return true;
        }

        string lower = normalized.ToLowerInvariant();
        if (lower is "inf" or "+inf")
        {
            value = asDouble ? double.PositiveInfinity : (object)float.PositiveInfinity;
            return true;
        }

        if (lower == "-inf")
        {
            value = asDouble ? double.NegativeInfinity : (object)float.NegativeInfinity;
            return true;
        }

        if (lower is "nan" or "+nan" or "-nan")
        {
            value = asDouble ? double.NaN : (object)float.NaN;
            return true;
        }

        if (asDouble)
        {
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
            {
                value = doubleValue;
                return true;
            }

            return false;
        }

        if (float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
        {
            value = floatValue;
            return true;
        }

        return false;
    }
}