namespace XeniaManager.Core.Models.Files.Config;

/// <summary>
/// Represents the type of configuration option value.
/// </summary>
public enum ConfigOptionType
{
    /// <summary>
    /// Unknown or unspecified type.
    /// </summary>
    Unknown,

    /// <summary>
    /// Boolean value (true/false).
    /// </summary>
    Boolean,

    /// <summary>
    /// Integer value.
    /// </summary>
    Integer,

    /// <summary>
    /// Floating-point value.
    /// </summary>
    Float,

    /// <summary>
    /// String value.
    /// </summary>
    String,

    /// <summary>
    /// Array value.
    /// </summary>
    Array
}