using System.Globalization;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Models.Files.Patches;

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

    /// <summary>
    /// The value to write at the address.
    /// The type depends on the patch type:
    /// - Be8, Be16, Be32, Be64, Array: uint, ushort, ulong, or byte[]
    /// - F32, F64: float or double
    /// - String, U16String: string
    /// </summary>
    public object? Value { get; set; }

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

    /// <summary>
    /// Optional inline comment from the value line.
    /// </summary>
    public string? ValueComment { get; set; }

    /// <summary>
    /// For Array type: tracks whether the original format used "0x" prefix.
    /// null = default behavior (use 0x prefix), true = use prefix, false = no prefix.
    /// </summary>
    public bool? UseArrayPrefix { get; set; }

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
        return Value switch
        {
            byte byteValue => $"0x{byteValue:x2}",
            ushort shortValue => $"0x{shortValue:x4}",
            uint uIntValue => $"0x{uIntValue:x8}",
            ulong longValue => $"0x{longValue:x16}",
            byte[] byteArrayValue => FormatArrayValue(byteArrayValue),
            float floatValue => floatValue.ToString(CultureInfo.InvariantCulture),
            double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
            string stringValue => $"\"{stringValue}\"",
            null => null,
            _ => Value.ToString()
        };
    }

    /// <summary>
    /// Formats array value preserving original prefix format.
    /// </summary>
    private string FormatArrayValue(byte[] bytes)
    {
        string hex = BitConverter.ToString(bytes).Replace("-", "").ToLower();
        bool usePrefix = UseArrayPrefix ?? true;
        Logger.Debug<PatchCommand>($"FormatArrayValue: UseArrayPrefix={UseArrayPrefix}, usePrefix={usePrefix}");
        return usePrefix ? $"\"0x{hex}\"" : $"\"{hex}\"";
    }
}