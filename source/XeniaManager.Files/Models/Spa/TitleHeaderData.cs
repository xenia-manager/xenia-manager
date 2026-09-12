namespace XeniaManager.Files.Models.Spa;

/// <summary>
/// Title header data stored in the SPA's XTHD section (section 0x0001, id "XTHD").
/// </summary>
/// <remarks>
/// Fixed size 32 bytes, big-endian. Layout: title_id (4), title_type (4), major (2),
/// minor (2), build (2), revision (2), flags (4), padding (12).
/// Follows a 12-byte section header (magic, version, size).
/// </remarks>
public sealed class TitleHeaderData
{
    /// <summary>
    /// Gets or sets the title ID.
    /// </summary>
    public uint TitleId { get; set; }

    /// <summary>
    /// Gets or sets the title type (system, full, demo, download, app).
    /// </summary>
    public TitleType TitleType { get; set; } = TitleType.Unknown;

    /// <summary>
    /// Gets or sets the major version.
    /// </summary>
    public ushort Major { get; set; }

    /// <summary>
    /// Gets or sets the minor version.
    /// </summary>
    public ushort Minor { get; set; }

    /// <summary>
    /// Gets or sets the build version.
    /// </summary>
    public ushort Build { get; set; }

    /// <summary>
    /// Gets or sets the revision version.
    /// </summary>
    public ushort Revision { get; set; }

    /// <summary>
    /// Gets or sets the title flags (profile inclusion hints).
    /// </summary>
    public uint Flags { get; set; }
}