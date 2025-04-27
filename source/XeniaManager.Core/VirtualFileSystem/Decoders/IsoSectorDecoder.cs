using XeniaManager.Core.VirtualFileSystem.Interface;
using XeniaManager.Core.VirtualFileSystem.Models;

namespace XeniaManager.Core.VirtualFileSystem.Decoders;

/// <summary>
/// The <c>IsoSectorDecoder</c> class is responsible for decoding ISO file sectors.
/// It provides functionality for managing ISO sector details and allows reading sectors
/// based on a specified logical sector index. This class is thread-safe and applies
/// internal synchronization mechanisms to ensure proper handling of concurrent access.
/// </summary>
/// <remarks>
/// <c>IsoSectorDecoder</c> is an implementation of the abstract <c>SectorDecoder</c> class.
/// It uses ISO-specific details to interpret data sectors within a given ISO file structure.
/// </remarks>
public class IsoSectorDecoder : SectorDecoder
{
    /// <summary>
    /// Represents an array containing details of various ISO sectors to be used by the ISO sector decoder.
    /// Each item in the array provides information about the sector's start position and end position,
    /// as well as the stream for reading sector data.
    /// </summary>
    private readonly IsoDetail[] _isoDetails;

    /// <summary>
    /// A synchronization object used to ensure thread safety
    /// while accessing or modifying shared resources within the class.
    /// </summary>
    /// <remarks>
    /// This mutex is primarily used in conjunction with lock statements
    /// to protect critical sections of code that involve the processing
    /// of ISO sector data in a multithreaded environment. It prevents
    /// race conditions when multiple threads attempt to access or modify
    /// the same set of resources simultaneously.
    /// </remarks>
    private readonly object _mutex;

    /// <summary>
    /// A boolean flag indicating whether the object has already been disposed of.
    /// </summary>
    /// <remarks>
    /// This variable is used to ensure that resources are not disposed of multiple times,
    /// which could lead to undefined behavior or exceptions. It is set to <c>true</c>
    /// when the object's <see cref="Dispose"/> method is called.
    /// </remarks>
    private bool _disposed;

    /// Decodes sectors of an ISO container file and provides access to its data.
    /// The IsoSectorDecoder class processes ISO sectors based on their structure and mappings
    /// provided by the IsoDetail collection. It offers methods to retrieve the total number of sectors
    /// and to attempt to read specific sector data.
    /// This class is designed to be thread-safe through the use of synchronization mechanisms during
    /// sector access and modification.
    /// Inherits from the SectorDecoder abstract base class.
    public IsoSectorDecoder(IsoDetail[] isoDetails)
    {
        _isoDetails = isoDetails;
        _mutex = new object();
        _disposed = false;
    }

    /// <summary>
    /// Returns the total number of sectors available in the ISO, determined by the highest `EndSector` value among the provided ISO details.
    /// </summary>
    /// <returns>The total number of sectors as an unsigned integer.</returns>
    public override uint TotalSectors()
    {
        return (uint)(_isoDetails.Max(iso => iso.EndSector) + 1);
    }
    /// <summary>
    /// Attempts to read a specific sector from the ISO file and retrieve its data.
    /// </summary>
    /// <param name="sector">The sector number to be read.</param>
    /// <param name="sectorData">
    /// The output parameter that will contain the data of the requested sector
    /// if the method succeeds.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the sector was successfully read.
    /// Returns <c>true</c> if the sector was read successfully; otherwise, <c>false</c>.
    /// </returns>
    public override bool TryReadSector(long sector, out byte[] sectorData)
    {
        sectorData = new byte[Constants.XGD_SECTOR_SIZE];
        lock (_mutex)
        {
            foreach (IsoDetail isoDetail in _isoDetails)
            {
                if (sector >= isoDetail.StartSector && sector <= isoDetail.EndSector)
                {
                    isoDetail.Stream.Position = (sector - isoDetail.StartSector) * Constants.XGD_SECTOR_SIZE;
                    int bytesRead = isoDetail.Stream.Read(sectorData, 0, (int)Constants.XGD_SECTOR_SIZE);
                    return bytesRead == Constants.XGD_SECTOR_SIZE;
                }
            }
            return false;
        }
    }
}