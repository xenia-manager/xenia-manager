using System.Buffers.Binary;
using System.Text;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Stfs;

namespace XeniaManager.Core.Files;

/// <summary>
/// Handles loading, parsing, and extraction of files from STFS (Secure Transacted File System) packages.
/// STFS is used by the Xbox 360 for all packages created and downloaded by the system.
/// Package types include CON (console signed), LIVE, and PIRS (Microsoft signed).
/// </summary>
public class StfsFile : IDisposable
{
    private bool _disposed;
    private readonly byte[] _rawData;

    /// <summary>
    /// Gets the package name (filename without extension).
    /// Set this when loading from a file to preserve the original name.
    /// </summary>
    public string? PackageName { get; set; }

    /// <summary>
    /// The signature type of the package (CON, LIVE, or PIRS).
    /// </summary>
    public SignatureType SignatureType { get; private set; }

    /// <summary>
    /// The package signature bytes.
    /// </summary>
    public byte[] Signature { get; private set; } = Array.Empty<byte>();

    /// <summary>
    /// The public key certificate (for CON packages).
    /// </summary>
    public byte[] PublicKeyCertificate { get; private set; } = Array.Empty<byte>();

    /// <summary>
    /// The content ID from the package header (20 bytes).
    /// </summary>
    public byte[] ContentId { get; private set; } = Array.Empty<byte>();

    /// <summary>
    /// The metadata contained in the package.
    /// </summary>
    public StfsMetadata Metadata { get; private set; } = new StfsMetadata();

    /// <summary>
    /// List of all file entries in the STFS package.
    /// </summary>
    public List<StfsFileEntry> FileEntries { get; private set; } = new List<StfsFileEntry>();

    /// <summary>
    /// Gets the header size from metadata.
    /// </summary>
    public int HeaderSize => Metadata.HeaderSize;

    /// <summary>
    /// Gets the block size (always 0x1000 / 4096 bytes).
    /// </summary>
    public const int BlockSize = 0x1000;

    /// <summary>
    /// Gets the offset where the data section starts (aligned header size).
    /// Calculated as ((HeaderSize + 0xFFF) & 0xF000).
    /// Typically, 0xB000 for standard packages, making the first data block at 0xC000.
    /// </summary>
    public int DataSectionStart => ((HeaderSize + 0xFFF) & 0xF000);

    // Constants for hash table calculations
    private static readonly uint[] kBlocksPerHashLevel = [170, 28900, 4913000];
    private const uint kEndOfChain = 0xFFFFFF;
    private uint blocksPerHashTable;

