namespace XeniaManager.Files.Models.Spa;

/// <summary>
/// Context descriptor stored in the SPA's XCTX section (section 0x0001, id "XCTX").
/// </summary>
/// <remarks>
/// Fixed size 16 bytes, big-endian. Layout: id (4), unk1 (2), string_id (2),
/// max_value (4), default_value (4). Follows a 12-byte section header
/// (magic, version, size) and a 4-byte count.
/// </remarks>
public sealed class SpaContext
{
    /// <summary>
    /// Gets or sets the context ID.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets or sets the unknown field at offset 4.
    /// </summary>
    public ushort Unk1 { get; set; }

    /// <summary>
    /// Gets or sets the string table ID for the context name.
    /// </summary>
    public ushort StringId { get; set; }

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public uint MaxValue { get; set; }

    /// <summary>
    /// Gets or sets the default value.
    /// </summary>
    public uint DefaultValue { get; set; }
}