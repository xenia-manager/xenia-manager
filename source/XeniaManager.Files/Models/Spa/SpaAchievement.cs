namespace XeniaManager.Files.Models.Spa;

/// <summary>
/// Represents a single title achievement as stored in the SPA's XACH section.
/// </summary>
/// <remarks>
/// Fixed size 36 bytes (<c>0x24</c>), big-endian. Layout: id (2), labelId (2), descriptionId (2),
/// unachievedId (2), imageId (4), gamerscore (2), unkE (2), flags (4), unk14 (4), unk18 (4), unk1C (4), unk20 (4).
/// The <see cref="ImageId"/> references an entry in the image section (0x0002).
/// </remarks>
public sealed class SpaAchievement
{
    /// <summary>
    /// Gets or sets the achievement ID (unique per title, e.g., 0x0001).
    /// </summary>
    public ushort Id { get; set; }

    /// <summary>
    /// Gets or sets the string table ID for the achievement name.
    /// </summary>
    public ushort LabelId { get; set; }

    /// <summary>
    /// Gets or sets the string table ID for the achieved description.
    /// </summary>
    public ushort DescriptionId { get; set; }

    /// <summary>
    /// Gets or sets the string table ID for the unachieved description.
    /// </summary>
    public ushort UnachievedId { get; set; }

    /// <summary>
    /// Gets or sets the image entry ID for the achievement icon (references image section 0x0002).
    /// </summary>
    public uint ImageId { get; set; }

    /// <summary
    /// >Gets or sets the gamerscore value for this achievement.
    /// </summary>
    public ushort Gamerscore { get; set; }

    /// <summary>
    /// Gets or sets an unknown field (always 0 in observed samples).
    /// </summary>
    public ushort UnkE { get; set; }

    /// <summary>
    /// Gets or sets the achievement flags (visibility, type, etc.).
    /// </summary>
    public uint Flags { get; set; }

    /// <summary>
    /// Gets or sets an unknown field at offset 0x14.
    /// </summary>
    public uint Unk14 { get; set; }

    /// <summary>
    /// Gets or sets an unknown field at offset 0x18.
    /// </summary>
    public uint Unk18 { get; set; }

    /// <summary>
    /// Gets or sets an unknown field at offset 0x1C.
    /// </summary>
    public uint Unk1C { get; set; }

    /// <summary>
    /// Gets or sets an unknown field at offset 0x20.
    /// </summary>
    public uint Unk20 { get; set; }
}