    /// <summary>
    /// Rounds up a value to the nearest multiple of the specified alignment.
    /// </summary>
    /// <param name="value">The value to round up.</param>
    /// <param name="alignment">The alignment.</param>
    /// <returns>The rounded-up value.</returns>
    private static int RoundUp(int value, int alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    /// <summary>
    /// Private constructor to enforce factory methods.
    /// </summary>
    /// <param name="data">The raw package data.</param>
    private StfsFile(byte[] data)
    {
        _rawData = data;
        // blocksPerHashTable will be set after metadata is parsed
        // Default to 1, will be updated in FromBytes after metadata parsing
        blocksPerHashTable = 1;
    }

    /// <summary>
    /// Loads an STFS package from the specified file path.
    /// Automatically detects the package type based on the magic number.
    /// The package name is automatically set from the filename.
    /// </summary>
    /// <param name="filePath">The path to the STFS package file.</param>
    /// <returns>A new StfsFile instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when the file has an invalid magic number.</exception>
    public static StfsFile Load(string filePath)
    {
        Logger.Debug<StfsFile>($"Loading STFS package from {filePath}");

        if (!File.Exists(filePath))
        {
            Logger.Error<StfsFile>($"STFS package does not exist: {filePath}");
            throw new FileNotFoundException($"STFS package does not exist at {filePath}", filePath);
        }

        byte[] fileData = File.ReadAllBytes(filePath);
        Logger.Info<StfsFile>($"Loaded STFS package: {filePath} ({fileData.Length} bytes)");

        StfsFile stfs = FromBytes(fileData);

        // Set the package name from the filename (without extension)
        stfs.PackageName = Path.GetFileName(filePath);
        Logger.Debug<StfsFile>($"Package name set to: {stfs.PackageName}");

        return stfs;
    }

    /// <summary>
    /// Parses an STFS package from raw bytes.
    /// </summary>
    /// <param name="data">The raw byte data of the STFS package.</param>
    /// <returns>A new StfsFile instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the magic number is invalid.</exception>
    public static StfsFile FromBytes(byte[] data)
    {
        Logger.Trace<StfsFile>($"Parsing STFS package from bytes ({data.Length} bytes)");
        Logger.Trace<StfsFile>($"First 64 bytes: {BitConverter.ToString(data.Take(64).ToArray())}");

        if (data.Length < 4)
        {
            Logger.Error<StfsFile>("Data too short to contain STFS magic number");
            throw new ArgumentException("Data too short to be an STFS package", nameof(data));
        }

        // Read the magic number
        string magic = Encoding.ASCII.GetString(data, 0, 4);
        Logger.Debug<StfsFile>($"STFS Magic: '{magic}'");
        Logger.Trace<StfsFile>($"Magic bytes: {BitConverter.ToString(data.Take(4).ToArray())}");

        // Determine signature types first (needed for constructor)
        SignatureType signatureType = magic switch
        {
            "CON " => SignatureType.CON,
            "PIRS" => SignatureType.PIRS,
            "LIVE" => SignatureType.LIVE,
            _ => throw new ArgumentException($"Invalid STFS magic number: '{magic}'. Expected 'CON ', 'PIRS', or 'LIVE'")
        };

        StfsFile stfs = new StfsFile(data);
        stfs.SignatureType = signatureType;

        Logger.Info<StfsFile>($"STFS package type: {stfs.SignatureType}");

        // Parse signature based on type
        if (stfs.SignatureType == SignatureType.CON)
        {
            // CON signature structure (0x22C bytes total)
            // 0x004: Cert Size (2), Console ID (5), Part Number (0x14), Type (1), Date (8)
            // 0x028: Public Exponent (4), Modulus (0x80), Cert Signature (0x100)
            // 0x1AC: Signature (0x80)
            Logger.Trace<StfsFile>($"CON Signature block (0x004-0x24B): {BitConverter.ToString(data.Skip(0x004).Take(64).ToArray())}");
            ushort certSize = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(0x004));
            Logger.Debug<StfsFile>($"Certificate Size: 0x{certSize:X4}");

            stfs.PublicKeyCertificate = new byte[0x1AC];
            Array.Copy(data, 0x004, stfs.PublicKeyCertificate, 0, 0x1AC);
            Logger.Trace<StfsFile>($"Public Key Certificate (first 32 bytes): {BitConverter.ToString(stfs.PublicKeyCertificate.Take(32).ToArray())}");

            stfs.Signature = new byte[0x80];
            Array.Copy(data, 0x1AC, stfs.Signature, 0, 0x80);
            Logger.Trace<StfsFile>($"CON Signature: {BitConverter.ToString(stfs.Signature)}");
        }
        else
        {
            // LIVE/PIRS signature structure
            // 0x004: Signature (0x100), Padding (0x128)
            // Total: 0x22C bytes before metadata
            Logger.Trace<StfsFile>($"LIVE/PIRS Signature block (0x004-0x103): {BitConverter.ToString(data.Skip(0x004).Take(64).ToArray())}");
            stfs.Signature = new byte[0x100];
            Array.Copy(data, 0x004, stfs.Signature, 0, 0x100);
            Logger.Trace<StfsFile>($"LIVE/PIRS Signature: {BitConverter.ToString(stfs.Signature)}");
            Logger.Trace<StfsFile>($"Padding after signature (0x104-0x22B): {BitConverter.ToString(data.Skip(0x104).Take(64).ToArray())}");
        }

        // Content ID is at offset 0x218 (20 bytes)
        stfs.ContentId = new byte[0x14];
        Array.Copy(data, 0x218, stfs.ContentId, 0, 0x14);
        Logger.Trace<StfsFile>($"Content ID: {BitConverter.ToString(stfs.ContentId)}");

        // Parse metadata
        Logger.Debug<StfsFile>($"Parsing STFS metadata...");
        Logger.Trace<StfsFile>($"Metadata block (0x22C-0x3FF): {BitConverter.ToString(data.Skip(0x22C).Take(64).ToArray())}");
        stfs.Metadata = StfsMetadata.FromBytes(data, stfs.SignatureType);
        Logger.Info<StfsFile>($"Metadata parsed: {stfs.Metadata.DisplayName} (Type: {stfs.Metadata.ContentType}, Version: {stfs.Metadata.MetadataVersion})");

        // Set blocksPerHashTable from volume descriptor flags
        // Could try to use SignatureType instead, but for now this works
        stfs.blocksPerHashTable = (uint)stfs.Metadata.VolumeDescriptor.BlocksPerHashTable;
        Logger.Debug<StfsFile>($"Blocks per hash table: {stfs.blocksPerHashTable} (from volume descriptor flags: 0x{stfs.Metadata.VolumeDescriptor.Flags:X2})");

        // Parse file table
        Logger.Debug<StfsFile>($"Parsing file table...");
        stfs.ParseFileTable();
        Logger.Info<StfsFile>($"Successfully parsed STFS package with {stfs.FileEntries.Count} file entries");

        return stfs;
    }

