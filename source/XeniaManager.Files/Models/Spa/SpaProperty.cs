namespace XeniaManager.Files.Models.Spa;

/// <summary>
/// Property descriptor stored in the SPA's XPRP section (section 0x0001, id "XPRP").
/// </summary>
/// <remarks>
/// Fixed size 8 bytes, big-endian. Layout: id (4), string_id (2), data_size (2).
/// Follows a 12-byte section header (magic, version, size) and a 2-byte count.
/// </remarks>
public sealed class SpaProperty
{
    /// <summary>
    /// Gets or sets the property ID.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets or sets the string table ID for the property name.
    /// </summary>
    public ushort StringId { get; set; }

    /// <summary>
    /// Gets or sets the property data size.
    /// </summary>
    public ushort DataSize { get; set; }
}