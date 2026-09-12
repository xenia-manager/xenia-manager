using XeniaManager.Logging;
using XeniaManager.Files.Models.Iso;

namespace XeniaManager.Files;

/// <summary>
/// Handles loading and parsing of ISO files containing Xbox disc images.
/// This class extracts the default.xex executable from the ISO and parses it
/// using the existing XexFile parser to get MediaID and TitleID.
/// </summary>
public sealed class IsoFile : IDisposable
{
    private bool _disposed;
    private IsoSectorReader? _sectorReader;
    private List<GdfxEntry>? _files;
    private List<GdfxEntry>? _entries;

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
    /// Gets a flat list of all files on the disc with their full paths and sizes.
    /// Lazily computed and cached on first access. Empty when the sector reader is unavailable.
    /// </summary>
    public IReadOnlyList<GdfxEntry> Files
    {
        get
        {
            if (_files == null)
            {
                _files = WalkEntries().Where(e => e.IsFile).ToList();
            }

            return _files;
        }
    }

    /// <summary>
    /// Gets a flat list of all entries (files and directories) on the disc
    /// with their full paths and sizes. Directories have IsFile = false and Size = 0.
    /// Lazily computed and cached on first access. Empty when the sector reader is unavailable.
    /// </summary>
    public IReadOnlyList<GdfxEntry> Entries
    {
        get
        {
            if (_entries == null)
            {
                _entries = WalkEntries().ToList();
            }

            return _entries;
        }
    }

    /// <summary>
    /// Looks up a file or directory by its path within the disc.
    /// Supports both forward and backslash path separators. Comparison is case-insensitive.
    /// </summary>
    /// <param name="path">The path to look up (e.g., "game/data.bin" or "default.xex").</param>
    /// <returns>The entry if found; null if the path does not exist.</returns>
    public GdfxEntry? Lookup(string path)
    {
        // Note: explicit array — Split('/', '\\', options) binds to the (separator, count, options)
        // overload with '\\' as count and silently ignores the backslash.
        string normalized = string.Join('/', path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries));
        return WalkEntries().FirstOrDefault(e => e.FullPath.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Lists the immediate children of a directory by path.
    /// </summary>
    /// <param name="path">The directory path (e.g., "game" or "" for root).</param>
    /// <returns>A list of child entries, or null if the path does not exist or is a file.</returns>
    public List<GdfxEntry>? ListDirectory(string path)
    {
        string normalized = string.Join('/', path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries));
        if (normalized.Length == 0)
        {
            return WalkEntries().Where(e => !e.FullPath.Contains('/')).ToList();
        }

        GdfxEntry? dir = Lookup(normalized);
        if (dir == null || dir.IsFile)
        {
            return null;
        }

        string prefix = normalized + '/';
        return WalkEntries()
            .Where(e => e.FullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                        && !e.FullPath[prefix.Length..].Contains('/'))
            .ToList();
    }

    /// <summary>
    /// Reads the full contents of a file identified by its path within the disc.
    /// </summary>
    /// <param name="path">The path to the file (e.g., "default.xex").</param>
    /// <returns>The complete file data, or null if the file was not found or is a directory.</returns>
    public byte[]? ReadFile(string path)
    {
        GdfxEntry? entry = Lookup(path);
        if (entry == null || !entry.IsFile)
        {
            return null;
        }

        return ReadFile(entry);
    }

    /// <summary>
    /// Reads the full contents of a file from a <see cref="GdfxEntry"/>.
    /// </summary>
    /// <param name="entry">The file entry to read.</param>
    /// <returns>The complete file data as a byte array.</returns>
    public byte[] ReadFile(GdfxEntry entry) => ReadFile(entry, 0, entry.Size);

    /// <summary>
    /// Reads a portion of a file starting at the specified offset with the specified length.
    /// </summary>
    /// <param name="entry">The file entry to read from.</param>
    /// <param name="offset">The byte offset within the file to start reading from.</param>
    /// <param name="length">The number of bytes to read.</param>
    /// <returns>The requested file data. Empty when offset is beyond the file size.</returns>
    public byte[] ReadFile(GdfxEntry entry, ulong offset, ulong length)
    {
        if (offset >= entry.Size)
        {
            return Array.Empty<byte>();
        }

        ulong bytesToRead = Math.Min(length, entry.Size - offset);
        if (bytesToRead == 0)
        {
            return Array.Empty<byte>();
        }

        // GDFX sectors are 0x800 bytes; the first sector may start mid-way when offset is unaligned.
        const ulong sectorSize = IsoConstants.SECTOR_SIZE;
        byte[] result = new byte[bytesToRead];
        ulong remaining = bytesToRead;
        ulong destOffset = 0;
        ulong fileOffset = offset;
        while (remaining > 0)
        {
            uint sectorIndex = (uint)(fileOffset / sectorSize);
            uint sectorOffset = (uint)(fileOffset % sectorSize);
            uint step = (uint)Math.Min(remaining, sectorSize - sectorOffset);
            byte[]? sectorData = ReadSectors(entry.Sector + sectorIndex, IsoConstants.SECTOR_SIZE);
            if (sectorData == null)
            {
                Logger.Error<IsoFile>($"Failed to read file sector for '{entry.FullPath}'");
                return Array.Empty<byte>();
            }

            Array.Copy(sectorData, sectorOffset, result, (long)destOffset, step);
            fileOffset += step;
            remaining -= step;
            destOffset += step;
        }

        return result;
    }