    /// <summary>
    /// Parses the file table from the STFS package.
    /// The file table location is determined from the volume descriptor.
    /// </summary>
    private void ParseFileTable()
    {
        int fileTableBlockNumber = Metadata.VolumeDescriptor.FileTableBlockNumber;
        int fileTableBlockCount = Metadata.VolumeDescriptor.FileTableBlockCount;

        Logger.Debug<StfsFile>($"File Table Block Number: {fileTableBlockNumber}, Block Count: {fileTableBlockCount}");
        Logger.Trace<StfsFile>($"Volume Descriptor bytes: {BitConverter.ToString(Metadata.VolumeDescriptor.ToBytes())}");

        int fileTableOffset = BlockNumberToOffset(fileTableBlockNumber);
        Logger.Debug<StfsFile>($"File Table Offset: 0x{fileTableOffset:X8}");

        // Validate file table offset - if it's beyond the file size, the package is corrupted or incomplete
        if (fileTableOffset < 0 || fileTableOffset >= _rawData.Length)
        {
            Logger.Warning<StfsFile>($"Invalid file table offset 0x{fileTableOffset:X8} (file size: {_rawData.Length} bytes). Package may be corrupted or incomplete.");
            return;
        }

        Logger.Trace<StfsFile>($"Raw bytes at file table offset: {BitConverter.ToString(_rawData.Skip(fileTableOffset).Take(128).ToArray())}");

        // Read file entries
        int offset = fileTableOffset;
        int entriesRead = 0;

        while (entriesRead < fileTableBlockCount * (BlockSize / StfsFileEntry.Size))
        {
            // Check if there's enough data remaining for a file entry
            if (offset + StfsFileEntry.Size > _rawData.Length)
            {
                Logger.Warning<StfsFile>($"Insufficient data remaining for file entry at offset 0x{offset:X8} (remaining: {_rawData.Length - offset} bytes)");
                break;
            }

            Logger.Trace<StfsFile>($"Reading file entry at offset 0x{offset:X8}: {BitConverter.ToString(_rawData.Skip(offset).Take(64).ToArray())}");

            StfsFileEntry entry = StfsFileEntry.FromBytes(_rawData, offset);

            // Check for empty entry (end of file table)
            if (string.IsNullOrEmpty(entry.FileName) || entry.Flags == 0)
            {
                Logger.Debug<StfsFile>($"Found empty entry at offset 0x{offset:X8}, ending file table parsing");
                break;
            }

            FileEntries.Add(entry);
            Logger.Trace<StfsFile>($"File Entry {entriesRead}: {entry}");
            Logger.Trace<StfsFile>($"  - FileName: '{entry.FileName}' (NameLength: {entry.NameLength})");
            Logger.Trace<StfsFile>($"  - Flags: 0x{entry.Flags:X2} (IsDirectory: {entry.IsDirectory}, HasConsecutiveBlocks: {entry.HasConsecutiveBlocks})");
            Logger.Trace<StfsFile>($"  - ValidDataBlocks: {entry.ValidDataBlocks}, AllocatedDataBlocks: {entry.AllocatedDataBlocks}");
            Logger.Trace<StfsFile>($"  - StartingBlock: {entry.StartingBlock}");
            Logger.Trace<StfsFile>($"  - PathIndicator: {entry.PathIndicator}, FileSize: {entry.FileSize}");
            Logger.Trace<StfsFile>($"  - UpdateDateTime: {entry.UpdateDateTime}, AccessDateTime: {entry.AccessDateTime}");

            offset += StfsFileEntry.Size;
            entriesRead++;
        }

        Logger.Info<StfsFile>($"Parsed {FileEntries.Count} file entries from file table");
    }

    /// <summary>
    /// Converts a block number to a file offset.
    /// Based on Xenia's BlockToOffset implementation.
    /// For every level there is a hash table:
    /// Level 0: hash table of next 170 blocks
    /// Level 1: hash table of next 170 hash tables  
    /// Level 2: hash table of next 170 level 1 hash tables
    /// Note: Data block 0 is at offset DataSectionStart + BlockSize for CON packages,
    /// or DataSectionStart + 2*BlockSize for PIRS/LIVE packages (due to blocksPerHashTable).
    /// </summary>
    /// <param name="blockNumber">The block number to convert.</param>
    /// <returns>The file offset corresponding to the block number.</returns>
    public int BlockNumberToOffset(int blockNumber)
    {
        if (blockNumber > 0xFFFFFF)
        {
            return -1;
        }

        // For every level there is a hash table
        // Level 0: hash table of next 170 blocks
        // Level 1: hash table of next 170 hash tables
        // Level 2: hash table of next 170 level 1 hash tables...
        uint block = (uint)blockNumber;
        for (uint i = 0; i < 3; i++)
        {
            uint levelBase = kBlocksPerHashLevel[i];
            block += ((uint)blockNumber + levelBase) / levelBase * blocksPerHashTable;
            if (blockNumber < levelBase)
            {
                break;
            }
        }

        // Data starts after the aligned header size
        // The first block at DataSectionStart is reserved for hash tables
        // Data block 0 comes after the initial hash table block(s)
        int dataStartOffset = RoundUp(HeaderSize, BlockSize);
        return dataStartOffset + ((int)block << 12);
    }

