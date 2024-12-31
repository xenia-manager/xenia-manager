using XeniaManager.VFS.Interface;
using XeniaManager.VFS.Models;

namespace XeniaManager.VFS.Decoders;

internal class IsoSectorDecoder : SectorDecoder
{
    private readonly IsoDetail[] _IsoDetails;
    private readonly object _Mutex;
    private bool _Disposed;

    public IsoSectorDecoder(IsoDetail[] isoDetails)
    {
        _IsoDetails = isoDetails;
        _Mutex = new object();
        _Disposed = false;
    }

    public override uint TotalSectors()
    {
        return (uint)(_IsoDetails.Max(iso => iso.EndSector) + 1);
    }

    public override bool TryReadSector(long sector, out byte[] sectorData)
    {
        sectorData = new byte[Constants.XGD_SECTOR_SIZE];
        lock (_Mutex)
        {
            foreach (IsoDetail isoDetail in _IsoDetails)
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