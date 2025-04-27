namespace XeniaManager.Core.VirtualFileSystem.Models;

/// <summary>
/// Represents detailed information about a portion of an ISO file, including its associated data stream
/// and the range of sectors it covers within the ISO.
/// </summary>
public class IsoDetail
{
    /// <summary>
    /// Represents the stream associated with an ISO file slice.
    /// </summary>
    /// <remarks>
    /// This property holds a reference to a file stream that corresponds to a specific slice
    /// of an ISO file. It is used to manage read operations for specific sectors within the
    /// slice, ensuring efficient access to ISO data during decoding processes.
    /// </remarks>
    public Stream Stream;

    /// <summary>
    /// Represents the starting sector index in the ISO file. This value indicates the sector at which
    /// the corresponding stream begins within the file and is used for mapping file slices to their respective sectors.
    /// </summary>
    /// <remarks>
    /// The StartSector value is assigned incrementally based on the cumulative sector count of the slices
    /// during initialization in the mounting process. It serves as a key parameter in identifying the location
    /// of data for reading operations in the file.
    /// </remarks>
    public long StartSector;

    /// <summary>
    /// Represents the ending sector for a specific ISO segment within a collection of ISO details.
    /// </summary>
    /// <remarks>
    /// This variable is used to determine the boundary of a particular ISO file slice within a sequence of slices.
    /// It is calculated as the sum of the starting sector and the number of sectors in the ISO slice minus one.
    /// </remarks>
    public long EndSector;
}