using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Iso;

namespace XeniaManager.Core.Files;

/// <summary>
/// Handles loading and parsing of ISO files containing Xbox disc images.
/// This class extracts the default.xex executable from the ISO and parses it
/// using the existing XexFile parser to get MediaID and TitleID.
/// </summary>
public sealed class IsoFile : IDisposable
{
    private bool _disposed;
    private IsoSectorReader? _sectorReader;

    /// <summary>
    /// Gets the parsed XEX file from the ISO's default.xex.
    /// </summary>
    public XexFile? XexFile { get; private set; }

    /// <summary>
    /// Gets whether the ISO file was successfully parsed.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the validation error message if the file is invalid.
    /// </summary>
    public string? ValidationError { get; private set; }

    /// <summary>
    /// Gets the XGD information if the ISO was successfully parsed.
    /// </summary>
    public XgdInfo? XgdInformation { get; private set; }

    /// <summary>
    /// Gets the path to the ISO file.
    /// </summary>
    public string FilePath { get; private set; } = string.Empty;

    /// <summary>
    /// Private constructor to enforce factory methods.
    /// </summary>
    private IsoFile()
    {
        IsValid = false;
    }

    /// <summary>
    /// Loads an ISO file from the specified path and extracts the default.xex.
    /// </summary>
    /// <param name="filePath">The path to the ISO file to load.</param>
    /// <returns>A new IsoFile instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static IsoFile Load(string filePath)
    {
        Logger.Debug<IsoFile>($"Loading ISO file from {filePath}");

        if (!File.Exists(filePath))
        {
            Logger.Error<IsoFile>($"ISO file does not exist: {filePath}");
            throw new FileNotFoundException($"ISO file does not exist at {filePath}", filePath);
        }

        IsoFile isoFile = new IsoFile
        {
            FilePath = filePath
        };

        try
        {
            // Get ISO file slices (handles split .iso.1, .iso.2, etc.)
            string[] slices = GetIsoSlices(filePath);
            Logger.Debug<IsoFile>($"ISO has {slices.Length} slice(s)");

            // Open all slices
            IsoDetail[] isoDetails = new IsoDetail[slices.Length];
            long sectorCount = 0;

            for (int i = 0; i < slices.Length; i++)
            {
                FileStream stream = new FileStream(slices[i], FileMode.Open, FileAccess.Read, FileShare.Read);
                long sectors = stream.Length / IsoConstants.SECTOR_SIZE;

                isoDetails[i] = new IsoDetail
                {
                    Stream = stream,
                    StartSector = sectorCount,
                    EndSector = sectorCount + sectors - 1
                };

                sectorCount += sectors;
            }

            // Create sector reader
            isoFile._sectorReader = new IsoSectorReader(isoDetails);

            // Initialize the sector reader
            if (!isoFile._sectorReader.Initialize())
            {
                isoFile.ValidationError = "Failed to initialize ISO sector reader - invalid or unsupported format";
                Logger.Error<IsoFile>(isoFile.ValidationError);
                isoFile._sectorReader.Dispose();
                isoFile._sectorReader = null;
                return isoFile;
            }

            // Get XGD info
            isoFile.XgdInformation = isoFile._sectorReader.GetXgdInfo();
            Logger.Info<IsoFile>($"ISO initialized - Base Sector: {isoFile.XgdInformation.BaseSector}, Root Dir: {isoFile.XgdInformation.RootDirSector}");

            // Extract and parse default.xex
            if (!isoFile.ExtractAndParseDefaultXex())
            {
                isoFile.ValidationError = "Failed to extract or parse default.xex from ISO";
                Logger.Error<IsoFile>(isoFile.ValidationError);
                return isoFile;
            }

            isoFile.IsValid = true;
            Logger.Info<IsoFile>($"Successfully parsed ISO - TitleID: {isoFile.XexFile!.TitleId}, MediaID: {isoFile.XexFile.MediaId}");
        }
        catch (Exception ex)
        {
            isoFile.ValidationError = $"Failed to parse ISO: {ex.Message}";
            Logger.Error<IsoFile>(isoFile.ValidationError);
            Logger.LogExceptionDetails<IsoFile>(ex);
            isoFile._sectorReader?.Dispose();
            isoFile._sectorReader = null;
        }

        return isoFile;
    }

    /// <summary>
    /// Creates an IsoFile from raw byte data.
    /// Note: This method is not supported for ISO files as they are typically too large.
    /// </summary>
    /// <param name="data">The raw byte data (not supported for ISO).</param>
    /// <returns>An invalid IsoFile instance.</returns>
    public static IsoFile FromBytes(byte[] data)
    {
        Logger.Error<IsoFile>("IsoFile.FromBytes() is not supported - ISO files must be loaded from disk due to their size");
        return new IsoFile
        {
            ValidationError = "FromBytes is not supported for ISO files - use Load() instead"
        };
    }

