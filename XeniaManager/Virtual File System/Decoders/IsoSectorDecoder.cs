using XeniaManager.VFS.Interface;
using XeniaManager.VFS.Models;

namespace XeniaManager.VFS.Decoders;

internal class IsoSectorDecoder : SectorDecoder
{
    private readonly IsoDetail[] _isoDetails;
    private readonly object _mutex;
    private bool _disposed;

    public IsoSectorDecoder(IsoDetail[] isoDetails)
    {
        _isoDetails = isoDetails;
        _mutex = new object();
        _disposed = false;
    }

    public override uint TotalSectors()
    {
        return (uint)(_isoDetails.Max(iso => iso.EndSector) + 1);
    }

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