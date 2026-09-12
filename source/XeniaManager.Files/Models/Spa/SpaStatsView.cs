namespace XeniaManager.Files.Models.Spa;

/// <summary>
/// Stats view stored in the SPA's XVC2 section (section 0x0001, id "XVC2").
/// </summary>
/// <remarks>
/// Each view has a 16-byte table entry (id, flags, shared_index, string_id)
/// referencing a shared view: column/row field lists plus a property bag.
/// </remarks>
public sealed class SpaStatsView
{
    /// <summary>
    /// Gets or sets the view ID.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// Gets or sets the view flags (view type in the low 4 bits).
    /// </summary>
    public uint Flags { get; set; }

    /// <summary>
    /// Gets or sets the shared view index referenced by this view.
    /// </summary>
    public ushort SharedIndex { get; set; }

    /// <summary>
    /// Gets or sets the string table ID for the view name.
    /// </summary>
    public ushort StringId { get; set; }

    /// <summary>
    /// Gets the column field entries.
    /// </summary>
    public List<SpaViewField> Columns { get; } = [];

    /// <summary>
    /// Gets the row field entries.
    /// </summary>
    public List<SpaViewField> Rows { get; } = [];

    /// <summary>
    /// Gets the shared view's property bag.
    /// </summary>
    public SpaPropertyBag PropertyBag { get; } = new SpaPropertyBag();
}