    /// <summary>
    /// Converts a hash table block number to a file offset.
    /// Based on Xenia's BlockToHashBlockOffset implementation.
    /// </summary>
    /// <param name="blockNumber">The hash table block number.</param>
    /// <param name="level">The hash table level (0, 1, or 2).</param>
    /// <returns>The file offset corresponding to the hash table block.</returns>
    private int HashTableBlockNumberToOffset(int blockNumber, int level = 0)
    {
        if (blockNumber > 0xFFFFFF)
        {
            return -1;
        }

        uint block = BlockToHashBlockNumber(blockNumber, level);
        return RoundUp(HeaderSize, BlockSize) + ((int)block << 12);
    }

    /// <summary>
    /// Calculates the hash block number for a given block index and hash level.
    /// Based on Xenia's BlockToHashBlockNumber implementation.
    /// </summary>
    /// <param name="blockIndex">The block index.</param>
    /// <param name="hashLevel">The hash level (0, 1, or 2).</param>
    /// <returns>The hash block number.</returns>
    private uint BlockToHashBlockNumber(int blockIndex, int hashLevel)
    {
        // Calculate blockStep values (same as Xenia's SetupContainer)
        // block_step[0] = 170 + blocksPerHashTable
        // block_step[1] = 28900 + ((170 + 1) * blocksPerHashTable)
        uint blockStep0 = kBlocksPerHashLevel[0] + blocksPerHashTable;
        uint blockStep1 = kBlocksPerHashLevel[1] + ((kBlocksPerHashLevel[0] + 1) * blocksPerHashTable);

        if (hashLevel == 2)
        {
            return blockStep1;
        }

        if (blockIndex < kBlocksPerHashLevel[hashLevel])
        {
            return hashLevel == 0 ? 0u : blockStep0;
        }

        uint block = ((uint)blockIndex / kBlocksPerHashLevel[hashLevel]) * (hashLevel == 0 ? blockStep0 : blockStep1);

        if (hashLevel == 0)
        {
            block += ((uint)blockIndex / kBlocksPerHashLevel[1] + 1) * blocksPerHashTable;

            if (blockIndex < kBlocksPerHashLevel[1])
            {
                return block;
            }
        }

        return block + blocksPerHashTable;
    }

    /// <summary>
    /// Gets the offset of a hash table entry for a given block number.
    /// Based on Xenia's GetBlockHash implementation.
    /// </summary>
    /// <param name="blockNumber">The block number.</param>
    /// <param name="level">The hash table level (0, 1, or 2).</param>
    /// <returns>The offset of the hash table entry.</returns>
    private int GetHashTableOffset(int blockNumber, int level = 0)
    {
        // For read-only packages (CON), we only need to check level 0
        if (SignatureType == SignatureType.CON)
        {
            level = 0;
        }

        uint hashTableBlock = BlockToHashBlockNumber(blockNumber, level);
        int hashTableOffset = HashTableBlockNumberToOffset((int)hashTableBlock, level);

        // Calculate entry index within the hash table
        uint record = (uint)blockNumber % kBlocksPerHashLevel[0];
        if (level >= 1)
        {
            record = ((uint)blockNumber / kBlocksPerHashLevel[level - 1]) % kBlocksPerHashLevel[0];
        }

        // Each hash entry is 0x18 (24) bytes: 0x14 SHA1 + 0x04 info
        return hashTableOffset + ((int)record * 0x18);
    }

