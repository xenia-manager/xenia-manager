using XeniaManager.VFS.Models;

namespace XeniaManager.VFS.Interface;

public abstract class SectorDecoder : ISectorDecoder
{
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
            _XgdInfo = new XgdInfo
            {
                BaseSector = baseSector,
                RootDirSector = header.RootDirSector,
                RootDirSize = header.RootDirSize,
                CreationDateTime = DateTime.FromFileTime(header.CreationFileTime)
            };
            return true;    
        }
        return false;
    }

    private XgdInfo? _XgdInfo;
    public XgdInfo GetXgdInfo()
    {
        if (_XgdInfo == null)
        {
            throw new NullReferenceException("Sector decoder is not initialized");
        }
        return _XgdInfo;
    }

    public abstract uint TotalSectors();

    public uint SectorSize()
    {
        return Constants.XGD_SECTOR_SIZE;
    }

    public abstract bool TryReadSector(long sector, out byte[] sectorData);
    
    public virtual void Dispose()
    {
        return;
    }
}