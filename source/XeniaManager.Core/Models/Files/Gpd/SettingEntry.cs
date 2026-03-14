using System.Buffers.Binary;
using System.Text;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Models.Files.Gpd;

/// <summary>
/// Represents a setting entry in a GPD file.
/// Settings store profile configuration data with various data types.
/// </summary>
public class SettingEntry
{
    /// <summary>
    /// Gets whether this setting entry is valid (successfully parsed).
    /// Invalid entries may be caused by corrupted data or non-standard formats.
    /// </summary>
    public bool IsValid { get; private set; } = true;

    /// <summary>
    /// Gets the error message if this entry is invalid.
    /// </summary>
    public string? ValidationError { get; private set; }

    /// <summary>
    /// Setting ID.
    /// Offset: 0x0, Length: 4 bytes
    /// </summary>
    public uint SettingId { get; set; }

    /// <summary>
    /// DOS Time (Last edited?).
    /// Offset: 0x4, Length: 2 bytes
    /// </summary>
    public ushort DosTime { get; set; }

    /// <summary>
    /// Unknown field.
    /// Offset: 0x6, Length: 2 bytes
    /// </summary>
    public ushort Unknown { get; set; }

    /// <summary>
    /// Data type of the setting.
    /// Offset: 0x8, Length: 1 byte
    /// </summary>
    public SettingDataType DataType { get; set; }

