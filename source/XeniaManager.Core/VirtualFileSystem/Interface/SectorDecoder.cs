using XeniaManager.Core.VirtualFileSystem.Models;

namespace XeniaManager.Core.VirtualFileSystem.Interface;

/// <summary>
/// Represents an abstract base class for reading and decoding sectors within a file system
/// container. Provides methods for initializing, retrieving sector details, and accessing
/// XGD information about the file system.
/// </summary>
public abstract class SectorDecoder : ISectorDecoder
{
    /// Initializes the sector decoder by attempting to locate and validate
    /// a valid XGD image header within specified sector ranges in the file system.
    /// If a valid header is identified, the decoder sets necessary information
    /// including the base sector, root directory sector, and creation date.
    /// <returns>
    /// Returns true if a valid XGD image header is found and initialization succeeds;
    /// otherwise, returns false.
    /// </returns>
    public bool Init()
    {
        bool found = false;
        uint baseSector = 0U;
        XgdHeader header = new XgdHeader();

        if (TotalSectors() >= Constants.XGD_MAGIC_SECTOR_XDKI)
        {
            if (TryReadSector(Constants.XGD_MAGIC_SECTOR_XDKI, out byte[] sector))
            {
                header = Helpers.GetXgdHeader(sector);
                if (header != null && Helpers.GetUtf8String(header.Magic).Equals(Constants.XGD_IMAGE_MAGIC) && Helpers.GetUtf8String(header.MagicTail).Equals(Constants.XGD_IMAGE_MAGIC))
                {
                    baseSector = Constants.XGD_MAGIC_SECTOR_XDKI - Constants.XGD_ISO_BASE_SECTOR;
                    found = true;
                }
            }
        }

        if (!found && TotalSectors() >= Constants.XGD_MAGIC_SECTOR_XGD1)
        {
            if (TryReadSector(Constants.XGD_MAGIC_SECTOR_XGD1, out byte[] sector))
            {
                header = Helpers.GetXgdHeader(sector);
                if (header != null && Helpers.GetUtf8String(header.Magic).Equals(Constants.XGD_IMAGE_MAGIC) && Helpers.GetUtf8String(header.MagicTail).Equals(Constants.XGD_IMAGE_MAGIC))
                {
                    baseSector = Constants.XGD_MAGIC_SECTOR_XGD1 - Constants.XGD_ISO_BASE_SECTOR;
                    found = true;
                }
            }
        }
        
        if (!found && TotalSectors() >= Constants.XGD_MAGIC_SECTOR_XGD2)
        {
            if (TryReadSector(Constants.XGD_MAGIC_SECTOR_XGD2, out byte[] sector))
            {
                header = Helpers.GetXgdHeader(sector);
                if (header != null && Helpers.GetUtf8String(header.Magic).Equals(Constants.XGD_IMAGE_MAGIC) && Helpers.GetUtf8String(header.MagicTail).Equals(Constants.XGD_IMAGE_MAGIC))
                {
                    baseSector = Constants.XGD_MAGIC_SECTOR_XGD2 - Constants.XGD_ISO_BASE_SECTOR;
                    found = true;
                }
            }
        }
        
        if (!found && TotalSectors() >= Constants.XGD_MAGIC_SECTOR_XGD3)
        {
            if (TryReadSector(Constants.XGD_MAGIC_SECTOR_XGD3, out byte[] sector))
            {
                header = Helpers.GetXgdHeader(sector);
                if (header != null && Helpers.GetUtf8String(header.Magic).Equals(Constants.XGD_IMAGE_MAGIC) && Helpers.GetUtf8String(header.MagicTail).Equals(Constants.XGD_IMAGE_MAGIC))
                {
                    baseSector = Constants.XGD_MAGIC_SECTOR_XGD3 - Constants.XGD_ISO_BASE_SECTOR;
                    found = true;
                }
            }
        }

        if (found && header != null)
        {
            _xgdInfo = new XgdInfo()
            {
                BaseSector = baseSector,
                RootDirSector = header.RootDirSector,
                RootDirSize = header.RootDirSize,
                CreationDateTime = DateTime.FromFileTimeUtc(header.CreationFileTime)
            };
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Stores information about the Xbox Game Disc (XGD) format, including metadata
    /// such as base sector, root directory sector, root directory size, and creation date.
    /// This variable is used internally by the <see cref="SectorDecoder"/> class
    /// to manage and decode XGD content for the virtual file system.
    /// </summary>
    /// <remarks>
    /// The <see cref="XgdInfo"/> type encapsulates the structural details of the game disc file system.
    /// The data in this variable is populated during initialization of the SectorDecoder class
    /// and is accessed through the <see cref="GetXgdInfo"/> method.
    /// </remarks>
    private XgdInfo _xgdInfo;

    /// Retrieves the XgdInfo object associated with the SectorDecoder.
    /// <returns>
    /// The XgdInfo object containing information about the XGD (Xbox Game Disc) layout.
    /// </returns>
    /// <exception cref="NullReferenceException">
    /// Thrown if the SectorDecoder has not been properly initialized and the XgdInfo object is null.
    /// </exception>
    public XgdInfo GetXgdInfo()
    {
        if (_xgdInfo == null)
        {
            throw new NullReferenceException("SectorDecoder is not initialized.");
        }
        return _xgdInfo;
    }

    /// <summary>
    /// Retrieves the total number of sectors available.
    /// </summary>
    /// <returns>The total number of sectors as an unsigned integer.</returns>
    public abstract uint TotalSectors();
    
    /// Retrieves the size of a sector in an Xbox Game Disc (XGD) format.
    /// The sector size is defined as a constant value and is used in operations
    /// involving XGD-format files or virtual file systems.
    /// <returns>
    /// The size of a sector in an XGD-format, represented as an unsigned 32-bit integer.
    /// </returns>
    public uint SectorSize()
    {
        return Constants.XGD_SECTOR_SIZE;
    }
    
    /// <summary>
    /// Attempts to read a specific sector and retrieve its data.
    /// </summary>
    /// <param name="sector">The sector number to read from.</param>
    /// <param name="sectorData">
    /// The output parameter that will contain the sector data if the read operation succeeds.
    /// </param>
    /// <returns>
    /// A boolean value indicating whether the operation was successful.
    /// Returns true if the sector was read successfully; otherwise, false.
    /// </returns>
    public abstract bool TryReadSector(long sector, out byte[] sectorData);

    /// <summary>
    /// Releases resources used by the instance of the implementing class.
    /// This method is part of the <see cref="IDisposable"/> pattern and
    /// should be overridden to perform cleanup of both managed and unmanaged resources.
    /// </summary>
    public virtual void Dispose()
    {
        return;
    }
}