    /// <summary>
    /// Extracts a file with non-consecutive blocks using the hash table.
    /// Based on Xenia's ReadEntry implementation for STFS files.
    /// </summary>
    /// <param name="entry">The file entry.</param>
    /// <returns>The file data.</returns>
    private byte[] ExtractNonConsecutiveFile(StfsFileEntry entry)
    {
        Logger.Trace<StfsFile>($"ExtractNonConsecutiveFile: {entry.FileName}, StartBlock={entry.StartingBlock}, Size={entry.FileSize}, Blocks={entry.AllocatedDataBlocks}");

        byte[] data = new byte[entry.FileSize];
        int offset = 0;
        uint currentBlock = (uint)entry.StartingBlock;
        int blocksRemaining = entry.AllocatedDataBlocks;

        while (offset < entry.FileSize && blocksRemaining > 0 && currentBlock != kEndOfChain)
        {
            // Read the hash table entry for this block (level 0)
            int hashTableOffset = GetHashTableOffset((int)currentBlock, level: 0);
            Logger.Trace<StfsFile>($"  Block {currentBlock}: Hash table offset 0x{hashTableOffset:X8}");

            if (hashTableOffset >= _rawData.Length)
            {
                Logger.Warning<StfsFile>($"Hash table offset out of bounds for block {currentBlock}");
                break;
            }

            // Hash table entry structure (StfsHashEntry - 0x18 bytes):
            // 0x00-0x13: SHA1 hash (0x14 bytes)
            // 0x14-0x17: info_raw (uint32_t little endian)
            //   - Bits 0-23: level0_next_block
            //   - Bits 30-31: level0_allocation_state (2 = in use)
            Logger.Trace<StfsFile>($"  Hash table entry: {BitConverter.ToString(_rawData.Skip(hashTableOffset).Take(24).ToArray())}");

            // Read info_raw as little-endian uint32
            uint infoRaw = (uint)(_rawData[hashTableOffset + 0x14] |
                                  (_rawData[hashTableOffset + 0x15] << 8) |
                                  (_rawData[hashTableOffset + 0x16] << 16) |
                                  (_rawData[hashTableOffset + 0x17] << 24));
            Logger.Trace<StfsFile>($"  info_raw: 0x{infoRaw:X8}");

            // Check allocation state (bits 30-31)
            uint allocationState = (infoRaw >> 30) & 0x03;
            Logger.Trace<StfsFile>($"  Allocation state: {allocationState} (2 = in use)");

            // Check if the block is in use (0x02 = In Use)
            if (allocationState != 2)
            {
                Logger.Warning<StfsFile>($"Block {currentBlock} is not marked as in use (allocation state: {allocationState})");
                break;
            }

            // Get the next block number (bits 0-23)
            uint nextBlock = infoRaw & 0xFFFFFF;
            Logger.Trace<StfsFile>($"  Next block: {nextBlock}");

            // Read block data
            int blockOffset = BlockNumberToOffset((int)currentBlock);
            int bytesToRead = Math.Min(entry.FileSize - offset, BlockSize);
            Logger.Trace<StfsFile>($"  Reading {bytesToRead} bytes from block offset 0x{blockOffset:X8}");

            if (blockOffset + bytesToRead > _rawData.Length)
            {
                Logger.Warning<StfsFile>($"Block offset out of bounds for block {currentBlock}");
                break;
            }

            Logger.Trace<StfsFile>($"  Data at block offset: {BitConverter.ToString(_rawData.Skip(blockOffset).Take(Math.Min(32, bytesToRead)).ToArray())}");
            Array.Copy(_rawData, blockOffset, data, offset, bytesToRead);
            offset += bytesToRead;
            currentBlock = nextBlock;
            blocksRemaining--;
        }

        Logger.Debug<StfsFile>($"Extracted {offset} bytes for {entry.FileName}");
        Logger.Trace<StfsFile>($"  First 32 bytes: {BitConverter.ToString(data.Take(32).ToArray())}");
        Logger.Trace<StfsFile>($"  Last 32 bytes: {BitConverter.ToString(data.Skip(Math.Max(0, data.Length - 32)).ToArray())}");
        return data;
    }

    /// <summary>
    /// Extracts a file from the STFS package by its name.
    /// </summary>
    /// <param name="fileName">The name of the file to extract.</param>
    /// <returns>The file data as a byte array, or null if the file is not found.</returns>
    public byte[]? ExtractFile(string fileName)
    {
        Logger.Debug<StfsFile>($"Attempting to extract file: {fileName}");

        StfsFileEntry? entry = FileEntries.FirstOrDefault(e =>
            e.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase) && !e.IsDirectory);

        if (entry == null)
        {
            Logger.Warning<StfsFile>($"File not found: {fileName}");
            return null;
        }

