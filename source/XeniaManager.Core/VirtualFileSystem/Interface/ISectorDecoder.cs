using XeniaManager.Core.VirtualFileSystem.Models;

namespace XeniaManager.Core.VirtualFileSystem.Interface;

/// <summary>
/// Defines the contract for a sector decoder, providing essential methods for
/// reading and decoding sectors as part of a virtual file system.
/// </summary>
public interface ISectorDecoder : IDisposable
{
    /// Initializes the sector decoder by searching for and validating a valid XGD image header
    /// within predefined sector ranges in the file system. If a valid header is found,
    /// the decoder configures necessary information, such as the base sector, root directory sector,
    /// root directory size, and creation date of the XGD image.
    /// <returns>
    /// true if the initialization is successful and a valid XGD image header is detected;
    /// otherwise, false.
    /// </returns>
    public bool Init();
    
    /// <summary>
    /// Retrieves information about the XGD format, including details about the
    /// base sector, root directory sector, root directory size, and creation date/time.
    /// </summary>
    /// <returns>
    /// An instance of <see cref="XgdInfo"/> containing XGD-related information.
    /// </returns>
    public XgdInfo GetXgdInfo();
    
    /// <summary>
    /// Retrieves the total number of sectors available.
    /// </summary>
    /// <returns>The total number of sectors as an unsigned integer.</returns>
    public uint TotalSectors();
    
    /// Retrieves the size of a sector in an Xbox Game Disc (XGD) format.
    /// This size is constant and typically used in sector-based operations
    /// within the virtual file system or for handling XGD-format data.
    /// <returns>
    /// The sector size in an XGD format, represented as a 32-bit unsigned integer.
    /// </returns>
    public uint SectorSize();
    
    /// <summary>
    /// Attempts to read the data from a specific sector in the file system.
    /// </summary>
    /// <param name="sector">The sector number from which data is to be read.</param>
    /// <param name="sectorData">
    /// An output parameter that contains the data of the sector if the operation is successful.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the operation was successful. Returns true if the sector
    /// was read successfully; otherwise, false.
    /// </returns>
    public bool TryReadSector(long sector, out byte[] sectorData);
}