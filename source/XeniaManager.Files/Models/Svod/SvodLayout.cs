namespace XeniaManager.Files.Models.Svod;

/// <summary>
/// Describes the on-disk layout of an SVOD (GOD) container and determines how logical GDFX sectors
/// are translated to physical byte offsets in the data files.
/// </summary>
/// <remarks>
/// <para>
/// SVOD (Game on Demand) reuses the STFS hash-table format but splits the hashed payload across
/// multiple <c>Data####</c> fragments. The translation depends on the container's layout, which is
/// inferred from the volume descriptor and the host file set.
/// </para>
/// <para>
/// Layouts:
/// <list type="bullet">
/// <item><see cref="EnhancedGdf"/> – <c>IsEnhancedGdfLayout</c> (<c>0x40</c>) set; GDFX magic at <c>0x2000</c> raw, sector offset <c>0x2000</c>, <c>trueBlock += 2</c>.</item>
/// <item><see cref="Xsf"/> – third-party XSF header (<c>"XSF"</c> at <c>0x2000</c>) with <c>MICROSOFT*XBOX*MEDIA</c> at <c>0x12000</c> raw; data still hashed with normal offset <c>0x1000</c>.</item>
/// <item><see cref="SingleFile"/> – single fragment (<c>header</c> only, no <c>.data</c> dir); GDFX at <c>0xD000</c> raw (<c>0xB000</c> header + <c>0x2000</c> hash), base <c>0xB000</c> added in <c>BlockToOffset</c>.</item>
/// <item><see cref="MultipleFiles"/> – standard GOD with <c>Data####</c> fragments; GDFX at <c>0x2000</c> raw (or <c>0x12000</c> for Velocity compatibility) and hashed with level-0/level-1 tables.</item>
/// </list>
/// </para>
/// </remarks>
public enum SvodLayout
{
    /// <summary>
    /// Layout could not be determined or has not been probed yet.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Enhanced GDF layout (<c>IsEnhancedGdfLayout</c> set). Uses an extra 2-block offset for every sector and a <c>0x2000</c> sector offset.
    /// </summary>
    EnhancedGdf = 1,

    /// <summary>
    /// XSF layout produced by third-party tools. Detected by <c>"XSF"</c> at <c>0x2000</c> and <c>MICROSOFT*XBOX*MEDIA</c> at <c>0x12000</c> raw in the first data file.
    /// Uses normal hashing with base <c>0x10000</c> for the GDFX header only; data sectors use normal <c>0x1000</c> offset.
    /// </summary>
    Xsf = 2,

    /// <summary>
    /// Single-file GOD (header file also contains the hashed payload). GDFX at <c>0xD000</c> raw, base <c>0xB000</c> is added to every <c>BlockToOffset</c> result.
    /// </summary>
    SingleFile = 3,

    /// <summary>
    /// Standard multi-file GOD (<c>Data####</c> fragments). GDFX at <c>0x2000</c> raw (with Velocity fallback to <c>0x12000</c>), no base offset for data.
    /// </summary>
    MultipleFiles = 4
}