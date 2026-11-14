using System.Globalization;
using XeniaManager.Files.Utilities;

namespace XeniaManager.Files.Models.Patches;

/// <summary>
/// Represents a single patch command with an address and value.
/// The type of patch determines the format of the value.
/// </summary>
public class PatchCommand
{
    /// <summary>
    /// The memory address to patch (typically starts with 0x8, can be up to 8 bytes).
    /// Stored as lowercase hex in TOML.
    /// </summary>
    public ulong Address { get; set; }

    private object? _value;

    /// <summary>
    /// The value to write at the address.
    /// The type depends on the patch type:
    /// - Be8, Be16, Be32, Be64, Array: uint, ushort, ulong, or byte[]
    /// - F32, F64: float or double
    /// - String, U16String: string
    /// </summary>
    public object? Value
    {
        get
        {
            return _value;
        }

        set
        {
            if (!Equals(_value, value))
            {
                _value = value;
                RawValue = null;
                RawValueLineSuffix = null;
            }
        }
    }

    /// <summary>
    /// Original TOML literal for the value. When set, it is written back
    /// verbatim so untouched numeric values keep their precision and formatting.
    /// Cleared automatically when <see cref="Value"/> changes.
    /// </summary>
    public string? RawValue { get; set; }

    /// <summary>
    /// Exact text after "value = " from the source line, including the inline
    /// comment and its spacing. Used to preserve untouched value lines verbatim.
    /// Cleared automatically when <see cref="Value"/> changes.
    /// </summary>
    public string? RawValueLineSuffix { get; set; }

    /// <summary>
    /// The type of this patch command.
    /// </summary>
    public PatchType Type { get; set; }

    /// <summary>
    /// Optional inline comment from the [[patch.type]] line.
    /// </summary>
    public string? TypeComment { get; set; }

    /// <summary>
    /// Optional inline comment from the address line.
    /// </summary>
    public string? AddressComment { get; set; }

    private string? _valueComment;
    private bool? _useArrayPrefix;

    /// <summary>
    /// Optional inline comment from the value line. Changing it invalidates
    /// the preserved value line so the new comment is written.
    /// </summary>
    public string? ValueComment
    {
        get
        {
            return _valueComment;
        }

        set
        {
            if (!string.Equals(_valueComment, value, StringComparison.Ordinal))
            {
                _valueComment = value;
                RawValueLineSuffix = null;
            }
        }
    }

    /// <summary>
    /// For Array type: tracks whether the original format used "0x" prefix.
    /// null = default behavior (use 0x prefix), true = use prefix, false = no prefix.
    /// Changing it invalidates the preserved value line.
    /// </summary>
    public bool? UseArrayPrefix
    {
        get
        {
            return _useArrayPrefix;
        }

        set
        {
            if (_useArrayPrefix != value)
            {
                _useArrayPrefix = value;
                RawValueLineSuffix = null;
            }
        }
    }

    /// <summary>
    /// Standalone comment lines before this [[patch.type]] table.
    /// </summary>
    public List<string> LeadingComments { get; set; } = [];

    /// <summary>
    /// Standalone comment lines between the address and value lines, such as
    /// commented-out alternative values.
    /// </summary>
    public List<string> ValueLeadingComments { get; set; } = [];

    /// <summary>
    /// Creates a new instance of PatchCommand.
    /// </summary>
    public PatchCommand()
    {
    }

    /// <summary>
    /// Creates a new instance of PatchCommand with the specified parameters.
    /// </summary>
    /// <param name="address">The memory address to patch.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="type">The type of patch.</param>
    public PatchCommand(ulong address, object? value, PatchType type)
    {
        Address = address;
        Value = value;
        Type = type;
    }

    /// <summary>
    /// Converts the value to a string representation suitable for TOML output.
    /// Hex values are lowercase, strings are quoted. Floats use invariant culture.
    /// Array values preserve the original format's "0x" prefix if present.
    /// </summary>
    /// <returns>The string representation of the value.</returns>
    public string? GetValueAsString()
    {
        if (!string.IsNullOrEmpty(RawValue))
        {
            return RawValue;
        }

        return Value switch
        {
            byte byteValue => $"0x{byteValue:x2}",
            ushort shortValue => $"0x{shortValue:x4}",
            uint uIntValue => $"0x{uIntValue:x8}",
            // TOML integers are signed 64-bit. Values with the high bit set
            // must be written as negative decimals; Xenia casts the signed
            // value back to the target width.
            ulong longValue when longValue > long.MaxValue =>
                unchecked((long)longValue).ToString(CultureInfo.InvariantCulture),
            ulong longValue => $"0x{longValue:x16}",
            byte[] byteArrayValue => FormatArrayValue(byteArrayValue),
            float floatValue => FormatFloat(floatValue),
            double doubleValue => FormatDouble(doubleValue),
            string stringValue => TomlString.Encode(stringValue),
            null => null,
            _ => Value.ToString()
        };
    }

    private static string FormatFloat(float value)
    {
        if (float.IsNaN(value))
        {
            return "nan";
        }

        if (float.IsPositiveInfinity(value))
        {
            return "inf";
        }

        if (float.IsNegativeInfinity(value))
        {
            return "-inf";
        }

        if (value == 0 && float.IsNegative(value))
        {
            return "-0.0";
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatDouble(double value)
    {
        if (double.IsNaN(value))
        {
            return "nan";
        }

        if (double.IsPositiveInfinity(value))
        {
            return "inf";
        }

        if (double.IsNegativeInfinity(value))
        {
            return "-inf";
        }

        if (value == 0 && double.IsNegative(value))
        {
            return "-0.0";
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats array value preserving original prefix format.
    /// </summary>
    private string FormatArrayValue(byte[] bytes)
    {
        string hex = Convert.ToHexString(bytes).ToLowerInvariant();
        bool usePrefix = UseArrayPrefix ?? true;
        return usePrefix ? $"\"0x{hex}\"" : $"\"{hex}\"";
    }
}