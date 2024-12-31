using Serilog;
using XeniaManager.VFS.Decoders;
using XeniaManager.VFS.Interface;
using XeniaManager.VFS.Models;

namespace XeniaManager.VFS.Container;

public class IsoContainerReader : ContainerReader, IDisposable
{
    private string _FilePath { get; set; }
    private int _MountCount { get; set; }
    private SectorDecoder? _SectorDecoder { get; set;}
    private bool _Disposed { get; set; }
    
    public IsoContainerReader(string filePath)
    {
        _FilePath = filePath;
        _MountCount = 0;
    }
    
    public override SectorDecoder GetDecoder()
    {
        if (_SectorDecoder == null)
        {
            throw new Exception("Container not mounted.");
        }
        return _SectorDecoder;
    }
    
    /// <summary>
    /// Checks if the file is an .iso or not
    /// </summary>
    /// <param name="filePath">Path to the file we're checking</param>
    /// <returns>true if the file is an .iso file, otherwise false</returns>
    public static bool IsIso(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }
        return Path.GetExtension(filePath).Equals(".iso", StringComparison.OrdinalIgnoreCase);
    }

    public override bool TryMount()
    {
        try
        {
            if (_MountCount > 0)
            {
                _MountCount++;
                return true;
            }

            if (!IsIso(_FilePath))
            {
                return false;
            }
            
            string[] fileSlices = Utility.GetSlicesFromFile(_FilePath);
            List<IsoDetail> isoDetails = new List<IsoDetail>();

            long sectorCount = 0L;

            foreach (string fileSlice in fileSlices)
            {
                FileStream stream = new FileStream(fileSlice, FileMode.Open, FileAccess.Read, FileShare.Read);
                long sectors = stream.Length / Constants.XGD_SECTOR_SIZE;
                isoDetails.Add(new IsoDetail
                {
                    Stream = stream,
                    StartSector = sectorCount,
                    EndSector = sectorCount + sectors - 1
                });
                sectorCount += sectors;
            }

            _SectorDecoder = new IsoSectorDecoder(isoDetails.ToArray());
            if (!_SectorDecoder.Init())
            {
                return false;
            }
            _MountCount++;
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message + "\nFull Error:\n" + ex);
            return false;
        }
    }

    public override void Dismount()
    {
        if (_MountCount == 0)
        {
            return;
        }
        _MountCount--;
    }
    
    public override int GetMountCount()
    {
        return _MountCount;
    }
    
    public override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_Disposed == false)
        {
            if (disposing)
            {
                _SectorDecoder?.Dispose();
            }
            _Disposed = true;
        }
    }
}