    /// <summary>
    /// Setting data (7 bytes of unknown/padding, then actual data).
    /// Offset: 0x9, Length: 7 bytes (unknown, always null?)
    /// Offset: 0x10, Variable length (depends on DataType)
    /// </summary>
    public byte[] Data { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets the parsed data value based on the DataType.
    /// </summary>
    public object? Value
    {
        get
        {
            if (Data.Length == 0)
            {
                return null;
            }

            return DataType switch
            {
                SettingDataType.Int32 => Data.Length >= 4 ? BinaryPrimitives.ReadInt32BigEndian(Data) : null,
                SettingDataType.Int64 => Data.Length >= 8 ? BinaryPrimitives.ReadInt64BigEndian(Data) : null,
                SettingDataType.Float => Data.Length >= 4 ? BitConverter.ToSingle(BitConverter.IsLittleEndian ? Data.Reverse().ToArray() : Data, 0) : null,
                SettingDataType.Double => Data.Length >= 8 ? BitConverter.ToDouble(BitConverter.IsLittleEndian ? Data.Reverse().ToArray() : Data, 0) : null,
                SettingDataType.DateTime => Data.Length >= 8 ? DateTime.FromFileTime(BinaryPrimitives.ReadInt64BigEndian(Data)) : null,
                SettingDataType.String or SettingDataType.Binary => ParseLengthPrefixedData(Data),
                SettingDataType.Context => Data.Length > 0 ? Data[0] : null,
                _ => null
            };
        }
    }

    /// <summary>
    /// Gets the string value if the DataType is String.
    /// </summary>
    public string? StringValue
    {
        get
        {
            if (DataType != SettingDataType.String || Data.Length < 4)
            {
                return null;
            }

            int length = BinaryPrimitives.ReadInt32BigEndian(Data);
            if (length <= 0 || length > Data.Length - 4)
            {
                return null;
            }

            return Encoding.BigEndianUnicode.GetString(Data, 4, length);
        }
    }

    /// <summary>
    /// Gets the binary value if the DataType is Binary.
    /// </summary>
    public byte[]? BinaryValue
    {
        get
        {
            if (DataType != SettingDataType.Binary || Data.Length < 4)
            {
                return null;
            }

            int length = BinaryPrimitives.ReadInt32BigEndian(Data);
            if (length <= 0 || length > Data.Length - 4)
            {
                return null;
            }

            byte[] result = new byte[length];
            Array.Copy(Data, 4, result, 0, length);
            return result;
        }
    }

    /// <summary>
    /// Sets the data from a string value.
    /// </summary>
    /// <param name="value">The string value to set.</param>
    public void SetStringValue(string value)
    {
        byte[] stringBytes = Encoding.BigEndianUnicode.GetBytes(value);
        Data = new byte[4 + stringBytes.Length];
        BinaryPrimitives.WriteInt32BigEndian(Data, stringBytes.Length);
        stringBytes.CopyTo(Data, 4);
        DataType = SettingDataType.String;
    }

    /// <summary>
    /// Sets the data from a binary value.
    /// </summary>
    /// <param name="value">The binary value to set.</param>
    public void SetBinaryValue(byte[] value)
    {
        Data = new byte[4 + value.Length];
        BinaryPrimitives.WriteInt32BigEndian(Data, value.Length);
        value.CopyTo(Data, 4);
        DataType = SettingDataType.Binary;
    }

    /// <summary>
    /// Sets the data from an Int32 value.
    /// </summary>
    /// <param name="value">The Int32 value to set.</param>
    public void SetInt32Value(int value)
    {
        Data = new byte[4];
        BinaryPrimitives.WriteInt32BigEndian(Data, value);
        DataType = SettingDataType.Int32;
    }

    /// <summary>
    /// Sets the data from an Int64 value.
    /// </summary>
    /// <param name="value">The Int64 value to set.</param>
    public void SetInt64Value(long value)
    {
        Data = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(Data, value);
        DataType = SettingDataType.Int64;
    }

    /// <summary>
    /// Parses a setting entry from raw bytes.
    /// If the data is invalid, returns an invalid entry instead of throwing.
    /// </summary>
    /// <param name="data">The raw byte data containing the setting entry.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <param name="length">The total length of the entry data.</param>
    /// <returns>The parsed SettingEntry (can be invalid if data is corrupted).</returns>
    public static SettingEntry FromBytes(byte[] data, int offset, uint length)
    {
        Logger.Trace<SettingEntry>($"Parsing setting entry from bytes at offset {offset}, length {length}");

        SettingEntry entry = new SettingEntry();

        try
        {
            if (data.Length < offset + 16)
            {
                Logger.Error<SettingEntry>($"Data too short for setting entry (expected 16, got {data.Length - offset})");
                entry.IsValid = false;
                entry.ValidationError = $"Data too short for setting entry (expected 16, got {data.Length - offset})";
                return entry;
            }

            entry.SettingId = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset));
            entry.DosTime = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset + 4));
            entry.Unknown = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset + 6));
            entry.DataType = (SettingDataType)data[offset + 8];

            // Data starts at offset + 16, length is (total length - 16)
            int dataLength = (int)length - 16;
            if (dataLength > 0)
            {
                entry.Data = new byte[dataLength];
                Array.Copy(data, offset + 16, entry.Data, 0, dataLength);
            }

            entry.IsValid = true;
            Logger.Debug<SettingEntry>($"Setting ID: 0x{entry.SettingId:X8}, Type: {entry.DataType}, Data Length: {entry.Data.Length}");

            if (entry.Value != null)
            {
                Logger.Debug<SettingEntry>($"Setting value: {entry.Value}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error<SettingEntry>($"Failed to parse setting entry: {ex.Message}");
            entry.IsValid = false;
            entry.ValidationError = $"Failed to parse setting entry: {ex.Message}";
        }

        return entry;
    }

    /// <summary>
    /// Converts the setting entry to a byte array.
    /// </summary>
    /// <returns>The setting entry as a byte array.</returns>
    public byte[] ToBytes()
    {
        Logger.Trace<SettingEntry>($"Converting setting (ID: 0x{SettingId:X8}) to bytes");

        byte[] data = new byte[16 + Data.Length];

        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x0), SettingId);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(0x4), DosTime);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(0x6), Unknown);
        data[8] = (byte)DataType;
        // Bytes 9-15 are unknown/padding (always null)
        Data.CopyTo(data, 16);

        Logger.Debug<SettingEntry>($"Setting converted to bytes ({data.Length} bytes)");
        return data;
    }

    /// <summary>
    /// Parses length-prefixed data (used for String and Binary types).
    /// </summary>
    private static object? ParseLengthPrefixedData(byte[] data)
    {
        if (data.Length < 4)
        {
            return null;
        }

        int length = BinaryPrimitives.ReadInt32BigEndian(data);
        if (length <= 0 || length > data.Length - 4)
        {
            return null;
        }

        byte[] valueData = new byte[length];
        Array.Copy(data, 4, valueData, 0, length);
        return valueData;
    }

    /// <summary>
    /// Returns a string representation of the setting.
    /// </summary>
    public override string ToString() => $"Setting 0x{SettingId:X8}: {DataType} = {Value}";
}