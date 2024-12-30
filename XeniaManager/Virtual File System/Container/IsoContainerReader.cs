using Serilog;
using XeniaManager.VFS.Decoders;
using XeniaManager.VFS.Interface;
using XeniaManager.VFS.Models;

namespace XeniaManager.VFS.Container;

public class IsoContainerReader : ContainerReader, IDisposable
{
    private string FilePath { get; set; }
    private int MountCount { get; set; }
    private SectorDecoder? _sectorDecoder { get; set;}
    private bool _disposed { get; set; }
    
    public IsoContainerReader(string filePath)
    {
        FilePath = filePath;
        MountCount = 0;
    }
    
    public override SectorDecoder GetDecoder()
    {
        if (_sectorDecoder == null)
        {
            throw new Exception("Container not mounted.");
        }
        return _sectorDecoder;
    }
    
    /// <summary>
    /// Checks if the file is an .iso or not
    /// </summary>
    /// <param name="filePath">Path to the file we're checking</param>
    /// <returns>true if the file is an .iso file, otherwise false</returns>
    public static bool FileChecker(string filePath)
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
            if (MountCount > 0)
            {
                MountCount++;
                return true;
            }

            if (!FileChecker(FilePath))
            {
                return false;
            }
            
            string[] fileSlices = Utility.GetSlicesFromFile(FilePath);
            List<IsoDetail> isoDetails = new List<IsoDetail>();

            long sectorCount = 0L;

            foreach (string fileSlice in fileSlices)
            {
                FileStream stream = new FileStream(fileSlice, FileMode.Open, FileAccess.Read, FileShare.Read);
                long sectors = stream.Length / Constants.XGD_SECTOR_SIZE;
                // TODO: Optimize this
                IsoDetail isoDetail = new IsoDetail
                {
                    Stream = stream,
                    StartSector = sectorCount,
                    EndSector = sectorCount + sectors - 1
                };
                isoDetails.Add(isoDetail);
                sectorCount += sectors;
            }

            _sectorDecoder = new IsoSectorDecoder(isoDetails.ToArray());
            if (!_sectorDecoder.Init())
            {
                return false;
            }
            MountCount++;
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
        throw new NotImplementedException();
    }
    
    public override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed == false)
        {
            if (disposing)
            {
                _sectorDecoder?.Dispose();
            }
            _disposed = true;
        }
    }

    public override int GetMountCount()
    {
        throw new NotImplementedException();
    }
}