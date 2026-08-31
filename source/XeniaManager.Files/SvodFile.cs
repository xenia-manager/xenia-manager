using System.Buffers.Binary;
using System.Text;
using XeniaManager.Files.Models.Iso;
using XeniaManager.Files.Models.Stfs;
using XeniaManager.Files.Models.Svod;
using XeniaManager.Logging;

namespace XeniaManager.Files;

/// <summary>
/// Handles loading and parsing of SVOD (GOD / Installed Game) packages – the disc-based STFS variant.
/// </summary>
/// <remarks>
/// <para>
/// SVOD is STFS with <c>DescriptorType == 1</c> and a <see cref="SvodVolumeDescriptor"/> at <c>0x379</c>.
/// STFS header (<c>CON/LIVE/PIRS</c>), license area (<c>0x22C</c>) and metadata (thumbnail at <c>0x171A</c>)
/// are shared with STFS, so the thumbnail is the fastest icon source.
/// </para>
/// <para>
/// File structure:
/// <list type="bullet">
/// <item>STFS header at <c>0x0</c> (magic, header size, metadata at <c>0x22C</c>, <see cref="SvodVolumeDescriptor"/> at <c>0x379</c>, thumbnail at <c>0x171A</c>).</item>
/// <item>Data files <c>Data0000…</c> (each <c>0xA290000</c> bytes) with hash tables every <c>0x198</c> sectors.</item>
/// <item>GDFX at <c>baseAddress</c> (<c>0x12000</c> or <c>0x2000</c>) containing <c>default.xex</c>.</item>
/// </list>
/// </para>
/// <para>
/// <c>TryGetIcon</c> returns the STFS thumbnail first; only when no valid thumbnail exists does it
/// decrypt the XEX and extract the SPA/XDBF image <c>0x8000</c> (like <see cref="StfsFile.TryGetIcon"/>).
/// </para>
/// </remarks>
public sealed class SvodFile : IDisposable
{
    private bool _disposed;
    private List<FileStream>? _dataStreams;
    private SvodVolumeDescriptor _svodDescriptor;
    private StfsMetadata _metadata = new StfsMetadata();
    private XgdInfo? _xgdInfo;
    private int _baseAddress;
    private int _sectorOffset;
    private SvodLayout _svodLayout;
    private int _svodBaseOffset;
    private int _magicOffset;

    /// <summary>
    /// Gets the parsed XEX file from the SVOD's <c>default.xex</c>.
    /// </summary>
    public XexFile? XexFile { get; private set; }

    /// <summary>
    /// Gets whether the SVOD package was successfully parsed.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the validation error message if the package is invalid.
    /// </summary>
    public string? ValidationError { get; private set; }

