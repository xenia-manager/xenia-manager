namespace XeniaManager.Files.Models.Spa;

/// <summary>
/// Stats view field descriptor stored in the SPA's XVC2 section (section 0x0001, id "XVC2").
/// </summary>
/// <remarks>
/// Fixed size 32 bytes (0x20), big-endian. Layout: size (4), property_id (4),
/// flags (4), attribute_id (2), string_id (2), aggregation_type (2), ordinal (1),
/// field_type (1), format_type (4), unused (8).
/// </remarks>
public sealed class SpaViewField
{
    /// <summary>
    /// Gets or sets the field size.
    /// </summary>
    public uint Size { get; set; }

    /// <summary>
    /// Gets or sets the property ID.
    /// </summary>
    public uint PropertyId { get; set; }

    /// <summary>
    /// Gets or sets the field flags.
    /// </summary>
    public uint Flags { get; set; }

    /// <summary>
    /// Gets or sets the attribute ID.
    /// </summary>
    public ushort AttributeId { get; set; }

    /// <summary>
    /// Gets or sets the string table ID for the field name.
    /// </summary>
    public ushort StringId { get; set; }

    /// <summary>
    /// Gets or sets the aggregation type.
    /// </summary>
    public ushort AggregationType { get; set; }

    /// <summary>
    /// Gets or sets the ordinal.
    /// </summary>
    public byte Ordinal { get; set; }

    /// <summary>
    /// Gets or sets the field type (context or property field).
    /// </summary>
    public byte FieldType { get; set; }

    /// <summary>
    /// Gets or sets the format type.
    /// </summary>
    public uint FormatType { get; set; }
}