    /// <summary>
    /// Extracts all files from the disc to the specified output directory,
    /// preserving the directory structure. Entries that would escape the output
    /// directory are skipped with a warning (same guard as STFS extraction).
    /// </summary>
    /// <param name="outputDir">The root output directory to extract files into.</param>
    public void ExtractAll(string outputDir)
    {
        Logger.Info<IsoFile>($"Extracting all files from {FilePath} to {outputDir}");
        foreach (GdfxEntry file in Files)
        {
            string filePath;
            try
            {
                filePath = Utilities.ArchiveExtractor.GetSafeEntryOutputPath(outputDir,
                    file.FullPath.Replace('/', Path.DirectorySeparatorChar));
            }
            catch (IOException ex)
            {
                Logger.Warning<IsoFile>($"Skipping '{file.FullPath}': {ex.Message}");
                continue;
            }

            string? parentDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            File.WriteAllBytes(filePath, ReadFile(file));
        }

        Logger.Info<IsoFile>($"Extraction complete to {outputDir}");
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
    /// Searches the whole GDFX tree and returns the first case-insensitive name match.
    /// </summary>
    /// <param name="fileName">The name of the file to find (case-insensitive).</param>
    /// <returns>The file data, or null if not found.</returns>
    private byte[]? FindFileInIso(string fileName)
    {
        foreach (GdfxEntry entry in WalkEntries())
        {
            if (!entry.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!entry.IsFile)
            {
                Logger.Error<IsoFile>($"Found {fileName} but it's a directory, not a file");
                return null;
            }

            if (entry.Size == 0)
            {
                return Array.Empty<byte>();
            }

            byte[] fileData = ReadFile(entry);
            if (entry.Size > 0 && fileData.Length == 0)
            {
                return null;
            }

            Logger.Info<IsoFile>($"Successfully extracted {fileName} ({fileData.Length} bytes)");
            return fileData;
        }

        Logger.Warning<IsoFile>($"File {fileName} not found in ISO");
        return null;
    }

    /// <summary>
    /// Reads <paramref name="size"/> bytes starting at GDFX <paramref name="sector"/>
    /// (relative to <see cref="XgdInfo.BaseSector"/>), or null when any sector fails to read.
    /// </summary>
    private byte[]? ReadSectors(uint sector, uint size)
    {
        if (_sectorReader == null || XgdInformation == null || size == 0)
        {
            return size == 0 ? Array.Empty<byte>() : null;
        }

        uint sectorCount = (size + IsoConstants.SECTOR_SIZE - 1) / IsoConstants.SECTOR_SIZE;
        byte[] data = new byte[size];
        for (uint i = 0; i < sectorCount; i++)
        {
            if (!_sectorReader.TryReadSector(XgdInformation.BaseSector + sector + i, out byte[] sectorData))
            {
                return null;
            }

            uint offset = i * IsoConstants.SECTOR_SIZE;
            uint length = Math.Min(IsoConstants.SECTOR_SIZE, size - offset);
            Array.Copy(sectorData, 0, data, offset, length);
        }

        return data;
    }

    /// <summary>
    /// Walks the whole GDFX tree preorder (entry, then subdirectory contents,
    /// then left and right sibling subtrees), yielding every file and directory
    /// with its full root-relative path. Same visit order as the previous
    /// duplicated walkers; table linkage matches <c>DiscImageDevice::ReadEntry</c>.
    /// </summary>
    private IEnumerable<GdfxEntry> WalkEntries()
    {
        if (_sectorReader == null || XgdInformation == null)
        {
            yield break;
        }

        byte[]? rootData = ReadSectors(XgdInformation.RootDirSector, XgdInformation.RootDirSize);
        if (rootData == null)
        {
            Logger.Error<IsoFile>("Failed to read ISO root directory");
            yield break;
        }

        // Stack-based directory traversal (preorder)
        Stack<DirectoryNode> directoryStack = new Stack<DirectoryNode>();
        directoryStack.Push(new DirectoryNode
        {
            Data = rootData,
            Offset = 0,
            ParentPath = string.Empty
        });

        while (directoryStack.Count > 0)
        {
            DirectoryNode currentNode = directoryStack.Pop();

            if (currentNode.Offset * 4 >= (uint)currentNode.Data.Length)
            {
                continue;
            }

            uint entryOffset = currentNode.Offset * 4;
            if (entryOffset + 14 > (uint)currentNode.Data.Length)
            {
                continue;
            }

            // Read the directory entry header (14 bytes, little-endian per XDVDFS spec)
            ushort left = (ushort)(currentNode.Data[entryOffset] | (currentNode.Data[entryOffset + 1] << 8));
            ushort right = (ushort)(currentNode.Data[entryOffset + 2] | (currentNode.Data[entryOffset + 3] << 8));
            uint sector = (uint)(currentNode.Data[entryOffset + 4] | (currentNode.Data[entryOffset + 5] << 8) | (currentNode.Data[entryOffset + 6] << 16) |
                                 (currentNode.Data[entryOffset + 7] << 24));
            uint size = (uint)(currentNode.Data[entryOffset + 8] | (currentNode.Data[entryOffset + 9] << 8) | (currentNode.Data[entryOffset + 10] << 16) |
                               (currentNode.Data[entryOffset + 11] << 24));
            byte attribute = currentNode.Data[entryOffset + 12];
            byte nameLength = currentNode.Data[entryOffset + 13];

            // Check for empty entry
            bool allFF = true;
            bool allZero = true;
            for (int i = 0; i < 14; i++)
            {
                if (currentNode.Data[entryOffset + i] != 0xFF)
                {
                    allFF = false;
                }

                if (currentNode.Data[entryOffset + i] != 0x00)
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
            if (filenameOffset + nameLength > (uint)currentNode.Data.Length)
            {
                continue;
            }

            byte[] filenameBytes = new byte[nameLength];
            Array.Copy(currentNode.Data, filenameOffset, filenameBytes, 0, nameLength);
            string filename = Utilities.Windows1252.GetString(filenameBytes);
            string fullPath = string.IsNullOrEmpty(currentNode.ParentPath) ? filename : $"{currentNode.ParentPath}/{filename}";
            bool isDirectory = (attribute & 0x10) != 0;

            yield return new GdfxEntry
            {
                Name = filename,
                FullPath = fullPath,
                IsFile = !isDirectory,
                Size = isDirectory ? 0 : size,
                Sector = sector
            };

            // Push the right child first (so the left is processed first)
            if (right != 0 && right != 0xFFFF)
            {
                uint rightOffsetBytes = (uint)right * 4;
                if (rightOffsetBytes < (ulong)currentNode.Data.Length)
                {
                    directoryStack.Push(new DirectoryNode
                    {
                        Data = currentNode.Data,
                        Offset = right,
                        ParentPath = currentNode.ParentPath
                    });
                }
            }

            // Push left child
            if (left != 0 && left != 0xFFFF)
            {
                uint leftOffsetBytes = (uint)left * 4;
                if (leftOffsetBytes < (ulong)currentNode.Data.Length)
                {
                    directoryStack.Push(new DirectoryNode
                    {
                        Data = currentNode.Data,
                        Offset = left,
                        ParentPath = currentNode.ParentPath
                    });
                }
            }

            // If directory, add its contents to the stack
            if (isDirectory && size > 0)
            {
                byte[]? directoryData = ReadSectors(sector, size);
                if (directoryData != null)
                {
                    directoryStack.Push(new DirectoryNode
                    {
                        Data = directoryData,
                        Offset = 0,
                        ParentPath = fullPath
                    });
                }
            }
        }
    }

    /// <summary>
    /// Tries to extract the SPA (XDBF) file embedded in the ISO's default.xex.
    /// </summary>
    /// <param name="spaFile">The parsed <see cref="SpaFile"/> (caller must <c>Dispose()</c>) if found; otherwise null.</param>
    /// <returns>True if SPA was found and parsed, false otherwise.</returns>
    public bool TryGetSpaFile(out SpaFile? spaFile)
    {
        spaFile = null;
        try
        {
            if (XexFile is { IsValid: true })
            {
                return XexFile.TryGetSpaFile(out spaFile);
            }

            // Fallback: ISO valid but default.xex missing or invalid - try to locate an alternative XEX inside the ISO.
            byte[]? xexBytes = TryExtractAlternativeXex();
            if (xexBytes == null)
            {
                return false;
            }

            XexFile altXex = XexFile.FromBytes(xexBytes);
            if (!altXex.IsValid)
            {
                Logger.Trace<IsoFile>($"ISO alternative XEX invalid: {altXex.ValidationError}");
                return false;
            }

            return altXex.TryGetSpaFile(out spaFile);
        }
        catch (Exception ex)
        {
            Logger.Trace<IsoFile>($"TryGetSpaFile failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tries to extract the dashboard title icon PNG from the ISO's embedded XEX SPA (XDBF image <c>0x8000</c>).
    /// </summary>
    /// <returns>PNG bytes if found (valid <c>89 50 4E 47</c> header), null otherwise. Never throws.</returns>
    public byte[]? TryGetIcon()
    {
        try
        {
            if (XexFile is { IsValid: true })
            {
                byte[]? icon = XexFile.TryGetIcon();
                if (icon != null)
                {
                    Logger.Debug<IsoFile>($"ISO embedded XEX icon extracted ({icon.Length} bytes)");
                    return icon;
                }
            }

            byte[]? xexBytes = TryExtractAlternativeXex();
            if (xexBytes != null)
            {
                XexFile altXex = XexFile.FromBytes(xexBytes);
                if (altXex.IsValid)
                {
                    byte[]? icon = altXex.TryGetIcon();
                    if (icon != null)
                    {
                        Logger.Debug<IsoFile>($"ISO alternative XEX icon extracted ({icon.Length} bytes)");
                        return icon;
                    }
                }
            }

            Logger.Trace<IsoFile>("ISO TryGetIcon: no icon found");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Trace<IsoFile>($"TryGetIcon failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Tries to extract the NXE background image (<c>nxebg.jpg</c>) from the <c>nxeart</c> file embedded in the ISO's GDFX filesystem.
    /// </summary>
    /// <returns>JPEG bytes if found, null otherwise. Never throws.</returns>
    /// <remarks>
    /// Searches GDFX for <c>nxeart</c> via <see cref="FindFileInIso"/> (case-insensitive, any directory),
    /// then delegates to <see cref="Utilities.NxeArtHelper"/> which parses the inner PIRS/STFS and extracts <c>nxebg.jpg</c> at <c>0xE000</c>.
    /// </remarks>
    public byte[]? TryGetNxeBackground()
    {
        try
        {
            if (_sectorReader == null || XgdInformation == null)
            {
                Logger.Trace<IsoFile>("ISO TryGetNxeBackground: sector reader not initialized");
                return null;
            }

            byte[]? nxeartBytes = FindFileInIso("nxeart");
            if (nxeartBytes == null || nxeartBytes.Length == 0)
            {
                Logger.Trace<IsoFile>("ISO TryGetNxeBackground: nxeart not found in GDFX");
                return null;
            }

            byte[]? bg = Utilities.NxeArtHelper.TryExtractNxebgFromNxeart(nxeartBytes);
            if (bg != null)
            {
                Logger.Debug<IsoFile>($"ISO nxeart nxebg.jpg extracted ({bg.Length} bytes)");
                return bg;
            }

            Logger.Trace<IsoFile>("ISO TryGetNxeBackground: nxebg.jpg not found inside nxeart");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Trace<IsoFile>($"TryGetNxeBackground failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Searches the ISO file tree for any <c>.xex</c> file other than the already-tried <c>default.xex</c>.
    /// Used as fallback when <see cref="XexFile"/> is null or its icon is missing.
    /// </summary>
    /// <returns>First alternative XEX bytes found, or null.</returns>
    private byte[]? TryExtractAlternativeXex()
    {
        if (_sectorReader == null || XgdInformation == null || !IsValid)
        {
            return null;
        }

        try
        {
            foreach (GdfxEntry entry in WalkEntries())
            {
                if (!entry.IsFile || entry.Size == 0)
                {
                    continue;
                }

                bool isXex = entry.Name.EndsWith(".xex", StringComparison.OrdinalIgnoreCase);
                bool isDefault = entry.Name.Equals(IsoConstants.DEFAULT_EXECUTABLE_NAME, StringComparison.OrdinalIgnoreCase);
                if (isXex && !isDefault)
                {
                    byte[] fileData = ReadFile(entry);
                    if ((ulong)fileData.Length == entry.Size)
                    {
                        Logger.Trace<IsoFile>($"ISO alternative XEX candidate found: '{entry.FullPath}' ({entry.Size} bytes)");
                        return fileData;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Trace<IsoFile>($"TryExtractAlternativeXex failed: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Internal class representing a directory node during traversal.
    /// </summary>
    private sealed class DirectoryNode
    {
        public byte[] Data { get; init; } = Array.Empty<byte>();
        public uint Offset { get; init; }
        public string ParentPath { get; init; } = string.Empty;
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