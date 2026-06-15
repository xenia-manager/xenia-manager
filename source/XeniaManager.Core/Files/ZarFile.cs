using System.Buffers.Binary;
using System.Text;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Zar;
using ZstdSharp;

namespace XeniaManager.Core.Files;

/// <summary>
/// Handles loading, parsing, and extraction of files from ZArchive (.zar) archives.
/// ZAR is a compressed read-only archive format designed for random-access file reads,
/// using zstd compression on 64 KiB blocks with all metadata stored at the end of the file.
/// <para>
/// File Layout (built backwards):
/// - Compressed Data Blocks (zstd-compressed 64 KiB blocks)
/// - CompressionOffsetRecord[] (40 bytes each, maps block indices to positions)
/// - Name Table (length-prefixed UTF-8 strings)
/// - FileDirectoryEntry[] (16 bytes each, breadth-first directory tree)
/// - Footer (144 bytes, last thing in the file)
/// </para>
/// <para>
/// All multibyte integers are big-endian on disk.
/// File paths use case-insensitive comparison for A-Z/a-z only; non-ASCII compared byte-by-byte.
/// </para>
/// </summary>
public sealed class ZarFile : IDisposable
{
    private bool _disposed;
    private Stream? _stream;
    private readonly List<CompressionOffsetRecord> _offsetRecords;
    private readonly byte[] _nameTable;
    private readonly List<FileDirectoryEntry> _fileTree;
    private readonly ulong _compressedDataOffset;
    private List<DirEntry>? _files;
    private List<DirEntry>? _entries;

    /// <summary>
    /// Gets the parsed XEX file from the archive's default.xex.
    /// May be null if default.xex is not present or cannot be parsed.
    /// </summary>
    public XexFile? XexFile { get; private set; }

    /// <summary>
    /// Gets whether the ZAR archive was successfully parsed.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the validation error message if the archive is invalid.
    /// Returns null if the archive is valid.
    /// </summary>
    public string? ValidationError { get; private set; }

    /// <summary>
    /// Gets the path to the ZAR archive file.
    /// </summary>
    public string FilePath { get; private set; } = string.Empty;

    /// <summary>
    /// Gets a flat list of all files in the archive with their full paths and sizes.
    /// Lazily computed and cached on first access.
    /// </summary>
    public IReadOnlyList<DirEntry> Files
    {
        get
        {
            if (_files == null)
            {
                _files = new List<DirEntry>();
                CollectFiles(0, string.Empty, _files);
            }
            return _files;
        }
    }

    /// <summary>
    /// Gets a flat list of all entries (files and directories) in the archive
    /// with their full paths and sizes. Directories have IsFile = false and Size = 0.
    /// Lazily computed and cached on first access.
    /// </summary>
    public IReadOnlyList<DirEntry> Entries
    {
        get
        {
            if (_entries == null)
            {
                _entries = new List<DirEntry>();
                CollectEntries(0, string.Empty, _entries);
            }
            return _entries;
        }
    }

    /// <summary>
    /// Private constructor to enforce factory methods.
    /// </summary>
    /// <param name="stream">The open file stream for random-access reading.</param>
    /// <param name="records">The parsed compression offset records.</param>
    /// <param name="nameTable">The raw name table bytes.</param>
    /// <param name="fileTree">The parsed file directory entries in BFS order.</param>
    /// <param name="compressedDataOffset">The absolute offset of the compressed data section.</param>
    /// <param name="filePath">The path to the archive file.</param>
    private ZarFile(Stream stream, List<CompressionOffsetRecord> records, byte[] nameTable,
        List<FileDirectoryEntry> fileTree, ulong compressedDataOffset, string filePath)
    {
        _stream = stream;
        _offsetRecords = records;
        _nameTable = nameTable;
        _fileTree = fileTree;
        _compressedDataOffset = compressedDataOffset;
        FilePath = filePath;
    }

