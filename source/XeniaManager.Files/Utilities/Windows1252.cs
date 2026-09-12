namespace XeniaManager.Files.Utilities;

/// <summary>
/// Windows-1252 encoding without the CodePages package.
/// Disc and package filenames on the console are stored in this encoding.
/// </summary>
internal static class Windows1252
{
    // Byte values 0x80-0x9F map to these Unicode characters; all other bytes map directly.
    private static readonly char[] SpecialChars =
    [
        '€', '\0', '‚', 'ƒ', '„', '…', '†', '‡', 'ˆ', '‰', 'Š', '‹', 'Œ', '\0', 'Ž', '\0',
        '\0', '‘', '’', '“', '”', '•', '–', '—', '˜', '™', 'š', '›', 'œ', '\0', 'ž', 'Ÿ'
    ];

    /// <summary>
    /// Decodes Windows-1252 bytes to a string. Undefined bytes decode to '\0' like the reference converter.
    /// </summary>
    public static string GetString(byte[] data) => GetString(data, 0, data.Length);

    /// <summary>
    /// Decodes Windows-1252 bytes to a string. Undefined bytes decode to '\0' like the reference converter.
    /// </summary>
    public static string GetString(byte[] data, int offset, int count)
    {
        char[] chars = new char[count];
        for (int i = 0; i < count; i++)
        {
            byte b = data[offset + i];
            chars[i] = b is >= 0x80 and <= 0x9F ? SpecialChars[b - 0x80] : (char)b;
        }

        return new string(chars);
    }

    /// <summary>
    /// Encodes a string to Windows-1252 bytes. Characters outside the encoding become '?'.
    /// </summary>
    public static byte[] GetBytes(string value)
    {
        byte[] bytes = new byte[value.Length];
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c < 0x80 || (c >= 0xA0 && c <= 0xFF))
            {
                bytes[i] = (byte)c;
                continue;
            }

            int special = Array.IndexOf(SpecialChars, c);
            bytes[i] = special >= 0 ? (byte)(special + 0x80) : (byte)'?';
        }

        return bytes;
    }
}