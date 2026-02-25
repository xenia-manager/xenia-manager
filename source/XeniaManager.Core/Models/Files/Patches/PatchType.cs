namespace XeniaManager.Core.Models.Files.Patches;

/// <summary>
/// Represents the type of patch command.
/// Each type corresponds to a specific data format for the patch value.
/// </summary>
public enum PatchType
{
    /// <summary>
    /// Unknown or invalid patch type.
    /// </summary>
    Unknown,

    /// <summary>
    /// Big-endian 8-bit unsigned integer (1 byte).
    /// </summary>
    Be8,

    /// <summary>
    /// Big-endian 16-bit unsigned integer (2 bytes).
    /// </summary>
    Be16,

    /// <summary>
    /// Big-endian 32-bit unsigned integer (4 bytes).
    /// </summary>
    Be32,

    /// <summary>
    /// Big-endian 64-bit unsigned integer (8 bytes).
    /// </summary>
    Be64,

    /// <summary>
    /// Big-endian byte array of any size.
    /// </summary>
    Array,

    /// <summary>
    /// 32-bit floating point (single precision).
    /// </summary>
    F32,

    /// <summary>
    /// 64-bit floating point (double precision).
    /// </summary>
    F64,

    /// <summary>
    /// UTF-8 encoded string.
    /// </summary>
    String,

    /// <summary>
    /// UTF-16 encoded string.
    /// </summary>
    U16String
}