    /// <summary>
    /// Quickly checks whether a file is a valid ZAR archive by reading and validating its footer.
    /// </summary>
    /// <param name="path">The path to the file to check.</param>
    /// <returns>True if the file has a valid ZAR magic and version in its footer; false otherwise.</returns>
    public static bool IsZarArchive(string path)
    {
        try
        {
            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length < ZarFooter.Size)
            {
                return false;
            }

            byte[] footerData = new byte[ZarFooter.Size];
            fs.Seek(-ZarFooter.Size, SeekOrigin.End);
            ReadExact(fs, footerData, 0, ZarFooter.Size);

            uint version = BinaryPrimitives.ReadUInt32BigEndian(footerData.AsSpan(136));
            uint magic = BinaryPrimitives.ReadUInt32BigEndian(footerData.AsSpan(140));
            return magic == ZarFooter.ExpectedMagic && version == ZarFooter.ExpectedVersion;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Loads a ZAR archive from the specified path, parses its structure, and extracts default.xex if present.
    /// <para>
    /// Parsing steps:
    /// 1. Reads the 144-byte footer from the end of the file and validates magic/version/size
    /// 2. Reads compression offset records to enable random-access block decompression
    /// 3. Reads the name table containing all filenames and directory names
    /// 4. Reads the file tree (breadth-first directory structure)
    /// 5. Locates and extracts default.xex for TitleID/MediaID extraction
    /// </para>
    /// </summary>
    /// <param name="path">The path to the ZAR archive to load.</param>
    /// <returns>A new ZarFile instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static ZarFile Load(string path)
    {
        Logger.Debug<ZarFile>($"Loading ZAR archive from {path}");

        if (!File.Exists(path))
        {
            Logger.Error<ZarFile>($"ZAR file does not exist: {path}");
            throw new FileNotFoundException($"ZAR file does not exist at {path}", path);
        }

        FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        try
        {
            // Read and validate the 144-byte footer from the end of the file
            if (fs.Length < ZarFooter.Size)
            {
                throw new InvalidDataException("File too small for ZAR archive");
            }

            byte[] footerData = new byte[ZarFooter.Size];
            fs.Seek(-ZarFooter.Size, SeekOrigin.End);
            ReadExact(fs, footerData, 0, ZarFooter.Size);
            ZarFooter footer = ZarFooter.Read(footerData, 0);

            if (footer.Magic != ZarFooter.ExpectedMagic)
            {
                throw new InvalidDataException($"Bad ZAR magic: 0x{footer.Magic:X8}");
            }
            if (footer.Version != ZarFooter.ExpectedVersion)
            {
                throw new InvalidDataException($"Bad ZAR version: 0x{footer.Version:X8}");
            }
            if (footer.TotalSize != (ulong)fs.Length)
            {
                throw new InvalidDataException("ZAR total size mismatch");
            }

            Logger.Debug<ZarFile>($"File size: {fs.Length} bytes, " +
                                  $"CompressedData: 0x{footer.CompressedData.Offset:X} ({footer.CompressedData.Size} bytes), " +
                                  $"OffsetRecords: {footer.OffsetRecords.Size / CompressionOffsetRecord.Size} records, " +
                                  $"NameTable: {footer.Names.Size} bytes, " +
                                  $"FileTree: {footer.FileTree.Size / FileDirectoryEntry.Size} entries");

            // Read compression offset records that map block indices to compressed data positions
            int recordCount = (int)(footer.OffsetRecords.Size / CompressionOffsetRecord.Size);
            byte[] recordsData = new byte[footer.OffsetRecords.Size];
            fs.Seek((long)footer.OffsetRecords.Offset, SeekOrigin.Begin);
            ReadExact(fs, recordsData, 0, recordsData.Length);
            List<CompressionOffsetRecord> records = new List<CompressionOffsetRecord>(recordCount);
            for (int i = 0; i < recordCount; i++)
            {
                records.Add(CompressionOffsetRecord.Read(recordsData, i * CompressionOffsetRecord.Size));
            }

            // Read the name table containing all length-prefixed UTF-8 filenames
            byte[] nameTable = new byte[footer.Names.Size];
            fs.Seek((long)footer.Names.Offset, SeekOrigin.Begin);
            ReadExact(fs, nameTable, 0, nameTable.Length);

            // Read the file tree directory entries (breadth-first order)
            int entryCount = (int)(footer.FileTree.Size / FileDirectoryEntry.Size);
            byte[] treeData = new byte[footer.FileTree.Size];
            fs.Seek((long)footer.FileTree.Offset, SeekOrigin.Begin);
            ReadExact(fs, treeData, 0, treeData.Length);
            List<FileDirectoryEntry> fileTree = new List<FileDirectoryEntry>(entryCount);
            for (int i = 0; i < entryCount; i++)
            {
                fileTree.Add(FileDirectoryEntry.Read(treeData, i * FileDirectoryEntry.Size));
            }

            // Validate the root entry (must be a directory with empty name sentinel)
            if (fileTree[0].IsFile)
            {
                throw new InvalidDataException("Root entry must be a directory");
            }

            ZarFile zarFile = new ZarFile(fs, records, nameTable, fileTree, footer.CompressedData.Offset, path);
            zarFile.IsValid = true;

            // Count directories vs files in the archive structure
            int dirCount = fileTree.Count(e => !e.IsFile);
            int fileCount = fileTree.Count(e => e.IsFile);
            Logger.Info<ZarFile>($"ZAR structure parsed: {fileCount} files, {dirCount} directories");

            Logger.Debug<ZarFile>("Searching for default.xex in ZAR archive...");

            // Locate and extract default.xex for TitleID and MediaID extraction
            FileDirectoryEntry? defaultXexEntry = zarFile.Lookup("default.xex");
            if (defaultXexEntry != null)
            {
                byte[] xexData = zarFile.ReadFile(defaultXexEntry);
                Logger.Info<ZarFile>($"Found default.xex ({xexData.Length} bytes), parsing...");
                XexFile xex = XexFile.FromBytes(xexData);
                if (xex.IsValid)
                {
                    zarFile.XexFile = xex;
                    Logger.Info<ZarFile>($"Extracted from default.xex - TitleID: {zarFile.XexFile.TitleId}, MediaID: {zarFile.XexFile.MediaId}");
                }
                else
                {
                    Logger.Warning<ZarFile>($"default.xex found but could not be parsed: {xex.ValidationError}");
                }
            }
            else
            {
                Logger.Warning<ZarFile>("default.xex not found in ZAR archive");
            }

            Logger.Info<ZarFile>($"Successfully loaded ZAR archive: {path}");
            return zarFile;
        }
        catch (Exception ex)
        {
            fs.Dispose();
            ZarFile invalid = new ZarFile(Stream.Null, new List<CompressionOffsetRecord>(), Array.Empty<byte>(), new List<FileDirectoryEntry>(), 0, path)
            {
                ValidationError = $"Failed to load ZAR: {ex.Message}"
            };
            Logger.Error<ZarFile>(invalid.ValidationError);
            Logger.LogExceptionDetails<ZarFile>(ex);
            return invalid;
        }
    }

    /// <summary>
    /// Creates a ZarFile from raw byte data.
    /// Note: This method is not supported for ZAR archives as they require random-access stream reading.
    /// </summary>
    /// <param name="data">The raw byte data (not supported for ZAR).</param>
    /// <returns>An invalid ZarFile instance.</returns>
    public static ZarFile FromBytes(byte[] data)
    {
        Logger.Error<ZarFile>("ZarFile.FromBytes() is not supported - ZAR archives must be loaded from disk");
        return new ZarFile(Stream.Null, new List<CompressionOffsetRecord>(), Array.Empty<byte>(), new List<FileDirectoryEntry>(), 0, string.Empty)
        {
            ValidationError = "FromBytes is not supported for ZAR files - use Load() instead"
        };
    }

    /// <summary>
    /// Looks up a file or directory by its path within the archive.
    /// Supports both forward and backslash path separators.
    /// Comparison is case-insensitive for A-Z/a-z only (matching the archive format specification).
    /// </summary>
    /// <param name="path">The path to look up (e.g., "game/mesh.bin" or "default.xex").</param>
    /// <returns>The FileDirectoryEntry if found; null if the path does not exist.</returns>
    public FileDirectoryEntry? Lookup(string path)
    {
        string[] parts = path.Split('/', '\\', StringSplitOptions.RemoveEmptyEntries);
        uint currentNode = 0;

        Logger.Trace<ZarFile>($"Looking up path: '{path}' ({parts.Length} segments)");

        foreach (string part in parts)
        {
            FileDirectoryEntry dir = _fileTree[(int)currentNode];
            if (dir.IsFile)
            {
                Logger.Warning<ZarFile>($"Path '{path}' resolves to a file before reaching '{part}'");
                return null;
            }

            uint start = dir.NodeStartIndex;
            uint end = start + dir.Count;
            bool found = false;

            Logger.Trace<ZarFile>($"Searching for '{part}' in directory node {currentNode} ({start}-{end})");

            for (uint i = start; i < end; i++)
            {
                FileDirectoryEntry child = _fileTree[(int)i];
                string name = ReadName(child.NameOffset);
                if (NameEquals(name, part))
                {
                    Logger.Trace<ZarFile>($"Found '{part}' at node index {i} ({(child.IsFile ? "file" : "directory")})");
                    currentNode = i;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Logger.Trace<ZarFile>($"Segment '{part}' not found in path '{path}'");
                return null;
            }
        }

        FileDirectoryEntry result = _fileTree[(int)currentNode];
        Logger.Trace<ZarFile>($"Path '{path}' resolved to node {currentNode} ({(result.IsFile ? "file" : "directory")})");
        return result;
    }

    /// <summary>
    /// Lists the contents of a directory by path.
    /// </summary>
    /// <param name="path">The path to the directory to list (e.g., "game" or "" for root).</param>
    /// <returns>A list of DirEntry objects, or null if the path does not exist or is a file.</returns>
    public List<DirEntry>? ListDirectory(string path)
    {
        FileDirectoryEntry? entry = Lookup(path);
        if (entry == null || entry.IsFile)
        {
            return null;
        }
        return ListDirectoryChildren(entry);
    }

    /// <summary>
    /// Lists the contents of a directory by its node index in the file tree.
    /// </summary>
    /// <param name="nodeIdx">The index of the directory node in the file tree array.</param>
    /// <returns>A list of DirEntry objects, or null if the node index is invalid or is a file.</returns>
    public List<DirEntry>? ListDirectory(uint nodeIdx)
    {
        if (nodeIdx >= _fileTree.Count)
        {
            return null;
        }
        FileDirectoryEntry entry = _fileTree[(int)nodeIdx];
        if (entry.IsFile)
        {
            return null;
        }
        return ListDirectoryChildren(entry);
    }

    /// <summary>
    /// Reads the names and metadata of all child entries under a directory node.
    /// </summary>
    /// <param name="dir">The directory entry whose children to enumerate.</param>
    /// <returns>A list of DirEntry objects for each child.</returns>
    private List<DirEntry> ListDirectoryChildren(FileDirectoryEntry dir)
    {
        List<DirEntry> entries = new List<DirEntry>();
        uint start = dir.NodeStartIndex;
        uint end = start + dir.Count;

        for (uint i = start; i < end; i++)
        {
            FileDirectoryEntry child = _fileTree[(int)i];
            entries.Add(new DirEntry
            {
                Name = ReadName(child.NameOffset),
                IsFile = child.IsFile,
                Size = child.IsFile ? child.GetFileSize() : 0
            });
        }

        return entries;
    }

    /// <summary>
    /// Recursively collects all file entries under a node into the results list.
    /// </summary>
    private void CollectFiles(uint nodeIdx, string currentPath, List<DirEntry> results)
    {
        FileDirectoryEntry entry = _fileTree[(int)nodeIdx];
        if (entry.IsFile)
        {
            results.Add(new DirEntry
            {
                Name = currentPath,
                IsFile = true,
                Size = entry.GetFileSize()
            });
        }
        else
        {
            uint start = entry.NodeStartIndex;
            uint end = start + entry.Count;
            for (uint i = start; i < end; i++)
            {
                FileDirectoryEntry child = _fileTree[(int)i];
                string name = ReadName(child.NameOffset);
                string childPath = string.IsNullOrEmpty(currentPath) ? name : $"{currentPath}/{name}";
                CollectFiles(i, childPath, results);
            }
        }
    }

    /// <summary>
    /// Recursively collects all entries (files and directories) under a node into the results list.
    /// </summary>
    private void CollectEntries(uint nodeIdx, string currentPath, List<DirEntry> results)
    {
        FileDirectoryEntry entry = _fileTree[(int)nodeIdx];
        if (entry.IsFile)
        {
            results.Add(new DirEntry
            {
                Name = currentPath,
                IsFile = true,
                Size = entry.GetFileSize()
            });
        }
        else
        {
            uint start = entry.NodeStartIndex;
            uint end = start + entry.Count;

            for (uint i = start; i < end; i++)
            {
                FileDirectoryEntry child = _fileTree[(int)i];
                string name = ReadName(child.NameOffset);
                string childPath = string.IsNullOrEmpty(currentPath) ? name : $"{currentPath}/{name}";
                results.Add(new DirEntry
                {
                    Name = childPath,
                    IsFile = child.IsFile,
                    Size = child.IsFile ? child.GetFileSize() : 0
                });
                if (!child.IsFile)
                {
                    CollectEntries(i, childPath, results);
                }
            }
        }
    }

    /// <summary>
    /// Reads the full contents of a file identified by its path within the archive.
    /// </summary>
    /// <param name="path">The path to the file (e.g., "default.xex" or "game/mesh.bin").</param>
    /// <returns>The complete file data as a byte array, or null if the file was not found.</returns>
    public byte[]? ReadFile(string path)
    {
        FileDirectoryEntry? entry = Lookup(path);
        if (entry == null || !entry.IsFile)
        {
            return null;
        }
        return ReadFile(entry);
    }

    /// <summary>
    /// Reads the full contents of a file from a FileDirectoryEntry.
    /// </summary>
    /// <param name="entry">The file entry to read.</param>
    /// <returns>The complete file data as a byte array.</returns>
    public byte[] ReadFile(FileDirectoryEntry entry)
    {
        return ReadFile(entry, 0, entry.GetFileSize());
    }

    /// <summary>
    /// Reads a portion of a file starting at the specified offset with the specified length.
    /// Supports random-access reading by decompressing only the required 64 KiB blocks.
    /// </summary>
    /// <param name="entry">The file entry to read from.</param>
    /// <param name="offset">The byte offset within the file to start reading from.</param>
    /// <param name="length">The number of bytes to read.</param>
    /// <returns>The requested file data as a byte array. May be empty if offset is beyond the file size.</returns>
    public byte[] ReadFile(FileDirectoryEntry entry, ulong offset, ulong length)
    {
        ulong fileOffset = entry.GetFileOffset();
        ulong fileSize = entry.GetFileSize();

        if (offset >= fileSize)
        {
            Logger.Trace<ZarFile>($"ReadFile at offset {offset} beyond file size {fileSize}, returning empty");
            return Array.Empty<byte>();
        }
        ulong bytesToRead = Math.Min(length, fileSize - offset);

        Logger.Trace<ZarFile>($"Reading {bytesToRead} bytes from file at 0x{fileOffset:X} (offset {offset}, size {fileSize})");

        ulong rawOffset = fileOffset + offset;
        byte[] result = new byte[bytesToRead];
        ulong remaining = bytesToRead;
        ulong destOffset = 0;

        // Walk through the file block-by-block, decompressing only the blocks we need
        int blockCount = 0;
        while (remaining > 0)
        {
            ulong blockIdx = rawOffset / 65536;
            uint blockOff = (uint)(rawOffset % 65536);
            uint step = Math.Min((uint)remaining, 65536 - blockOff);

            byte[] block = DecompressBlock(blockIdx);
            Array.Copy(block, blockOff, result, (long)destOffset, step);

            rawOffset += step;
            remaining -= step;
            destOffset += step;
            blockCount++;
        }

        Logger.Trace<ZarFile>($"Read {bytesToRead} bytes from file using {blockCount} block(s)");
        return result;
    }

    /// <summary>
    /// Extracts all files from the archive to the specified output directory,
    /// preserving the directory structure.
    /// </summary>
    /// <param name="outputDir">The root output directory to extract files into.</param>
    public void ExtractAll(string outputDir)
    {
        Logger.Info<ZarFile>($"Extracting all files from {FilePath} to {outputDir}");
        ExtractNode(0, outputDir, string.Empty);
        Logger.Info<ZarFile>($"Extraction complete to {outputDir}");
    }

    /// <summary>
    /// Recursively extracts a file tree node to disk.
    /// For files, reads and writes the decompressed data.
    /// For directories, recursively processes all children.
    /// </summary>
    /// <param name="nodeIdx">The index of the current node in the file tree.</param>
    /// <param name="outputDir">The root output directory.</param>
    /// <param name="currentPath">The relative path built so far from the root.</param>
    private void ExtractNode(uint nodeIdx, string outputDir, string currentPath)
    {
        FileDirectoryEntry entry = _fileTree[(int)nodeIdx];

        if (entry.IsFile)
        {
            byte[] data = ReadFile(entry);
            string filePath = Path.Combine(outputDir, currentPath);
            string? parentDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }
            File.WriteAllBytes(filePath, data);
            Logger.Trace<ZarFile>($"Extracted: {currentPath} ({data.Length} bytes)");
        }
        else
        {
            uint start = entry.NodeStartIndex;
            uint end = start + entry.Count;

            for (uint i = start; i < end; i++)
            {
                FileDirectoryEntry child = _fileTree[(int)i];
                string name = ReadName(child.NameOffset);
                string childPath = string.IsNullOrEmpty(currentPath)
                    ? name
                    : Path.Combine(currentPath, name);
                ExtractNode(i, outputDir, childPath);
            }
        }
    }

    /// <summary>
    /// Locates, reads, and decompresses a single 64 KiB block by its uncompressed block index.
    /// <para>
    /// Block location algorithm:
    /// 1. Determine the CompressionOffsetRecord (blockIdx / 16) and sub-index (blockIdx % 16)
    /// 2. Sum preceding block sizes within the record to find the relative offset
    /// 3. Read compressed data from the stream at the computed absolute offset
    /// 4. If compressedSize == 65536, the block is stored uncompressed
    /// 5. Otherwise, decompress using zstd
    /// </para>
    /// </summary>
    /// <param name="blockIdx">The uncompressed block index (0-based).</param>
    /// <returns>The decompressed 64 KiB block data.</returns>
    /// <exception cref="InvalidDataException">Thrown when decompression yields an unexpected size.</exception>
    private byte[] DecompressBlock(ulong blockIdx)
    {
        uint recordIdx = (uint)(blockIdx / 16);
        uint subIdx = (uint)(blockIdx % 16);

        CompressionOffsetRecord record = _offsetRecords[(int)recordIdx];

        // Sum the compressed sizes of all preceding blocks in this record
        ulong offset = record.BaseOffset;
        for (uint i = 0; i < subIdx; i++)
        {
            offset += (ulong)record.Sizes[i] + 1;
        }

        // compressed size is stored as (actualSize - 1) to fit in a ushort
        uint compressedSize = (uint)(record.Sizes[subIdx] + 1);
        byte[] compressed = new byte[compressedSize];

        long absoluteOffset = (long)(_compressedDataOffset + offset);

        Logger.Trace<ZarFile>($"Decompressing block {blockIdx} (record {recordIdx}, subIdx {subIdx}, compressedSize {compressedSize}, offset 0x{absoluteOffset:X})");

        Stream stream = _stream!;
        stream.Seek(absoluteOffset, SeekOrigin.Begin);
        ReadExact(stream, compressed, 0, (int)compressedSize);

        // Stored uncompressed if zstd did not help
        if (compressedSize == 65536)
        {
            Logger.Trace<ZarFile>($"Block {blockIdx} is stored uncompressed ({compressedSize} bytes)");
            return compressed;
        }

        // Decompress with zstd (always produces exactly 65536 bytes per format spec)
        using Decompressor decompressor = new Decompressor();
        byte[] output = new byte[65536];
        int written = decompressor.Unwrap(compressed, output);
        if (written != 65536)
        {
            throw new InvalidDataException($"Unexpected decompressed size {written} for block {blockIdx} (expected 65536, compressed {compressedSize} bytes)");
        }
        Logger.Trace<ZarFile>($"Block {blockIdx} decompressed: {compressedSize} -> 65536 bytes");
        return output;
    }

    /// <summary>
    /// Reads a name from the name table at the specified offset.
    /// Names are length-prefixed UTF-8 strings with a variable-length header:
    /// short names (less than 128 chars) use a 1-byte header, longer names use 2 bytes.
    /// </summary>
    /// <param name="offset">The offset into the name table byte array.</param>
    /// <returns>The decoded name string, or empty string if the offset is the root sentinel or invalid.</returns>
    private string ReadName(uint offset)
    {
        // 0x7FFFFFFF is the sentinel for the root directory (empty name)
        if (offset == 0x7FFFFFFF)
        {
            return string.Empty;
        }
        if (offset >= _nameTable.Length)
        {
            Logger.Warning<ZarFile>($"Name offset 0x{offset:X} is beyond name table ({_nameTable.Length} bytes)");
            return string.Empty;
        }

        int nameLength = _nameTable[offset] & 0x7F;
        int readOffset = (int)offset;

        if ((_nameTable[offset] & 0x80) != 0)
        {
            // Long name: low 7 bits + next byte = 15-bit length
            if (offset + 1 >= _nameTable.Length)
            {
                Logger.Warning<ZarFile>($"Long name header at offset 0x{offset:X} overflows name table");
                return string.Empty;
            }
            nameLength |= (_nameTable[offset + 1] << 7);
            readOffset += 2;
        }
        else
        {
            // Short name: single byte with length in bits 0-6
            readOffset += 1;
        }

        if (readOffset + nameLength > _nameTable.Length)
        {
            Logger.Warning<ZarFile>($"Name at offset 0x{offset:X} (length {nameLength}) overflows name table");
            return string.Empty;
        }

        return Encoding.UTF8.GetString(_nameTable, readOffset, nameLength);
    }

    /// <summary>
    /// Compares two filenames using ZAR's case-insensitivity rules:
    /// only A-Z/a-z are folded (ASCII-only), non-ASCII characters compare byte-by-byte.
    /// </summary>
    /// <param name="a">First name to compare.</param>
    /// <param name="b">Second name to compare.</param>
    /// <returns>True if the names are equal per ZAR comparison rules.</returns>
    private static bool NameEquals(string a, string b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }
        for (int i = 0; i < a.Length; i++)
        {
            char ca = a[i], cb = b[i];
            if (ca >= 'A' && ca <= 'Z')
            {
                ca = (char)(ca - ('A' - 'a'));
            }
            if (cb >= 'A' && cb <= 'Z')
            {
                cb = (char)(cb - ('A' - 'a'));
            }
            if (ca != cb)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Reads exactly <paramref name="count"/> bytes from the stream into the buffer.
    /// Unlike Stream.Read, this guarantees the requested number of bytes are read
    /// or throws EndOfStreamException.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    /// <param name="buffer">The buffer to write data into.</param>
    /// <param name="offset">The byte offset in the buffer to start writing at.</param>
    /// <param name="count">The exact number of bytes to read.</param>
    /// <exception cref="EndOfStreamException">Thrown when the stream ends before all bytes are read.</exception>
    private static void ReadExact(Stream stream, byte[] buffer, int offset, int count)
    {
        int read = 0;
        while (read < count)
        {
            int n = stream.Read(buffer, offset + read, count - read);
            if (n == 0)
            {
                throw new EndOfStreamException("Unexpected end of stream");
            }
            read += n;
        }
    }

    /// <summary>
    /// Disposes of the ZAR archive resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the ZAR archive resources.
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
            _stream?.Dispose();
            _stream = null;
        }
        _disposed = true;
    }

    /// <summary>
    /// Finalizer to ensure the stream is closed if Dispose was not called.
    /// </summary>
    ~ZarFile()
    {
        Dispose(false);
    }
}