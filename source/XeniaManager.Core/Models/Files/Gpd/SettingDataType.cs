namespace XeniaManager.Core.Models.Files.Gpd;

/// <summary>
/// Represents the data types used in Setting entries.
/// Settings can store various types of data, each with a specific format.
/// </summary>
public enum SettingDataType : byte
{
    /// <summary>
    /// Context data type.
    /// </summary>
    Context = 0,

    /// <summary>
    /// 32-bit signed integer.
    /// </summary>
    Int32 = 1,

    /// <summary>
    /// 64-bit signed integer.
    /// </summary>
    Int64 = 2,

    /// <summary>
    /// Double-precision floating-point number.
    /// </summary>
    Double = 3,

    /// <summary>
    /// Unicode string (with leading Int32 length prefix).
    /// </summary>
    String = 4,

    /// <summary>
    /// Single-precision floating-point number.
    /// </summary>
    Float = 5,

    /// <summary>
    /// Binary data (with leading Int32 length prefix).
    /// </summary>
    Binary = 6,

    /// <summary>
    /// DateTime value (file time format).
    /// </summary>
    DateTime = 7,

    /// <summary>
    /// Null/empty data type.
    /// </summary>
    Null = 0xFF
}