    /// <summary>
    /// Gets the path to the SVOD package (file or directory).
    /// </summary>
    public string FilePath { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the STFS metadata (shared header at <c>0x22C</c>, contains thumbnail at <c>0x171A</c>).
    /// </summary>
    public StfsMetadata Metadata
    {
        get
        {
            return _metadata;
        }
        private set
        {
            _metadata = value;
        }
    }

    /// <summary>
    /// Gets the SVOD volume descriptor.
    /// </summary>
    public SvodVolumeDescriptor SvodDescriptor
    {
        get
        {
            return _svodDescriptor;
        }
        private set
        {
            _svodDescriptor = value;
        }
    }

    private SvodFile()
    {
        IsValid = false;
    }

    /// <summary>
    /// Determines SVOD layout (Enhanced / XSF / Single / Multiple) and sets <see cref="_svodBaseOffset"/>, <see cref="_magicOffset"/>, <see cref="_baseAddress"/>, <see cref="_sectorOffset"/>.
    /// </summary>
    private void DetermineSvodLayout()
    {
        // Defaults for normal multi-file GOD (most common)
        _svodLayout = SvodLayout.MultipleFiles;
        _svodBaseOffset = 0x0000;
        _magicOffset = 0x2000;
        _baseAddress = 0x12000; // legacy fallback, overwritten below
        _sectorOffset = 0x1000;

        if (_svodDescriptor.IsEnhancedGdfLayout)
        {
            _svodLayout = SvodLayout.EnhancedGdf;
            _svodBaseOffset = 0x0000;
            _magicOffset = 0x2000;
            _baseAddress = 0x2000;
            _sectorOffset = 0x2000;
            Logger.Debug<SvodFile>($"SVOD layout EnhancedGDF base 0x{_svodBaseOffset:X} magic 0x{_magicOffset:X}");
            return;
        }

        if (IsXsfLayout())
        {
            _svodLayout = SvodLayout.Xsf;
            _svodBaseOffset = 0x10000;
            _magicOffset = 0x12000;
            _baseAddress = 0x12000;
            _sectorOffset = 0x1000; // XSF still uses 0x1000 sector offset for data (base is separate)
            Logger.Debug<SvodFile>($"SVOD layout XSF base 0x{_svodBaseOffset:X} magic 0x{_magicOffset:X}");
            return;
        }

        // Single vs multiple: single-file GOD has only the header file (or header+data as one file)
        bool isSingleFile = _dataStreams != null && _dataStreams.Count == 1;
        if (isSingleFile)
        {
            _svodLayout = SvodLayout.SingleFile;
            _svodBaseOffset = 0xB000;
            _magicOffset = 0xD000;
            _baseAddress = 0xD000;
            _sectorOffset = 0x1000;
            Logger.Debug<SvodFile>($"SVOD layout SingleFile base 0x{_svodBaseOffset:X} magic 0x{_magicOffset:X}");
            return;
        }

        _svodLayout = SvodLayout.MultipleFiles;
        _svodBaseOffset = 0x0000;
        _magicOffset = 0x2000;
        _baseAddress = 0x12000; // keep legacy 0x12000 for Forza-like multi that has MEDIA at 0x12000 raw (XSF already handled, but some multi may still have 0x12000)
        // For pure multiple without XSF, Xenia uses 0x2000, but Velocity uses 0x12000. Try to detect which has MEDIA
        try
        {
            if (_dataStreams != null && _dataStreams.Count > 0)
            {
                FileStream first = _dataStreams[0];
                if (first.Length >= 0x12020)
                {
                    long pos = first.Position;
                    byte[] buf = new byte[20];
                    first.Seek(0x12000, SeekOrigin.Begin);
                    int r = first.Read(buf, 0, 20);
                    first.Seek(pos, SeekOrigin.Begin);
                    string m = Encoding.ASCII.GetString(buf, 0, Math.Min(r, 20)).Trim('\0');
                    if (m == IsoConstants.XGD_IMAGE_MAGIC)
                    {
                        _magicOffset = 0x12000;
                        _baseAddress = 0x12000;
                        Logger.Debug<SvodFile>($"SVOD layout MultipleFiles (MEDIA at 0x12000) base 0x{_svodBaseOffset:X} magic 0x{_magicOffset:X}");
                        return;
                    }

                    first.Seek(0x2000, SeekOrigin.Begin);
                    r = first.Read(buf, 0, 20);
                    first.Seek(pos, SeekOrigin.Begin);
                    m = Encoding.ASCII.GetString(buf, 0, Math.Min(r, 20)).Trim('\0');
                    if (m == IsoConstants.XGD_IMAGE_MAGIC)
                    {
                        _magicOffset = 0x2000;
                        _baseAddress = 0x2000;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Trace<SvodFile>($"DetermineSvodLayout fallback probe failed: {ex.Message}");
        }

        Logger.Debug<SvodFile>($"SVOD layout MultipleFiles base 0x{_svodBaseOffset:X} magic 0x{_magicOffset:X}");
    }

    /// <summary>
    /// Detects XSF layout by checking for <c>"XSF"</c> at <c>0x2000</c> and <c>MICROSOFT*XBOX*MEDIA</c> at <c>0x12000</c> raw in the first data file.
    /// </summary>
    private bool IsXsfLayout()
    {
        try
        {
            if (_dataStreams == null || _dataStreams.Count == 0)
            {
                return false;
            }

            FileStream first = _dataStreams[0];
            if (first.Length < 0x12020)
            {
                return false;
            }

            long pos = first.Position;
            byte[] buf12000 = new byte[20];
            byte[] buf2000 = new byte[20];
            first.Seek(0x12000, SeekOrigin.Begin);
            int r1 = first.Read(buf12000, 0, 20);
            first.Seek(0x2000, SeekOrigin.Begin);
            int r2 = first.Read(buf2000, 0, 20);
            first.Seek(pos, SeekOrigin.Begin);
            string m12000 = r1 >= 20 ? Encoding.ASCII.GetString(buf12000).Trim('\0') : string.Empty;
            string m2000 = r2 >= 3 ? Encoding.ASCII.GetString(buf2000, 0, 3) : string.Empty;
            return m12000 == IsoConstants.XGD_IMAGE_MAGIC && m2000 == "XSF";
        }
        catch (Exception ex)
        {
            Logger.Trace<SvodFile>($"IsXsfLayout probe failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Maps a GDFX sector (as block) to physical byte offset in a data file and file index.
    /// </summary>
    /// <param name="sector">GDFX sector number (as in directory entry).</param>
    /// <param name="address">Physical byte offset within the target data file.</param>
    /// <param name="fileIndex">Index of the data file containing the sector.</param>
    private void BlockToOffset(uint sector, out long address, out int fileIndex)
    {
        const long BLOCK_SIZE = 0x800;
        const long HASH_BLOCK_SIZE = 0x1000;
        const long BLOCKS_PER_L0_HASH = 0x198;
        const long HASHES_PER_L1_HASH = 0xA1C4;
        const long BLOCKS_PER_FILE = 0x14388;
        const long MAX_FILE_SIZE = 0xA290000;
        long blockOffset = _svodDescriptor.DataBlockOffset;

        long trueBlock = (long)sector - blockOffset * 2;
        if (_svodDescriptor.IsEnhancedGdfLayout)
        {
            trueBlock += 2;
        }

        // Header area (trueBlock <0) should not be called for data sectors; fallback to direct
        if (trueBlock < 0)
        {
            address = sector * BLOCK_SIZE;
            fileIndex = 0;
            return;
        }

        long fileBlock = trueBlock % BLOCKS_PER_FILE;
        long fileIdx = trueBlock / BLOCKS_PER_FILE;

        long level0Count = fileBlock / BLOCKS_PER_L0_HASH + 1;
        long level1Count = level0Count / HASHES_PER_L1_HASH + 1;
        long offset = level0Count * HASH_BLOCK_SIZE + level1Count * HASH_BLOCK_SIZE;
        if (_svodLayout == SvodLayout.SingleFile)
        {
            offset += _svodBaseOffset; // 0xB000
        }

        long blockAddress = fileBlock * BLOCK_SIZE + offset;
        if (blockAddress >= MAX_FILE_SIZE)
        {
            fileIdx++;
            blockAddress %= MAX_FILE_SIZE;
            blockAddress += 0x2000;
        }

        address = blockAddress;
        fileIndex = (int)fileIdx;
    }

    /// <summary>
    /// Loads an SVOD package from the specified path (file or directory).
    /// </summary>
    /// <param name="path">Path to a single SVOD file or directory containing Data files.</param>
    /// <returns>A new <see cref="SvodFile"/> instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the path does not exist.</exception>
    public static SvodFile Load(string path)
    {
        Logger.Debug<SvodFile>($"Loading SVOD package from {path}");

        if (!File.Exists(path) && !Directory.Exists(path))
        {
            Logger.Error<SvodFile>($"SVOD path does not exist: {path}");
            throw new FileNotFoundException($"SVOD path does not exist at {path}", path);
        }

        SvodFile svod = new SvodFile
        {
            FilePath = path
        };

        try
        {
            // Resolve header and data files for GOD layout
            string headerPath;
            List<string> dataFilePaths;
            ResolveSvodPaths(path, out headerPath, out dataFilePaths);

            if (string.IsNullOrEmpty(headerPath) || !File.Exists(headerPath))
            {
                svod.ValidationError = "No SVOD header file found";
                Logger.Error<SvodFile>(svod.ValidationError);
                return svod;
            }

            if (dataFilePaths.Count == 0)
            {
                dataFilePaths = [headerPath];
            }

            Logger.Debug<SvodFile>($"SVOD header: {headerPath}, data files: {dataFilePaths.Count}");

            List<FileStream> streams = new List<FileStream>();
            foreach (string p in dataFilePaths)
            {
                FileStream fs = new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read);
                streams.Add(fs);
            }

            svod._dataStreams = streams;

            // Read STFS header (first 0xA000 bytes)
            byte[] header = new byte[0xA000];
            int headerLen = 0;
            using (FileStream hdr = new FileStream(headerPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int toRead = (int)Math.Min(header.Length, hdr.Length);
                headerLen = hdr.Read(header, 0, toRead);
                if (headerLen < 0x3AA)
                {
                    svod.ValidationError = "Header too short for SVOD";
                    Logger.Error<SvodFile>(svod.ValidationError);
                    svod.DisposeStreams();
                    return svod;
                }
            }

            try
            {
                svod._metadata = StfsMetadata.FromBytes(header, SignatureType.CON);
                Logger.Info<SvodFile>(
                    $"SVOD metadata parsed: {svod._metadata.DisplayName} (Type: {svod._metadata.ContentType}, DescriptorType: {svod._metadata.DescriptorType})");
            }
            catch (Exception ex)
            {
                Logger.Warning<SvodFile>($"Failed to parse STFS metadata for SVOD: {ex.Message}");
                svod._metadata = new StfsMetadata();
            }

            if (header.Length >= 0x379 + SvodVolumeDescriptor.Size)
            {
                svod._svodDescriptor = SvodVolumeDescriptor.FromBytes(header, 0x379);
                Logger.Debug<SvodFile>(
                    $"SVOD descriptor: Flags 0x{svod._svodDescriptor.Flags:X2} Enhanced={svod._svodDescriptor.IsEnhancedGdfLayout} DataBlockCount={svod._svodDescriptor.DataBlockCount} DataBlockOffset={svod._svodDescriptor.DataBlockOffset}");
            }
            else
            {
                svod.ValidationError = "Header too short for SVOD descriptor";
                Logger.Error<SvodFile>(svod.ValidationError);
                svod.DisposeStreams();
                return svod;
            }

            svod.DetermineSvodLayout();
            Logger.Debug<SvodFile>(
                $"SVOD layout {svod._svodLayout} baseOffset 0x{svod._svodBaseOffset:X} magicOffset 0x{svod._magicOffset:X} GDFX baseAddress 0x{svod._baseAddress:X}, sectorOffset 0x{svod._sectorOffset:X}");

            if (svod._metadata.DescriptorType != 1)
            {
                Logger.Warning<SvodFile>($"SVOD DescriptorType is {svod._metadata.DescriptorType}, expected 1, continuing anyway");
            }

            if (!svod.TryInitializeGdfx())
            {
                Logger.Warning<SvodFile>("SVOD GDFX header not found or invalid, but package may still be valid for thumbnail");
                svod.IsValid = true;
                return svod;
            }

            Logger.Info<SvodFile>($"SVOD GDFX initialized - RootDirSector: {svod._xgdInfo!.RootDirSector}, RootDirSize: {svod._xgdInfo.RootDirSize}");

            if (!svod.ExtractAndParseDefaultXex())
            {
                Logger.Warning<SvodFile>($"SVOD default.xex not found or invalid: {svod.ValidationError}");
                svod.IsValid = true;
                return svod;
            }

            svod.IsValid = true;
            Logger.Info<SvodFile>($"Successfully parsed SVOD - TitleID: {svod.XexFile!.TitleId}, MediaID: {svod.XexFile.MediaId}");
        }
        catch (Exception ex)
        {
            svod.ValidationError = $"Failed to parse SVOD: {ex.Message}";
            Logger.Error<SvodFile>(svod.ValidationError);
            Logger.LogExceptionDetails<SvodFile>(ex);
            svod.DisposeStreams();
        }

        return svod;
    }

    /// <summary>
    /// Creates an SvodFile from raw byte data (not supported, SVOD requires disk).
    /// </summary>
    public static SvodFile FromBytes(byte[] data)
    {
        Logger.Error<SvodFile>("SvodFile.FromBytes() is not supported - SVOD packages must be loaded from disk");
        return new SvodFile
        {
            ValidationError = "FromBytes is not supported for SVOD files - use Load() instead"
        };
    }

    /// <summary>
    /// Quickly checks whether a path is a valid SVOD package.
    /// </summary>
    /// <param name="path">Path to file or directory to test.</param>
    /// <returns>True if the path resolves to a header with SVOD descriptor (type 1).</returns>
    public static bool IsSvodPackage(string path)
    {
        try
        {
            string headerPath;
            List<string> dataFiles;
            ResolveSvodPaths(path, out headerPath, out dataFiles);

            if (string.IsNullOrEmpty(headerPath) || !File.Exists(headerPath))
            {
                headerPath = path;
                if (Directory.Exists(path))
                {
                    List<string> files = ResolveDataFilePaths(path);
                    if (files.Count == 0)
                    {
                        return false;
                    }

                    headerPath = files[0];
                }

                if (!File.Exists(headerPath))
                {
                    return false;
                }
            }

            using FileStream fs = new FileStream(headerPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length < 0x3AA)
            {
                return false;
            }

            byte[] header = new byte[0x400];
            fs.Seek(0, SeekOrigin.Begin);
            int read = fs.Read(header, 0, header.Length);
            if (read < 0x3AA)
            {
                return false;
            }

            string magic = Encoding.ASCII.GetString(header, 0, 4);
            if (magic is not ("CON " or "PIRS" or "LIVE"))
            {
                return false;
            }

            int descriptorType = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(0x3A9));
            return descriptorType == 1;
        }
        catch
        {
            return false;
        }
    }

    private static List<string> ResolveDataFilePaths(string path)
    {
        if (File.Exists(path))
        {
            return [path];
        }

        if (Directory.Exists(path))
        {
            string[] files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly)
                .Where(f =>
                {
                    string name = Path.GetFileName(f);
                    return name.StartsWith("Data", StringComparison.OrdinalIgnoreCase) || name.Equals("header", StringComparison.OrdinalIgnoreCase) ||
                           name.StartsWith("data", StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (files.Length == 0)
            {
                files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly)
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            return files.ToList();
        }

        return new List<string>();
    }

    /// <summary>
    /// Resolves the GOD header and data file paths for the three supported layouts.
    /// </summary>
    private static void ResolveSvodPaths(string path, out string headerPath, out List<string> dataFilePaths)
    {
        headerPath = string.Empty;
        dataFilePaths = new List<string>();

        if (File.Exists(path))
        {
            headerPath = path;
            string dataDir = path + ".data";
            if (Directory.Exists(dataDir))
            {
                dataFilePaths = Directory.GetFiles(dataDir, "*", SearchOption.TopDirectoryOnly)
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            else
            {
                dataFilePaths = [path];
            }

            return;
        }

        if (Directory.Exists(path))
        {
            string trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string dirName = Path.GetFileName(trimmed);
            if (dirName.EndsWith(".data", StringComparison.OrdinalIgnoreCase))
            {
                string parent = Path.GetDirectoryName(trimmed) ?? string.Empty;
                string headerCandidate = Path.Combine(parent, dirName.Substring(0, dirName.Length - 5));
                if (File.Exists(headerCandidate) && IsSvodHeaderFile(headerCandidate))
                {
                    headerPath = headerCandidate;
                }
                else
                {
                    headerPath = FindSvodHeaderInDirectory(parent) ?? headerCandidate;
                }

                dataFilePaths = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly)
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return;
            }

            string? headerInDir = FindSvodHeaderInDirectory(path);
            if (!string.IsNullOrEmpty(headerInDir))
            {
                headerPath = headerInDir;
                string dataDir = headerInDir + ".data";
                if (Directory.Exists(dataDir))
                {
                    dataFilePaths = Directory.GetFiles(dataDir, "*", SearchOption.TopDirectoryOnly)
                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                else
                {
                    dataFilePaths = [headerInDir];
                }

                return;
            }

            string[] files = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly)
                .Where(f => Path.GetFileName(f).StartsWith("Data", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (files.Length > 0)
            {
                headerPath = files[0];
                dataFilePaths = files.ToList();
                return;
            }

            List<string> allFiles = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (allFiles.Count > 0)
            {
                headerPath = allFiles[0];
                dataFilePaths = allFiles;
            }
        }
    }

    private static string? FindSvodHeaderInDirectory(string directory)
    {
        try
        {
            foreach (string file in Directory.GetFiles(directory, "*", SearchOption.TopDirectoryOnly).OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                if (IsSvodHeaderFile(file))
                {
                    return file;
                }
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private static bool IsSvodHeaderFile(string filePath)
    {
        try
        {
            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length < 0x3AA)
            {
                return false;
            }

            byte[] header = new byte[0x400];
            fs.Seek(0, SeekOrigin.Begin);
            int read = fs.Read(header, 0, header.Length);
            if (read < 0x3AA)
            {
                return false;
            }

            string magic = Encoding.ASCII.GetString(header, 0, 4);
            if (magic is not ("CON " or "PIRS" or "LIVE"))
            {
                return false;
            }

            int descriptorType = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(0x3A9));
            return descriptorType == 1;
        }
        catch { return false; }
    }

    /// <summary>
    /// Initializes the GDFX volume descriptor from the SVOD data files.
    /// Uses <see cref="_magicOffset"/> raw read, with fallback to <see cref="_baseAddress"/> and Xenia layout probing.
    /// </summary>
    private bool TryInitializeGdfx()
    {
        if (_dataStreams == null || _dataStreams.Count == 0)
        {
            return false;
        }

        try
        {
            byte[] headerSector = new byte[IsoConstants.SECTOR_SIZE];
            // Primary: magicOffset as determined by layout
            if (TryReadSvodSectorForGdfxHeader(headerSector, _magicOffset))
            {
                if (TryParseGdfxHeader(headerSector))
                {
                    return true;
                }
            }

            // Fallback: try legacy baseAddress and other common offsets (0x2000, 0x12000, 0xD000)
            int[] fallbacks = [_baseAddress, 0x2000, 0x12000, 0xD000, 0x10000];
            foreach (int off in fallbacks.Distinct())
            {
                if (off == _magicOffset)
                {
                    continue;
                }

                if (TryReadSvodSectorForGdfxHeader(headerSector, off))
                {
                    if (TryParseGdfxHeader(headerSector))
                    {
                        Logger.Debug<SvodFile>($"SVOD GDFX header found via fallback at 0x{off:X}");
                        _magicOffset = off;
                        return true;
                    }
                }
            }

            Logger.Trace<SvodFile>($"SVOD GDFX header magic mismatch at magicOffset 0x{_magicOffset:X} base 0x{_baseAddress:X}");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Trace<SvodFile>($"TryInitializeGdfx failed: {ex.Message}");
            Logger.LogExceptionDetails<SvodFile>(ex);
            return false;
        }
    }

    /// <summary>
    /// Parses a GDFX header sector and populates <see cref="_xgdInfo"/> if the magic is valid.
    /// </summary>
    /// <param name="headerSector">2048-byte sector containing the GDFX volume descriptor.</param>
    /// <returns>True if the magic was valid and <see cref="_xgdInfo"/> was populated.</returns>
    private bool TryParseGdfxHeader(byte[] headerSector)
    {
        string magic = Encoding.ASCII.GetString(headerSector, 0, 20).Trim('\0');
        string magicTail = Encoding.ASCII.GetString(headerSector, 0x800 - 20, 20).Trim('\0');
        if (magic != IsoConstants.XGD_IMAGE_MAGIC || magicTail != IsoConstants.XGD_IMAGE_MAGIC)
        {
            return false;
        }

        uint rootSector = BinaryPrimitives.ReadUInt32LittleEndian(headerSector.AsSpan(20));
        uint rootSize = BinaryPrimitives.ReadUInt32LittleEndian(headerSector.AsSpan(24));
        long creationTime = BinaryPrimitives.ReadInt64LittleEndian(headerSector.AsSpan(28));

        _xgdInfo = new XgdInfo
        {
            BaseSector = 0,
            RootDirSector = rootSector,
            RootDirSize = rootSize,
            CreationDateTime = DateTime.FromFileTime(creationTime)
        };
        return true;
    }

    private bool TryReadSvodSectorForGdfxHeader(byte[] sectorData) => TryReadSvodSectorForGdfxHeader(sectorData, _magicOffset);

    /// <summary>
    /// Reads a raw sector for GDFX header probing at the given byte offset in the first data file.
    /// Does not use hash translation; the GDFX header is at a raw offset (e.g., 0x2000, 0x12000, 0xD000) depending on layout.
    /// </summary>
    private bool TryReadSvodSectorForGdfxHeader(byte[] sectorData, int offset)
    {
        if (_dataStreams == null || _dataStreams.Count == 0)
        {
            return false;
        }

        try
        {
            FileStream first = _dataStreams[0];
            if (first.Length < offset + sectorData.Length)
            {
                return false;
            }

            long pos = first.Position;
            first.Seek(offset, SeekOrigin.Begin);
            int read = first.Read(sectorData, 0, sectorData.Length);
            first.Seek(pos, SeekOrigin.Begin);
            return read == sectorData.Length;
        }
        catch (Exception ex)
        {
            Logger.Trace<SvodFile>($"TryReadSvodSectorForGdfxHeader at 0x{offset:X} failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Extracts and parses <c>default.xex</c> from the GDFX filesystem.
    /// </summary>
    private bool ExtractAndParseDefaultXex()
    {
        try
        {
            byte[]? data = FindFileInSvod(IsoConstants.DEFAULT_EXECUTABLE_NAME);
            if (data == null || data.Length == 0)
            {
                ValidationError = "default.xex not found in SVOD";
                Logger.Warning<SvodFile>(ValidationError);
                return false;
            }

            Logger.Info<SvodFile>($"Found default.xex in SVOD ({data.Length} bytes), parsing...");
            XexFile xex = XexFile.FromBytes(data);
            if (!xex.IsValid)
            {
                ValidationError = $"default.xex is invalid: {xex.ValidationError}";
                Logger.Warning<SvodFile>(ValidationError);
                return false;
            }

            XexFile = xex;
            return true;
        }
        catch (Exception ex)
        {
            ValidationError = $"Failed to extract default.xex: {ex.Message}";
            Logger.Error<SvodFile>(ValidationError);
            Logger.LogExceptionDetails<SvodFile>(ex);
            return false;
        }
    }

    /// <summary>
    /// Finds and extracts a file from the SVOD's GDFX filesystem.
    /// </summary>
    private byte[]? FindFileInSvod(string fileName)
    {
        if (_dataStreams == null || _xgdInfo == null)
        {
            return null;
        }

        uint rootSectors = (_xgdInfo.RootDirSize + IsoConstants.SECTOR_SIZE - 1) / IsoConstants.SECTOR_SIZE;
        byte[] rootData = new byte[_xgdInfo.RootDirSize];
        for (uint i = 0; i < rootSectors; i++)
        {
            uint sector = _xgdInfo.RootDirSector + i;
            if (!TryReadSvodSector(sector, out byte[] sectorData))
            {
                Logger.Error<SvodFile>($"Failed to read SVOD root directory sector {sector}");
                return null;
            }

            uint offset = i * IsoConstants.SECTOR_SIZE;
            uint length = Math.Min(IsoConstants.SECTOR_SIZE, _xgdInfo.RootDirSize - offset);
            Array.Copy(sectorData, 0, rootData, offset, length);
        }

        Stack<DirectoryNode> stack = new Stack<DirectoryNode>();
        stack.Push(new DirectoryNode
        {
            Data = rootData,
            Offset = 0
        });

        while (stack.Count > 0)
        {
            DirectoryNode node = stack.Pop();
            if (node.Offset * 4 >= (uint)node.Data.Length)
            {
                continue;
            }

            uint entryOffset = node.Offset * 4;
            if (entryOffset + 14 > (uint)node.Data.Length)
            {
                continue;
            }

            byte[] headerBuffer = new byte[14];
            Array.Copy(node.Data, entryOffset, headerBuffer, 0, 14);

            ushort left = (ushort)(headerBuffer[0] | (headerBuffer[1] << 8));
            ushort right = (ushort)(headerBuffer[2] | (headerBuffer[3] << 8));
            uint sector = (uint)(headerBuffer[4] | (headerBuffer[5] << 8) | (headerBuffer[6] << 16) | (headerBuffer[7] << 24));
            uint size = (uint)(headerBuffer[8] | (headerBuffer[9] << 8) | (headerBuffer[10] << 16) | (headerBuffer[11] << 24));
            byte attribute = headerBuffer[12];
            byte nameLen = headerBuffer[13];

            bool allFF = true, allZero = true;
            for (int j = 0; j < 14; j++)
            {
                if (headerBuffer[j] != 0xFF)
                {
                    allFF = false;
                }

                if (headerBuffer[j] != 0x00)
                {
                    allZero = false;
                }
            }

            if (allFF || allZero || nameLen == 0)
            {
                continue;
            }

            uint filenameOffset = entryOffset + 14;
            if (filenameOffset + nameLen > (uint)node.Data.Length)
            {
                continue;
            }

            string filename = Encoding.ASCII.GetString(node.Data, (int)filenameOffset, nameLen);

            if (filename.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            {
                if ((attribute & 0x10) != 0)
                {
                    Logger.Error<SvodFile>($"Found {fileName} but it's a directory");
                    return null;
                }

                if (size == 0)
                {
                    return Array.Empty<byte>();
                }

                byte[] fileData = new byte[size];
                uint processed = 0;
                uint readSector = sector;
                while (processed < size)
                {
                    if (!TryReadSvodSector(readSector, out byte[] sectorData))
                    {
                        Logger.Error<SvodFile>($"Failed to read SVOD file sector {readSector}");
                        return null;
                    }

                    uint toCopy = Math.Min(size - processed, IsoConstants.SECTOR_SIZE);
                    Array.Copy(sectorData, 0, fileData, processed, toCopy);
                    readSector++;
                    processed += toCopy;
                }

                Logger.Info<SvodFile>($"Successfully extracted {fileName} from SVOD ({fileData.Length} bytes)");
                return fileData;
            }

            if (right != 0 && right != 0xFFFF)
            {
                uint ro = (uint)right * 4;
                if (ro < (ulong)node.Data.Length)
                {
                    stack.Push(new DirectoryNode
                    {
                        Data = node.Data,
                        Offset = right
                    });
                }
            }

            if (left != 0 && left != 0xFFFF)
            {
                uint lo = (uint)left * 4;
                if (lo < (ulong)node.Data.Length)
                {
                    stack.Push(new DirectoryNode
                    {
                        Data = node.Data,
                        Offset = left
                    });
                }
            }

            if ((attribute & 0x10) != 0 && size > 0)
            {
                uint dirSectors = (size + IsoConstants.SECTOR_SIZE - 1) / IsoConstants.SECTOR_SIZE;
                byte[] dirData = new byte[size];
                for (uint i = 0; i < dirSectors; i++)
                {
                    uint s = sector + i;
                    if (TryReadSvodSector(s, out byte[] sd))
                    {
                        uint off = i * IsoConstants.SECTOR_SIZE;
                        uint len = Math.Min(IsoConstants.SECTOR_SIZE, size - off);
                        Array.Copy(sd, 0, dirData, off, len);
                    }
                }

                stack.Push(new DirectoryNode
                {
                    Data = dirData,
                    Offset = 0
                });
            }
        }

        Logger.Warning<SvodFile>($"File {fileName} not found in SVOD");
        return null;
    }

    /// <summary>
    /// Reads a GDFX sector from the SVOD data files using the hash-table mapping.
    /// Handles Enhanced, XSF, Single and Multiple layouts and file-index overflow at <c>0xA290000</c>.
    /// </summary>
    private bool TryReadSvodSector(uint sector, out byte[] sectorData)
    {
        sectorData = new byte[IsoConstants.SECTOR_SIZE];
        if (_dataStreams == null || _dataStreams.Count == 0)
        {
            return false;
        }

        try
        {
            long blockOffset = _svodDescriptor.DataBlockOffset;
            // Header area: sectors before the hashed data area are stored linearly.
            if (sector < (uint)(blockOffset * 2))
            {
                long directOffset = sector * IsoConstants.SECTOR_SIZE;
                if (directOffset + sectorData.Length <= _dataStreams[0].Length)
                {
                    lock (_dataStreams[0])
                    {
                        _dataStreams[0].Seek(directOffset, SeekOrigin.Begin);
                        int read = _dataStreams[0].Read(sectorData, 0, sectorData.Length);
                        if (read == sectorData.Length)
                        {
                            return true;
                        }
                    }
                }

                // For multi-file GOD the header's first sectors may be in the header file itself;
                // fall through to hashed handling which will also fail gracefully and be logged.
                Logger.Trace<SvodFile>($"TryReadSvodSector direct read failed for sector {sector} at offset 0x{directOffset:X}");
                return false;
            }

            long address;
            int fileIndex;
            BlockToOffset(sector, out address, out fileIndex);

            if (fileIndex < 0 || fileIndex >= _dataStreams.Count)
            {
                Logger.Trace<SvodFile>($"TryReadSvodSector sector {sector} fileIndex {fileIndex} out of range (count {_dataStreams.Count})");
                return false;
            }

            FileStream stream = _dataStreams[fileIndex];
            if (address + sectorData.Length > stream.Length)
            {
                Logger.Trace<SvodFile>($"TryReadSvodSector sector {sector} address 0x{address:X} file {fileIndex} beyond length 0x{stream.Length:X}");
                return false;
            }

            lock (stream)
            {
                stream.Seek(address, SeekOrigin.Begin);
                int read = stream.Read(sectorData, 0, sectorData.Length);
                return read == sectorData.Length;
            }
        }
        catch (Exception ex)
        {
            Logger.Trace<SvodFile>($"TryReadSvodSector failed for sector {sector}: {ex.Message}");
            Logger.LogExceptionDetails<SvodFile>(ex);
            return false;
        }
    }

    /// <summary>
    /// Tries to extract the SPA (XDBF) file embedded in the SVOD's <c>default.xex</c>.
    /// </summary>
    public bool TryGetSpaFile(out SpaFile? spaFile)
    {
        spaFile = null;
        try
        {
            if (XexFile is { IsValid: true })
            {
                return XexFile.TryGetSpaFile(out spaFile);
            }

            byte[]? xexBytes = TryExtractAlternativeXex();
            if (xexBytes == null)
            {
                return false;
            }

            XexFile altXex = XexFile.FromBytes(xexBytes);
            if (!altXex.IsValid)
            {
                Logger.Trace<SvodFile>($"SVOD alternative XEX invalid: {altXex.ValidationError}");
                return false;
            }

            return altXex.TryGetSpaFile(out spaFile);
        }
        catch (Exception ex)
        {
            Logger.Trace<SvodFile>($"TryGetSpaFile failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tries to extract the dashboard title icon from the SVOD package.
    /// </summary>
    /// <remarks>Thumbnail first, XEX SPA last (same order as <see cref="StfsFile.TryGetIcon"/>).</remarks>
    public byte[]? TryGetIcon()
    {
        try
        {
            if (Metadata.ThumbnailImage is { Length: > 0 } thumb && IsValidImageData(thumb))
            {
                Logger.Debug<SvodFile>($"SVOD ThumbnailImage ({thumb.Length} bytes)");
                return thumb;
            }

            if (Metadata.TitleThumbnailImage is { Length: > 0 } titleThumb && IsValidImageData(titleThumb))
            {
                Logger.Debug<SvodFile>($"SVOD TitleThumbnailImage ({titleThumb.Length} bytes)");
                return titleThumb;
            }

            if (XexFile is { IsValid: true })
            {
                byte[]? icon = XexFile.TryGetIcon();
                if (icon != null)
                {
                    Logger.Debug<SvodFile>($"SVOD embedded XEX icon extracted ({icon.Length} bytes)");
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
                        Logger.Debug<SvodFile>($"SVOD alternative XEX icon extracted ({icon.Length} bytes)");
                        return icon;
                    }
                }
            }

            Logger.Trace<SvodFile>("SVOD TryGetIcon: no icon found");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Trace<SvodFile>($"TryGetIcon failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Scans the GDFX tree for any <c>*.xex</c> other than <c>default.xex</c>.
    /// </summary>
    private byte[]? TryExtractAlternativeXex()
    {
        if (_dataStreams == null || _xgdInfo == null || !IsValid)
        {
            return null;
        }

        try
        {
            uint rootSectors = (_xgdInfo.RootDirSize + IsoConstants.SECTOR_SIZE - 1) / IsoConstants.SECTOR_SIZE;
            byte[] rootData = new byte[_xgdInfo.RootDirSize];
            for (uint i = 0; i < rootSectors; i++)
            {
                uint sector = _xgdInfo.RootDirSector + i;
                if (!TryReadSvodSector(sector, out byte[] sd))
                {
                    return null;
                }

                uint offset = i * IsoConstants.SECTOR_SIZE;
                uint len = Math.Min(IsoConstants.SECTOR_SIZE, _xgdInfo.RootDirSize - offset);
                Array.Copy(sd, 0, rootData, offset, len);
            }

            Stack<DirectoryNode> stack = new Stack<DirectoryNode>();
            stack.Push(new DirectoryNode
            {
                Data = rootData,
                Offset = 0
            });

            while (stack.Count > 0)
            {
                DirectoryNode node = stack.Pop();
                if (node.Offset * 4 >= (uint)node.Data.Length)
                {
                    continue;
                }

                uint entryOffset = node.Offset * 4;
                if (entryOffset + 14 > (uint)node.Data.Length)
                {
                    continue;
                }

                byte[] hb = new byte[14];
                Array.Copy(node.Data, entryOffset, hb, 0, 14);
                ushort left = (ushort)(hb[0] | (hb[1] << 8));
                ushort right = (ushort)(hb[2] | (hb[3] << 8));
                uint sector = (uint)(hb[4] | (hb[5] << 8) | (hb[6] << 16) | (hb[7] << 24));
                uint size = (uint)(hb[8] | (hb[9] << 8) | (hb[10] << 16) | (hb[11] << 24));
                byte attr = hb[12];
                byte nameLen = hb[13];
                bool allFF = true, allZero = true;
                for (int j = 0; j < 14; j++)
                {
                    if (hb[j] != 0xFF)
                    {
                        allFF = false;
                    }

                    if (hb[j] != 0x00)
                    {
                        allZero = false;
                    }
                }

                if (allFF || allZero || nameLen == 0)
                {
                    continue;
                }

                uint fnOff = entryOffset + 14;
                if (fnOff + nameLen > (uint)node.Data.Length)
                {
                    continue;
                }

                string filename = Encoding.ASCII.GetString(node.Data, (int)fnOff, nameLen);
                bool isXex = filename.EndsWith(".xex", StringComparison.OrdinalIgnoreCase);
                bool isDefault = filename.Equals(IsoConstants.DEFAULT_EXECUTABLE_NAME, StringComparison.OrdinalIgnoreCase);
                if (isXex && !isDefault && (attr & 0x10) == 0 && size > 0)
                {
                    byte[] fileData = new byte[size];
                    uint processed = 0;
                    uint rs = sector;
                    while (processed < size)
                    {
                        if (!TryReadSvodSector(rs, out byte[] sd))
                        {
                            break;
                        }

                        uint toCopy = Math.Min(size - processed, IsoConstants.SECTOR_SIZE);
                        Array.Copy(sd, 0, fileData, processed, toCopy);
                        rs++;
                        processed += toCopy;
                    }

                    if (processed == size)
                    {
                        Logger.Trace<SvodFile>($"SVOD alternative XEX candidate found: '{filename}' ({size} bytes)");
                        return fileData;
                    }
                }

                if (right != 0 && right != 0xFFFF)
                {
                    uint ro = (uint)right * 4;
                    if (ro < (ulong)node.Data.Length)
                    {
                        stack.Push(new DirectoryNode
                        {
                            Data = node.Data,
                            Offset = right
                        });
                    }
                }

                if (left != 0 && left != 0xFFFF)
                {
                    uint lo = (uint)left * 4;
                    if (lo < (ulong)node.Data.Length)
                    {
                        stack.Push(new DirectoryNode
                        {
                            Data = node.Data,
                            Offset = left
                        });
                    }
                }

                if ((attr & 0x10) != 0 && size > 0)
                {
                    uint dirSectors = (size + IsoConstants.SECTOR_SIZE - 1) / IsoConstants.SECTOR_SIZE;
                    byte[] dirData = new byte[size];
                    for (uint i = 0; i < dirSectors; i++)
                    {
                        uint s = sector + i;
                        if (TryReadSvodSector(s, out byte[] sd))
                        {
                            uint off = i * IsoConstants.SECTOR_SIZE;
                            uint len = Math.Min(IsoConstants.SECTOR_SIZE, size - off);
                            Array.Copy(sd, 0, dirData, off, len);
                        }
                    }

                    stack.Push(new DirectoryNode
                    {
                        Data = dirData,
                        Offset = 0
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Trace<SvodFile>($"TryExtractAlternativeXex failed: {ex.Message}");
        }

        return null;
    }

    private static bool IsValidImageData(byte[] data)
    {
        if (data.Length < 8)
        {
            return false;
        }

        bool isPng = data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47
                     && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A;
        if (isPng)
        {
            return true;
        }

        bool isJpeg = data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF;
        return isJpeg;
    }

    private void DisposeStreams()
    {
        if (_dataStreams != null)
        {
            foreach (FileStream s in _dataStreams)
            {
                s.Dispose();
            }

            _dataStreams = null;
        }
    }

    private sealed class DirectoryNode
    {
        public byte[] Data { get; init; } = Array.Empty<byte>();
        public uint Offset { get; init; }
    }

    /// <summary>
    /// Disposes of the SVOD file resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        DisposeStreams();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    ~SvodFile()
    {
        if (!_disposed)
        {
            DisposeStreams();
        }
    }
}