        return ExtractFile(entry);
    }

    /// <summary>
    /// Extracts a file from the STFS package using its file entry.
    /// </summary>
    /// <param name="entry">The file entry to extract.</param>
    /// <returns>The file data as a byte array.</returns>
    public byte[] ExtractFile(StfsFileEntry entry)
    {
        if (entry.IsDirectory)
        {
            Logger.Warning<StfsFile>($"Cannot extract directory: {entry.FileName}");
            throw new InvalidOperationException($"Cannot extract directory: {entry.FileName}");
        }

        Logger.Debug<StfsFile>($"Extracting file: {entry.FileName} ({entry.FileSize} bytes)");

        if (entry.FileSize == 0)
        {
            Logger.Debug<StfsFile>($"File is empty: {entry.FileName}");
            return Array.Empty<byte>();
        }

        // Check if blocks are consecutive
        if (entry.HasConsecutiveBlocks)
        {
            Logger.Debug<StfsFile>($"File has consecutive blocks starting at {entry.StartingBlock}");
            return ExtractConsecutiveFile(entry);
        }
        else
        {
            Logger.Debug<StfsFile>($"File has non-consecutive blocks starting at {entry.StartingBlock}");
            return ExtractNonConsecutiveFile(entry);
        }
    }

    /// <summary>
    /// Extracts a file with consecutive blocks.
    /// </summary>
    /// <param name="entry">The file entry.</param>
    /// <returns>The file data.</returns>
    private byte[] ExtractConsecutiveFile(StfsFileEntry entry)
    {
        Logger.Trace<StfsFile>($"ExtractConsecutiveFile: {entry.FileName}, StartBlock={entry.StartingBlock}, Size={entry.FileSize}");

        int startOffset = BlockNumberToOffset(entry.StartingBlock);
        Logger.Trace<StfsFile>($"  Start offset: 0x{startOffset:X8}");

        byte[] data = new byte[entry.FileSize];

        int bytesToRead = Math.Min(entry.FileSize, BlockSize - (startOffset % BlockSize));
        Logger.Trace<StfsFile>($"  Reading {bytesToRead} bytes from offset 0x{startOffset:X8}");
        Logger.Trace<StfsFile>($"  Data at start offset: {BitConverter.ToString(_rawData.Skip(startOffset).Take(Math.Min(32, bytesToRead)).ToArray())}");

        Array.Copy(_rawData, startOffset, data, 0, bytesToRead);

        int offset = bytesToRead;
        int currentBlock = entry.StartingBlock + 1;

        while (offset < entry.FileSize)
        {
            int blockOffset = BlockNumberToOffset(currentBlock);
            int remaining = entry.FileSize - offset;
            int readSize = Math.Min(remaining, BlockSize);

            if (blockOffset + readSize > _rawData.Length)
            {
                Logger.Warning<StfsFile>($"Reached end of package data while extracting {entry.FileName}");
                break;
            }

            Logger.Trace<StfsFile>($"  Reading block {currentBlock} at offset 0x{blockOffset:X8}, {readSize} bytes");
            Array.Copy(_rawData, blockOffset, data, offset, readSize);
            offset += readSize;
            currentBlock++;
        }

        Logger.Debug<StfsFile>($"Extracted {data.Length} bytes for {entry.FileName}");
        Logger.Trace<StfsFile>($"  First 32 bytes: {BitConverter.ToString(data.Take(32).ToArray())}");
        Logger.Trace<StfsFile>($"  Last 32 bytes: {BitConverter.ToString(data.Skip(Math.Max(0, data.Length - 32)).ToArray())}");
        return data;
    }

    /// <summary>
    /// Lists all files in the STFS package.
    /// </summary>
    /// <returns>A list of file names.</returns>
    public List<string> ListFiles()
    {
        return FileEntries.Where(e => !e.IsDirectory).Select(e => e.FileName).ToList();
    }

    /// <summary>
    /// Lists all directories in the STFS package.
    /// </summary>
    /// <returns>A list of directory names.</returns>
    public List<string> ListDirectories()
    {
        return FileEntries.Where(e => e.IsDirectory).Select(e => e.FileName).ToList();
    }

    /// <summary>
    /// Extracts all files from the STFS package using Xenia's content folder structure.
    /// Structure: OutputDirectory/TitleID/ContentType/[packageName]/[files]
    /// Also creates header files in: OutputDirectory/TitleID/Headers/ContentType/[packageName].header
    /// Uses auto-detected values from metadata if parameters are not provided.
    /// </summary>
    /// <param name="outputDirectory">The root output directory.</param>
    /// <param name="titleId">Optional Title ID (e.g., "4D5309C9"). Uses metadata if not provided.</param>
    /// <param name="contentTypeHex">Optional content type hex (e.g., "000B0000"). Uses metadata if not provided.</param>
    /// <param name="packageName">Optional package name. Uses loaded filename if not provided.</param>
    public void ExtractToXeniaStructure(string outputDirectory, string? titleId = null, string? contentTypeHex = null, string? packageName = null)
    {
        if (FileEntries.Count == 0)
        {
            Logger.Warning<StfsFile>($"No files found in the STFS package");
            return;
        }

        // Use auto-detected values if not provided
        titleId ??= Metadata.TitleIdHex;
        contentTypeHex ??= Metadata.ContentType.ToHexString();
        packageName ??= PackageName ?? "unknown";

        Logger.Info<StfsFile>($"Extracting to Xenia structure: TitleID={titleId}, ContentType={contentTypeHex}, Package={packageName}");

        // Create the main content type folder: OutputDirectory/TitleID/ContentType/
        string contentTypeFolderPath = Path.Combine(outputDirectory, titleId, contentTypeHex);
        Logger.Debug<StfsFile>($"Creating content type folder: {contentTypeFolderPath}");
        Directory.CreateDirectory(contentTypeFolderPath);

        // Create the headers folder: OutputDirectory/TitleID/Headers/ContentType/
        string headersFolderPath = Path.Combine(outputDirectory, titleId, "Headers", contentTypeHex);
        Logger.Debug<StfsFile>($"Creating headers folder: {headersFolderPath}");
        Directory.CreateDirectory(headersFolderPath);

        // Create the package folder: OutputDirectory/TitleID/ContentType/[packageName]/
        string packageFolderPath = Path.Combine(contentTypeFolderPath, packageName);
        Logger.Debug<StfsFile>($"Creating package folder: {packageFolderPath}");
        Directory.CreateDirectory(packageFolderPath);

        // Create the header file: OutputDirectory/TitleID/Headers/ContentType/[packageName].header
        string headerFilePath = Path.Combine(headersFolderPath, $"{packageName}.header");
        Logger.Debug<StfsFile>($"Creating header file: {headerFilePath}");
        CreateHeaderFile(headerFilePath, titleId, contentTypeHex, packageName);

        // Build a proper directory tree to handle nested directories
        // Each entry's PathIndicator points to the index of its parent directory in FileEntries
        // -1 (or 0xFFFF) means root level

        // First pass: create the directory entries map with their full paths
        Dictionary<int, string> directoryFullPathMap = new Dictionary<int, string>();
        directoryFullPathMap[-1] = ""; // Root directory has an empty path

        // Log all entries for debugging
        Logger.Debug<StfsFile>($"Total entries: {FileEntries.Count}");
        for (int i = 0; i < FileEntries.Count; i++)
        {
            Logger.Trace<StfsFile>($"Entry[{i}]: {(FileEntries[i].IsDirectory ? "DIR" : "FILE")} '{FileEntries[i].FileName}' (PathIndicator: {FileEntries[i].PathIndicator})");
        }

        // We need to process directories in order since child directories depend on parents
        // Parent directories should always appear before their children in STFS
        for (int i = 0; i < FileEntries.Count; i++)
        {
            if (FileEntries[i].IsDirectory)
            {
                string parentPath = "";
                int parentIndex = FileEntries[i].PathIndicator;

                // Handle 0xFFFF as -1 (root)
                if (parentIndex == 0xFFFF)
                {
                    parentIndex = -1;
                }

                if (directoryFullPathMap.TryGetValue(parentIndex, out string? parent))
                {
                    parentPath = parent;
                }
                else
                {
                    Logger.Warning<StfsFile>($"Directory '{FileEntries[i].FileName}' at index {i} has invalid parent index {parentIndex}, placing at root");
                }

                string fullPath = string.IsNullOrEmpty(parentPath)
                    ? FileEntries[i].FileName
                    : Path.Combine(parentPath, FileEntries[i].FileName);

                directoryFullPathMap[i] = fullPath;
                Logger.Trace<StfsFile>($"Directory[{i}]: '{FileEntries[i].FileName}' -> Full path: '{fullPath}' (parent index: {parentIndex})");
            }
        }

        // Extract files to the package folder
        foreach (StfsFileEntry entry in FileEntries.Where(e => !e.IsDirectory))
        {
            try
            {
                // Determine the file's full directory path based on the path indicator
                string relativePath = entry.FileName;
                int parentIndex = entry.PathIndicator;

                // Handle 0xFFFF as -1 (root)
                if (parentIndex == 0xFFFF)
                {
                    parentIndex = -1;
                }

                if (parentIndex >= 0 && directoryFullPathMap.TryGetValue(parentIndex, out string? dirPath))
                {
                    relativePath = string.IsNullOrEmpty(dirPath)
                        ? entry.FileName
                        : Path.Combine(dirPath, entry.FileName);
                    Logger.Trace<StfsFile>($"File '{entry.FileName}' (index {FileEntries.IndexOf(entry)}) belongs to directory at index {parentIndex} -> Full path: '{relativePath}'");
                }
                else if (parentIndex == -1)
                {
                    Logger.Trace<StfsFile>($"File '{entry.FileName}' is at root level (path indicator: -1)");
                }
                else
                {
                    Logger.Warning<StfsFile>($"File '{entry.FileName}' has invalid path indicator {parentIndex}, placing at root");
                }

                string outputPath = Path.Combine(packageFolderPath, relativePath);
                string? directoryPath = Path.GetDirectoryName(outputPath);

                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Logger.Debug<StfsFile>($"Creating directory: {directoryPath}");
                    Directory.CreateDirectory(directoryPath);
                }

                byte[] data = ExtractFile(entry);
                File.WriteAllBytes(outputPath, data);
                Logger.Info<StfsFile>($"Extracted: {relativePath} ({data.Length} bytes)");
            }
            catch (Exception ex)
            {
                Logger.Error<StfsFile>($"Failed to extract {entry.FileName}: {ex.Message}");
            }
        }

        Logger.Info<StfsFile>($"Xenia structure extraction completed to {contentTypeFolderPath}");
    }

    /// <summary>
    /// Creates a Xenia-style header file for the extracted content.
    /// <para>
    /// Structure:
    /// </para>
    ///   - device_id (4 bytes) - 0x00
    /// <para>
    ///   - content_type (4 bytes) - 0x04
    /// </para>
    ///   - display_name_raw (256 bytes / 128 UTF-16 chars) - 0x08
    /// <para>
    ///   - file_name_raw (42 bytes) - 0x108
    /// </para>
    ///   - padding (2 bytes) - 0x132
    /// <para>
    ///   - xuid (8 bytes) - 0x134
    /// </para>
    /// <para>
    ///   - title_id (4 bytes) - 0x13C
    /// </para>
    ///   - license_mask (4 bytes) - 0x148
    /// <para>
    /// Total: 0x14C (332 bytes)
    /// </para>
    /// </summary>
    private void CreateHeaderFile(string headerFilePath, string titleId, string contentTypeHex, string packageName)
    {
        const int headerSize = 0x14C;
        byte[] headerData = new byte[headerSize];
        int offset = 0;

        // 0x00-0x03: device_id (big endian) - typically 1 for HDD
        headerData[offset++] = 0x00;
        headerData[offset++] = 0x00;
        headerData[offset++] = 0x00;
        headerData[offset++] = 0x01;

        // 0x04-0x07: content_type (big endian)
        if (uint.TryParse(contentTypeHex, System.Globalization.NumberStyles.HexNumber, null, out uint contentType))
        {
            headerData[offset++] = (byte)((contentType >> 24) & 0xFF);
            headerData[offset++] = (byte)((contentType >> 16) & 0xFF);
            headerData[offset++] = (byte)((contentType >> 8) & 0xFF);
            headerData[offset++] = (byte)(contentType & 0xFF);
        }
        else
        {
            offset += 4;
        }

        // 0x08-0x107: display_name_raw (UTF-16 BE, 128 characters = 256 bytes)
        string displayName = !string.IsNullOrEmpty(Metadata.DisplayName) ? Metadata.DisplayName : packageName;
        byte[] nameBytes = Encoding.BigEndianUnicode.GetBytes(displayName);
        int nameLength = Math.Min(nameBytes.Length, 256);
        Array.Copy(nameBytes, 0, headerData, 0x08, nameLength);

        // 0x108-0x131: file_name_raw (42 bytes, ASCII)
        byte[] filenameBytes = Encoding.ASCII.GetBytes(packageName);
        int filenameLength = Math.Min(filenameBytes.Length, 42);
        Array.Copy(filenameBytes, 0, headerData, 0x108, filenameLength);

        // 0x132-0x133: padding (2 bytes, already zeros)

        // 0x134-0x13B: XUID, in this case 0 because we are installing content (8 bytes, big endian)

        // 0x140-0x143: title_id (4 bytes, big endian)
        if (uint.TryParse(titleId, System.Globalization.NumberStyles.HexNumber, null, out uint titleIdValue))
        {
            headerData[0x140] = (byte)((titleIdValue >> 24) & 0xFF);
            headerData[0x141] = (byte)((titleIdValue >> 16) & 0xFF);
            headerData[0x142] = (byte)((titleIdValue >> 8) & 0xFF);
            headerData[0x143] = (byte)(titleIdValue & 0xFF);
        }

        // 0x148-0x14B: license_mask (4 bytes, big endian)
        // Xenia sets this to 0x00000000 for most content
        headerData[0x148] = 0x00;
        headerData[0x149] = 0x00;
        headerData[0x14A] = 0x00;
        headerData[0x14B] = 0x00;

        File.WriteAllBytes(headerFilePath, headerData);
        Logger.Debug<StfsFile>($"Header file created at {headerFilePath} ({headerSize} bytes)");
        Logger.Trace<StfsFile>($"Header bytes 0x00-0x0F: {BitConverter.ToString(headerData.Take(16).ToArray())}");
        Logger.Trace<StfsFile>($"Header bytes 0x100-0x11F: {BitConverter.ToString(headerData.Skip(0x100).Take(32).ToArray())}");
        Logger.Trace<StfsFile>($"Header bytes 0x13C-0x14B: {BitConverter.ToString(headerData.Skip(0x13C).Take(16).ToArray())}");
    }

    /// <summary>
    /// Gets file entries by path indicator (for directory support).
    /// </summary>
    /// <param name="pathIndicator">The path indicator (-1 for root).</param>
    /// <returns>File entries at the specified path.</returns>
    public List<StfsFileEntry> GetEntriesByPath(short pathIndicator)
    {
        return FileEntries.Where(e => e.PathIndicator == pathIndicator).ToList();
    }

    /// <summary>
    /// Disposes of resources used by the STFS file.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        Array.Clear(_rawData, 0, _rawData.Length);
    }

    /// <summary>
    /// Returns a string representation of the STFS package.
    /// TODO: Modify when adding Install Content option
    /// </summary>
    /// <returns>A string describing the package.</returns>
    public override string ToString()
    {
        return $"STFS Package: {Metadata.DisplayName} ({SignatureType}, {FileEntries.Count} files)";
    }
}