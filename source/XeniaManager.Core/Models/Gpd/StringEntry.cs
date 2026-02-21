using System.Text;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Models.Gpd;

/// <summary>
/// Represents a string entry in a GPD file.
/// String entries are simply null-terminated Unicode strings.
/// The length can be derived from the entry length.
/// </summary>
public class StringEntry
{
    /// <summary>
    /// Gets whether this string entry is valid (successfully parsed).
    /// </summary>
    public bool IsValid { get; private set; } = true;

    /// <summary>
    /// Gets the error message if this entry is invalid.
    /// </summary>
    public string? ValidationError { get; private set; }

    /// <summary>
    /// The string value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Creates a new StringEntry from a string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>A new StringEntry instance.</returns>
    public static StringEntry FromString(string value)
    {
        return new StringEntry { Value = value };
    }

    /// <summary>
    /// Creates a new StringEntry from raw entry data.
    /// If the data is invalid, returns an invalid entry instead of throwing.
    /// </summary>
    /// <param name="data">The raw byte data containing the string entry.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <param name="length">The total length of the entry data.</param>
    /// <returns>The parsed StringEntry (may be invalid if data is corrupted).</returns>
    public static StringEntry FromBytes(byte[] data, int offset, uint length)
    {
        Logger.Trace<StringEntry>($"Parsing string entry from bytes at offset {offset}, length {length}");

        StringEntry entry = new StringEntry();

        try
        {
            if (data.Length < offset + length)
            {
                Logger.Error<StringEntry>($"Data too short for string entry (expected {length}, got {data.Length - offset})");
                entry.IsValid = false;
                entry.ValidationError = $"Data too short for string entry (expected {length}, got {data.Length - offset})";
                return entry;
            }

            // Read null-terminated Unicode string
            int stringEnd = offset;
            while (stringEnd < offset + length - 1)
            {
                if (data[stringEnd] == 0 && data[stringEnd + 1] == 0)
                {
                    break;
                }
                stringEnd += 2;
            }

            entry.Value = Encoding.BigEndianUnicode.GetString(data, offset, stringEnd - offset);
            entry.IsValid = true;

            Logger.Debug<StringEntry>($"String entry parsed: '{entry.Value}'");
        }
        catch (Exception ex)
        {
            Logger.Error<StringEntry>($"Failed to parse string entry: {ex.Message}");
            entry.IsValid = false;
            entry.ValidationError = $"Failed to parse string entry: {ex.Message}";
        }

        return entry;
    }

    /// <summary>
    /// Converts the string entry to a byte array.
    /// </summary>
    /// <returns>The string entry as a byte array (null-terminated).</returns>
    public byte[] ToBytes()
    {
        Logger.Trace<StringEntry>($"Converting string entry to bytes: '{Value}'");

        byte[] stringBytes = Encoding.BigEndianUnicode.GetBytes(Value);
        byte[] result = new byte[stringBytes.Length + 2]; // +2 for null terminator
        stringBytes.CopyTo(result, 0);
        // Null terminator (0x0000) is already zero-initialized

        Logger.Debug<StringEntry>($"String converted to bytes ({result.Length} bytes)");
        return result;
    }

    /// <summary>
    /// Returns a string representation of the string entry.
    /// </summary>
    public override string ToString() => Value.Length > 50 ? Value.Substring(0, 47) + "..." : Value;
}