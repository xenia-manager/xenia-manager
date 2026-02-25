namespace XeniaManager.Core.Models.Iso;

/// <summary>
/// Internal structure holding information about an ISO file slice.
/// </summary>
internal struct IsoDetail
{
    /// <summary>
    /// The file stream for this ISO slice.
    /// </summary>
    public Stream Stream;

    /// <summary>
    /// The starting sector of this slice.
    /// </summary>
    public long StartSector;

    /// <summary>
    /// The ending sector of this slice.
    /// </summary>
    public long EndSector;
}