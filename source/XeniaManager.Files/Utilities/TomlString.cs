using System.Globalization;
using System.Text;

namespace XeniaManager.Files.Utilities;

/// <summary>
/// Minimal TOML string parsing and encoding helpers used by patch files.
/// Supports basic strings, literal strings and their multiline variants.
/// </summary>
internal static class TomlString
{
    /// <summary>
    /// Parses the string value of a <c>key = value</c> assignment starting on
    /// <paramref name="lineIndex"/>. The line index is advanced when the value
    /// spans multiple lines.
    /// </summary>
    /// <returns>True when the assignment contains a TOML string, false otherwise.</returns>
    public static bool TryParseAssignedValue(string[] lines, ref int lineIndex, string assignment, out string value)
    {
        value = string.Empty;

        int equalsIndex = assignment.IndexOf('=');
        if (equalsIndex < 0)
        {
            return false;
        }

        string remainder = assignment[(equalsIndex + 1)..].TrimStart();
        if (remainder.StartsWith("\"\"\"", StringComparison.Ordinal))
        {
            return TryParseMultilineBasic(lines, ref lineIndex, remainder[3..], out value);
        }

        if (remainder.StartsWith("'''", StringComparison.Ordinal))
        {
            return TryParseMultilineLiteral(lines, ref lineIndex, remainder[3..], out value);
        }

        if (remainder.Length > 0 && remainder[0] == '"')
        {
            return TryParseBasic(remainder[1..], out value);
        }

        if (remainder.Length > 0 && remainder[0] == '\'')
        {
            return TryParseLiteral(remainder[1..], out value);
        }

        return false;
    }

    /// <summary>
    /// Encodes a value as a single-line TOML basic string.
    /// </summary>
    public static string Encode(string value)
    {
        StringBuilder builder = new StringBuilder(value.Length + 2);
        builder.Append('"');

        foreach (char c in value)
        {
            switch (c)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                default:
                    if (c < 0x20 || c == 0x7F)
                    {
                        builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(c);
                    }

                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }

    private static bool TryParseBasic(string text, out string value)
    {
        StringBuilder raw = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\\' && i + 1 < text.Length)
            {
                raw.Append(c).Append(text[++i]);
                continue;
            }

            if (c == '"')
            {
                value = DecodeBasic(raw.ToString(), false);
                return true;
            }

            raw.Append(c);
        }

        value = string.Empty;
        return false;
    }

    private static bool TryParseLiteral(string text, out string value)
    {
        int end = text.IndexOf('\'');
        if (end < 0)
        {
            value = string.Empty;
            return false;
        }

        value = text[..end];
        return true;
    }

    private static bool TryParseMultilineBasic(string[] lines, ref int lineIndex, string remainder, out string value)
    {
        StringBuilder raw = new StringBuilder();
        int currentLine = lineIndex;
        string text = remainder;

        // A newline immediately following the opening delimiter is trimmed.
        if (text.Length == 0)
        {
            currentLine++;
            if (currentLine >= lines.Length)
            {
                value = string.Empty;
                return false;
            }

            text = lines[currentLine];
        }

        while (true)
        {
            int end = FindMultilineBasicEnd(text);
            if (end >= 0)
            {
                raw.Append(text, 0, end);
                value = DecodeBasic(raw.ToString(), true);
                lineIndex = currentLine;
                return true;
            }

            raw.Append(text);
            currentLine++;
            if (currentLine >= lines.Length)
            {
                value = string.Empty;
                return false;
            }

            raw.Append('\n');
            text = lines[currentLine];
        }
    }

    private static bool TryParseMultilineLiteral(string[] lines, ref int lineIndex, string remainder, out string value)
    {
        StringBuilder builder = new StringBuilder();
        int currentLine = lineIndex;
        string text = remainder;

        // A newline immediately following the opening delimiter is trimmed.
        if (text.Length == 0)
        {
            currentLine++;
            if (currentLine >= lines.Length)
            {
                value = string.Empty;
                return false;
            }

            text = lines[currentLine];
        }

        while (true)
        {
            int end = text.IndexOf("'''", StringComparison.Ordinal);
            if (end >= 0)
            {
                builder.Append(text, 0, end);
                value = builder.ToString();
                lineIndex = currentLine;
                return true;
            }

            builder.Append(text);
            currentLine++;
            if (currentLine >= lines.Length)
            {
                value = string.Empty;
                return false;
            }

            builder.Append('\n');
            text = lines[currentLine];
        }
    }

    private static int FindMultilineBasicEnd(string text)
    {
        for (int i = 0; i + 2 < text.Length; i++)
        {
            if (text[i] != '"' || text[i + 1] != '"' || text[i + 2] != '"')
            {
                continue;
            }

            int backslashes = 0;
            for (int j = i - 1; j >= 0 && text[j] == '\\'; j--)
            {
                backslashes++;
            }

            if (backslashes % 2 == 0)
            {
                return i;
            }
        }

        return -1;
    }

    private static string DecodeBasic(string raw, bool allowLineContinuation)
    {
        StringBuilder builder = new StringBuilder(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (c != '\\')
            {
                builder.Append(c);
                continue;
            }

            if (i + 1 >= raw.Length)
            {
                builder.Append('\\');
                break;
            }

            char escape = raw[++i];
            switch (escape)
            {
                case '"':
                    builder.Append('"');
                    break;
                case '\\':
                    builder.Append('\\');
                    break;
                case 'b':
                    builder.Append('\b');
                    break;
                case 't':
                    builder.Append('\t');
                    break;
                case 'n':
                    builder.Append('\n');
                    break;
                case 'f':
                    builder.Append('\f');
                    break;
                case 'r':
                    builder.Append('\r');
                    break;
                case 'u':
                    if (TryReadHex(raw, ref i, 4, out int unicode))
                    {
                        builder.Append(char.ConvertFromUtf32(unicode));
                    }

                    break;
                case 'U':
                    if (TryReadHex(raw, ref i, 8, out int unicodeLong))
                    {
                        builder.Append(char.ConvertFromUtf32(unicodeLong));
                    }

                    break;
                case '\n' when allowLineContinuation:
                    while (i + 1 < raw.Length && char.IsWhiteSpace(raw[i + 1]))
                    {
                        i++;
                    }

                    break;
                default:
                    builder.Append(escape);
                    break;
            }
        }

        return builder.ToString();
    }

    private static bool TryReadHex(string raw, ref int index, int count, out int value)
    {
        value = 0;
        if (index + count >= raw.Length)
        {
            return false;
        }

        string hex = raw.Substring(index + 1, count);
        if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
        {
            return false;
        }

        index += count;
        return true;
    }
}