using XeniaManager.Logging;

namespace XeniaManager.Files.Models.Iso;

/// <summary>
/// Handles reading sectors from an ISO file.
/// Xbox ISO files use a sector-based format where data is organized in 2048-byte sectors.
/// </summary>
internal sealed class IsoSectorReader : IDisposable
{
    private readonly IsoDetail[] _isoDetails;
    private readonly Lock _lock = new Lock();
    private bool _disposed;
    private XgdInfo? _xgdInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="IsoSectorReader"/> class.
    /// </summary>
    /// <param name="isoDetails">Array of ISO file slice details.</param>
    public IsoSectorReader(IsoDetail[] isoDetails)
    {
        _isoDetails = isoDetails;
    }

    /// <summary>
    /// Initializes the sector reader by finding and parsing the XGD header.
    /// </summary>
    /// <returns>True if initialization succeeded, false otherwise.</returns>
    public bool Initialize()
    {
        Logger.Trace<IsoSectorReader>("Initializing ISO sector reader");

        // Probe candidates in layout order: each holds the game-partition volume
        // descriptor ("MICROSOFT*XBOX*MEDIA") at sector 32, so the partition base
        // is the magic sector minus ISO_BASE_SECTOR.
        (uint MagicSector, string Name)[] probes =
        [
            (IsoConstants.MAGIC_SECTOR_XDKI, "XDKI"),
            (IsoConstants.MAGIC_SECTOR_XGD1, "XGD1"),
            (IsoConstants.MAGIC_SECTOR_XGD3, "XGD3"),
            (IsoConstants.MAGIC_SECTOR_XGD2, "XGD2")
        ];

        foreach ((uint magicSector, string name) in probes)
        {
            if (TotalSectors() < magicSector)
            {
                continue;
            }

            if (!TryReadSector(magicSector, out byte[] sector))
            {
                continue;
            }

            XgdHeader? header = ParseXgdHeader(sector);
            if (header == null || !IsValidMagic(header))
            {
                continue;
            }

            // Guard against corrupt descriptors: the root directory must be between 13 bytes and 32 MiB.
            if (header.Value.RootDirSize < 13 || header.Value.RootDirSize > 32 * 1024 * 1024)
            {
                Logger.Warning<IsoSectorReader>($"Rejected {name} descriptor with invalid root size {header.Value.RootDirSize}");
                continue;
            }

            uint baseSector = magicSector - IsoConstants.ISO_BASE_SECTOR;
            Logger.Debug<IsoSectorReader>($"Detected {name} format");
            _xgdInfo = new XgdInfo
            {
                BaseSector = baseSector,
                RootDirSector = header.Value.RootDirSector,
                RootDirSize = header.Value.RootDirSize,
                CreationDateTime = DateTime.FromFileTime(header.Value.CreationFileTime)
            };
            Logger.Info<IsoSectorReader>($"ISO initialized - Base Sector: {baseSector}, Root Dir: {header.Value.RootDirSector}," +
                                         $" Size: {header.Value.RootDirSize}");
            return true;
        }

        Logger.Error<IsoSectorReader>("Failed to initialize ISO - invalid or unsupported format");
        return false;
    }

    /// <summary>
    /// Gets the XGD information for this ISO.
    /// </summary>
    /// <returns>The XGD information.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the sector reader is not initialized.</exception>
    public XgdInfo GetXgdInfo() => _xgdInfo ?? throw new InvalidOperationException("Sector reader not initialized. Call Initialize() first.");

    /// <summary>
    /// Gets the total number of sectors across all ISO slices.
    /// </summary>
    /// <returns>The total sector count.</returns>
    public uint TotalSectors()
    {
        lock (_lock)
        {
            long maxSector = _isoDetails.Max(d => d.EndSector);
            return (uint)(maxSector + 1);
        }
    }

    /// <summary>
    /// Gets the size of each sector in bytes.
    /// </summary>
    /// <returns>The sector size (2048 bytes).</returns>
    public uint SectorSize() => IsoConstants.SECTOR_SIZE;

    /// <summary>
    /// Attempts to read a sector from the ISO.
    /// </summary>
    /// <param name="sector">The sector number to read.</param>
    /// <param name="sectorData">The sector data buffer (must be at least 2048 bytes).</param>
    /// <returns>True if the sector was read successfully, false otherwise.</returns>
    public bool TryReadSector(long sector, out byte[] sectorData)
    {
        sectorData = new byte[IsoConstants.SECTOR_SIZE];

        lock (_lock)
        {
            foreach (IsoDetail isoDetail in _isoDetails)
            {
                if (sector >= isoDetail.StartSector && sector <= isoDetail.EndSector)
                {
                    isoDetail.Stream.Position = (sector - isoDetail.StartSector) * IsoConstants.SECTOR_SIZE;
                    int bytesRead = isoDetail.Stream.Read(sectorData, 0, (int)IsoConstants.SECTOR_SIZE);
                    return bytesRead == IsoConstants.SECTOR_SIZE;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Parses an XGD header from sector data.
    /// </summary>
    /// <param name="sectorData">The sector data containing the header.</param>
    /// <returns>The parsed XGD header, or null if parsing failed.</returns>
    private static XgdHeader? ParseXgdHeader(byte[] sectorData)
    {
        try
        {
            using MemoryStream stream = new MemoryStream(sectorData);
            using BinaryReader reader = new BinaryReader(stream);

            XgdHeader header = new XgdHeader
            {
                Magic = reader.ReadBytes(20),
                RootDirSector = reader.ReadUInt32(),
                RootDirSize = reader.ReadUInt32(),
                CreationFileTime = reader.ReadInt64(),
                Padding = reader.ReadBytes(0x7C8),
                MagicTail = reader.ReadBytes(20)
            };

            return header;
        }
        catch (Exception ex)
        {
            Logger.Error<IsoSectorReader>($"Failed to parse XGD header: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Validates the leading magic string in the XGD header.
    /// </summary>
    /// <param name="header">The XGD header to validate.</param>
    /// <returns>True if the leading magic string is valid, false otherwise.</returns>
    private static bool IsValidMagic(XgdHeader? header)
    {
        if (!header.HasValue)
        {
            return false;
        }

        string magic = System.Text.Encoding.ASCII.GetString(header.Value.Magic).Trim('\0');
        return magic == IsoConstants.XGD_IMAGE_MAGIC;
    }

    /// <summary>
    /// Disposes of the sector reader and closes all file streams.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (IsoDetail isoDetail in _isoDetails)
        {
            isoDetail.Stream.Dispose();
        }

        _disposed = true;
    }
}