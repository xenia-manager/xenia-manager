using XeniaManager.Core.VirtualFileSystem.Decoders;
using XeniaManager.Core.VirtualFileSystem.Interface;
using XeniaManager.Core.VirtualFileSystem.Models;

namespace XeniaManager.Core.VirtualFileSystem;

/// <summary>
/// Provides functionality to manage and extract files from XISO container-based file systems.
/// </summary>
/// <remarks>
/// The XisoContainerReader class allows for reading, mounting, and dismounting of XISO files.
/// It includes utilities to check the validity of ISO files, extract slices from multipart ISO files,
/// and interact with underlying file sectors. This class implements IDisposable for proper resource management.
/// </remarks>
public class XisoContainerReader : ContainerReader, IDisposable
{
    /// <summary>
    /// Represents the file path to the container or resource being accessed or managed by the XisoContainerReader.
    /// </summary>
    /// <remarks>
    /// This property stores the location of the file, typically pointing to an ISO file, and is used during
    /// operations such as mounting or validating the container. The file path is initialized through the constructor
    /// and is intended to be updated as part of the object's lifecycle.
    /// </remarks>
    private string _filePath { get; set; }

    /// <summary>
    /// Tracks the number of times a container has been mounted.
    /// Used to prevent multiple initializations and to manage the proper lifecycle of the container.
    /// Incremented each time the container is successfully mounted and decremented during dismount.
    /// </summary>
    private int _mountCount { get; set; }

    /// <summary>
    /// Represents a private property that holds a reference to the currently used
    /// <see cref="SectorDecoder"/> instance. This decoder is responsible for
    /// interpreting and accessing sector data within the file system container.
    /// </summary>
    /// <remarks>
    /// The property is initialized during the mounting process and is required
    /// for accessing specific sector-related data. Accessing this property
    /// before initialization may result in exceptions.
    /// </remarks>
    private SectorDecoder? _sectorDecoder { get; set; }

    /// <summary>
    /// Represents a flag that indicates whether the object has been disposed of.
    /// </summary>
    /// <remarks>
    /// This property is used to track the disposal state of the object to prevent multiple
    /// disposal calls and ensure proper resource cleanup. It is typically checked internally
    /// before performing operations that rely on non-disposed state.
    /// </remarks>
    private bool _disposed { get; set; }

    /// <summary>
    /// Provides functionality for reading and managing Xbox ISO container files in a virtual file system.
    /// </summary>
    public XisoContainerReader(string filePath)
    {
        _filePath = filePath;
        _mountCount = 0;
    }

    /// <summary>
    /// Retrieves the current SectorDecoder instance associated with the container.
    /// </summary>
    /// <returns>The initialized SectorDecoder instance.</returns>
    /// <exception cref="Exception">Thrown if the decoder is not initialized.</exception>
    public override SectorDecoder GetDecoder()
    {
        if (_sectorDecoder == null)
        {
            throw new Exception("Decoder not initialized");
        }
        return _sectorDecoder;
    }

    /// <summary>
    /// Checks if the given file path points to a valid ISO file.
    /// </summary>
    /// <param name="filePath">The file path to verify.</param>
    /// <returns>True if the file exists and has a ".iso" extension, otherwise false.</returns>
    private static bool IsIso(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }
        return Path.GetExtension(filePath).Equals(".iso", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Retrieves a list of slice file paths based on the provided file path.
    /// </summary>
    /// <param name="filePath">The path to the file for which slice paths are to be determined.</param>
    /// <returns>An array of strings representing paths to the file slices, or the original file path if no slices are detected.</returns>
    private static string[] GetSlicesFromFile(string filePath)
    {
        List<string> slices = new List<string>();
        string fileExtension = Path.GetExtension(filePath);
        string fileWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        string fileSubExtension = Path.GetExtension(fileWithoutExtension);
        if (fileSubExtension?.Length == 2 && char.IsNumber(fileWithoutExtension[1]))
        {
            string fileWithoutSubExtension = Path.GetFileNameWithoutExtension(fileWithoutExtension);
            return Directory.GetFiles(Path.GetDirectoryName(filePath), $"{fileWithoutExtension}.?{fileExtension}").OrderBy(x => x).ToArray();
        }
        return new string[] { filePath };
    }

    /// Attempts to mount the container file for reading.
    /// This method checks if the file provided in the current instance is a valid ISO file
    /// and prepares it for sector-based operations. If the container is already mounted,
    /// the following calls to this method will increase the mount count and return true.
    /// In case of a failure during the mounting process, the method will return false.
    /// <returns>
    /// True if the container file is successfully mounted or already mounted,
    /// otherwise false if the file is invalid or an error occurs.
    /// </returns>
    public override bool TryMount()
    {
        try
        {
            if (_mountCount > 0)
            {
                _mountCount++;
                return true;
            }

            if (!IsIso(_filePath))
            {
                return false;
            }

            string[] fileSlices = GetSlicesFromFile(_filePath);
            List<IsoDetail> isoDetails = new List<IsoDetail>();

            long sectorCount = 0L;

            foreach (string fileSlice in fileSlices)
            {
                FileStream stream = new FileStream(fileSlice, FileMode.Open, FileAccess.Read, FileShare.Read);
                long sectors = stream.Length / Constants.XGD_SECTOR_SIZE;
                isoDetails.Add(new IsoDetail()
                {
                    Stream = stream,
                    StartSector = sectorCount,
                    EndSector = sectorCount + sectors - 1
                });
                sectorCount += sectors;
            }

            _sectorDecoder = new IsoSectorDecoder(isoDetails.ToArray());
            if (!_sectorDecoder.Init())
            {
                return false;
            }
            _mountCount++;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return false;
        }
    }
    /// <summary>
    /// Decreases the reference count of the mounted container.
    /// If the container is not currently mounted, the method has no effect.
    /// </summary>
    /// <remarks>
    /// This method is used to dismount or decrement the mount count of the container.
    /// It ensures that if the mount count is already zero, no further action will be taken.
    /// Typically used to manage resource allocation tied to container mount operations.
    /// </remarks>
    public override void Dismount()
    {
        if (_mountCount == 0)
        {
            return;
        }
        _mountCount--;
    }
    
    /// <summary>
    /// Retrieves the current mount count for the container.
    /// </summary>
    /// <returns>
    /// An integer representing the number of times the container has been mounted.
    /// </returns>
    public override int GetMountCount()
    {
        return _mountCount;
    }
    
    /// <summary>
    /// Releases all resources used by the XisoContainerReader instance.
    /// </summary>
    /// <remarks>
    /// This method releases unmanaged resources and, optionally, managed resources.
    /// It invokes the virtual Dispose method to allow derived classes to handle clean-up logic.
    /// Always call Dispose before releasing the last reference to the object to ensure proper resource deallocation.
    /// </remarks>
    public override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases all resources used by the <see cref="XisoContainerReader"/> instance.
    /// </summary>
    /// <param name="disposing">
    /// A boolean value indicating whether the method is being called explicitly or by a finalizer.
    /// Pass <c>true</c> if called explicitly to free both managed and unmanaged resources,
    /// otherwise <c>false</c> to only release unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _sectorDecoder?.Dispose();
            }
            _disposed = true;
        }
    }
}