    /// <summary>
    /// Gets all ISO file slices for a given file path.
    /// Handles split archives like game.iso, game.iso.1, game.iso.2, etc.
    /// </summary>
    /// <param name="filePath">The path to the main ISO file.</param>
    /// <returns>Array of file paths for all slices.</returns>
    private static string[] GetIsoSlices(string filePath)
    {
        string extension = Path.GetExtension(filePath);
        string fileWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
        string? directory = Path.GetDirectoryName(filePath);

        // Check if this is a split archive (e.g., .iso.1, .iso.2)
        string subExtension = Path.GetExtension(fileWithoutExtension);
        if (subExtension.Length != 2 || !char.IsNumber(subExtension[1]))
        {
            return [filePath];
        }
        // This is a split archive - find all parts
        string fileWithoutSubExtension = Path.GetFileNameWithoutExtension(fileWithoutExtension);
        string searchPattern = $"{fileWithoutSubExtension}.{extension}.*";

        if (!string.IsNullOrEmpty(directory))
        {
            string[] files = Directory.GetFiles(directory, searchPattern)
                .OrderBy(f => f)
                .ToArray();
            return files.Length > 0 ? files : [filePath];
        }

        return [filePath];
    }

    /// <summary>
    /// Extracts the default.xex file from the ISO and parses it.
    /// </summary>
    /// <returns>True if extraction and parsing succeeded, false otherwise.</returns>
    private bool ExtractAndParseDefaultXex()
    {
        if (_sectorReader == null || XgdInformation == null)
        {
            ValidationError = "Sector reader not initialized";
            return false;
        }

        Logger.Debug<IsoFile>("Searching for default.xex in ISO...");

        try
        {
            // Navigate the ISO filesystem to find default.xex
            byte[]? defaultXexData = FindFileInIso(IsoConstants.DEFAULT_EXECUTABLE_NAME);

            if (defaultXexData == null || defaultXexData.Length == 0)
            {
                ValidationError = "default.xex not found in ISO";
                Logger.Error<IsoFile>(ValidationError);
                return false;
            }

            Logger.Info<IsoFile>($"Found default.xex ({defaultXexData.Length} bytes), parsing...");

            // Parse the XEX using existing XexFile parser
            XexFile xexFile = XexFile.FromBytes(defaultXexData);

            if (!xexFile.IsValid)
            {
                ValidationError = $"default.xex is invalid: {xexFile.ValidationError}";
                Logger.Error<IsoFile>(ValidationError);
                return false;
            }

            // Store the parsed XEX file
            XexFile = xexFile;
            Logger.Debug<IsoFile>($"Extracted from default.xex - TitleID: {XexFile!.TitleId}, MediaID: {XexFile.MediaId}");

            return true;
        }
        catch (Exception ex)
        {
            ValidationError = $"Failed to extract/default.xex: {ex.Message}";
            Logger.Error<IsoFile>(ValidationError);
            Logger.LogExceptionDetails<IsoFile>(ex);
            return false;
        }
    }

    /// <summary>
    /// Finds and extracts a file from the ISO by name.
    /// Uses a stack-based traversal of the XDVDFS directory structure.
    /// </summary>
    /// <param name="fileName">The name of the file to find (case-insensitive).</param>
    /// <returns>The file data, or null if not found.</returns>
    private byte[]? FindFileInIso(string fileName)
    {
        if (_sectorReader == null || XgdInformation == null)
        {
            return null;
        }

        // Read the root directory
        uint rootSectors = (XgdInformation.RootDirSize + IsoConstants.SECTOR_SIZE - 1) / IsoConstants.SECTOR_SIZE;
        byte[] rootData = new byte[XgdInformation.RootDirSize];

        for (uint i = 0; i < rootSectors; i++)
        {
            uint currentSector = XgdInformation.BaseSector + XgdInformation.RootDirSector + i;
            if (!_sectorReader.TryReadSector(currentSector, out byte[] sectorData))
            {
                Logger.Error<IsoFile>($"Failed to read root directory sector {i}");
                return null;
            }

            uint offset = i * IsoConstants.SECTOR_SIZE;
            uint length = Math.Min(IsoConstants.SECTOR_SIZE, XgdInformation.RootDirSize - offset);
            Array.Copy(sectorData, 0, rootData, offset, length);
        }

        // Stack-based directory traversal (preorder)
        Stack<DirectoryNode> directoryStack = new Stack<DirectoryNode>();
        directoryStack.Push(new DirectoryNode { Data = rootData, Offset = 0 });

        while (directoryStack.Count > 0)
        {
            DirectoryNode currentNode = directoryStack.Pop();

            using MemoryStream dirStream = new MemoryStream(currentNode.Data);
            using BinaryReader dirReader = new BinaryReader(dirStream);

            if (currentNode.Offset * 4 >= (uint)dirStream.Length)
            {
                continue;
            }

            uint entryOffset = currentNode.Offset * 4;
            dirStream.Position = entryOffset;

            // Read the directory entry header (14 bytes)
            byte[] headerBuffer = dirReader.ReadBytes(14);
            if (headerBuffer.Length != 14)
            {
                continue;
            }

            // Parse header (little-endian format per XDVDFS spec)
            ushort left = (ushort)(headerBuffer[0] | (headerBuffer[1] << 8));
            ushort right = (ushort)(headerBuffer[2] | (headerBuffer[3] << 8));
            uint sector = (uint)(headerBuffer[4] | (headerBuffer[5] << 8) | (headerBuffer[6] << 16) | (headerBuffer[7] << 24));
            uint size = (uint)(headerBuffer[8] | (headerBuffer[9] << 8) | (headerBuffer[10] << 16) | (headerBuffer[11] << 24));
            byte attribute = headerBuffer[12];
            byte nameLength = headerBuffer[13];

            // Check for empty entry
            bool allFF = true;
            bool allZero = true;
            for (int i = 0; i < 14; i++)
            {
                if (headerBuffer[i] != 0xFF)
                {
                    allFF = false;
                }
                if (headerBuffer[i] != 0x00)
                {
                    allZero = false;
                }
            }
            if (allFF || allZero)
            {
                continue;
            }

            // Validate name length
            if (nameLength == 0)
            {
                continue;
            }

            // Read filename
            uint filenameOffset = entryOffset + 14;
            if (filenameOffset + nameLength > (uint)dirStream.Length)
            {
                continue;
            }

            dirStream.Position = filenameOffset;
            byte[] filenameBytes = dirReader.ReadBytes(nameLength);
            string filename = System.Text.Encoding.ASCII.GetString(filenameBytes);

            // Check if this is the file we're looking for
            if (filename.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                if ((attribute & 0x10) != 0)
                {
                    Logger.Error<IsoFile>($"Found {fileName} but it's a directory, not a file");
                    return null;
                }

                // Extract file data
                if (size == 0)
                {
                    return Array.Empty<byte>();
                }

                byte[] fileData = new byte[size];
                uint readSector = sector + XgdInformation.BaseSector;
                uint processed = 0;

                while (processed < size)
                {
                    if (!_sectorReader.TryReadSector(readSector, out byte[] sectorData))
                    {
                        Logger.Error<IsoFile>($"Failed to read file sector at {readSector}");
                        return null;
                    }

                    uint bytesToCopy = Math.Min(size - processed, IsoConstants.SECTOR_SIZE);
                    Array.Copy(sectorData, 0, fileData, processed, bytesToCopy);
                    readSector++;
                    processed += bytesToCopy;
                }

                Logger.Info<IsoFile>($"Successfully extracted {fileName} ({fileData.Length} bytes)");
                return fileData;
            }

            // Push the right child first (so the left is processed first)
            if (right != 0 && right != 0xFFFF)
            {
                uint rightOffsetBytes = (uint)right * 4;
                if (rightOffsetBytes < (ulong)dirStream.Length)
                {
                    directoryStack.Push(new DirectoryNode { Data = currentNode.Data, Offset = right });
                }
            }

            // Push left child
            if (left != 0 && left != 0xFFFF)
            {
                uint leftOffsetBytes = (uint)left * 4;
                if (leftOffsetBytes < (ulong)dirStream.Length)
                {
                    directoryStack.Push(new DirectoryNode { Data = currentNode.Data, Offset = left });
                }
            }

            // If directory, add its contents to the stack
            if ((attribute & 0x10) != 0 && size > 0)
            {
                uint directorySectors = (size + IsoConstants.SECTOR_SIZE - 1) / IsoConstants.SECTOR_SIZE;
                byte[] directoryData = new byte[size];

                for (uint i = 0; i < directorySectors; i++)
                {
                    uint currentDirectorySector = XgdInformation.BaseSector + sector + i;
                    if (_sectorReader.TryReadSector(currentDirectorySector, out byte[] sectorData))
                    {
                        uint offset = i * IsoConstants.SECTOR_SIZE;
                        uint length = Math.Min(IsoConstants.SECTOR_SIZE, size - offset);
                        Array.Copy(sectorData, 0, directoryData, offset, length);
                    }
                }

                directoryStack.Push(new DirectoryNode { Data = directoryData, Offset = 0 });
            }
        }

        Logger.Warning<IsoFile>($"File {fileName} not found in ISO");
        return null;
    }

    /// <summary>
    /// Internal class representing a directory node during traversal.
    /// </summary>
    private sealed class DirectoryNode
    {
        public byte[] Data { get; init; } = Array.Empty<byte>();
        public uint Offset { get; init; }
    }

    /// <summary>
    /// Disposes of the ISO file resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the ISO file resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose of managed resources.</param>
    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        if (disposing)
        {
            _sectorReader?.Dispose();
        }
        _disposed = true;
    }

    ~IsoFile()
    {
        Dispose(false);
    }
}