using System.Buffers.Binary;
using System.Text;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Xex;

namespace XeniaManager.Core.Files;

/// <summary>
/// Handles loading and parsing of XEX (Xbox Executable) files.
/// <para>
/// XEX is the executable file format used by the Xbox 360 operating system.
/// </para>
/// <para>
/// File Structure:
/// - 24-byte XEX Header (magic "XEX2", module flags, PE data offset, security info offset, optional header count)
/// - Variable-length optional headers (directory entries with ID and data/offset)
/// - Security Info (RSA signatures, AES keys, hashes, region locks)
/// - Program/Section content (encrypted PE file, typically at offset 0x2000)
/// </para>
/// <para>
/// Cryptography:
/// - Section contents are encrypted with CBC AES using a key derived from the RSA block and a console-specific key
/// - Some debug XEX files exist in uncompressed/unencrypted form
/// - Contents are compressed using Microsoft's proprietary LDIC compression
/// - The RSA signature block at the beginning contains the image hash and AES key seed
/// </para>
/// <para>
/// Module Flags (Bitfield):
/// </para>
/// - Bit 0: Title Module
/// <para>
/// - Bit 1: Exports To Title
/// </para>
/// - Bit 2: System Debugger
/// <para>
/// - Bit 3: DLL Module
/// </para>
/// - Bit 4: Module Patch
/// <para>
/// - Bit 5: Patch Full
/// </para>
/// - Bit 6: Patch Delta
/// <para>
/// - Bit 7: User Mode
/// </para>
/// </summary>
public sealed class XexFile
{
    /// <summary>
    /// Gets the parsed XEX header.
    /// Contains magic bytes, module flags, and offsets to other structures.
    /// </summary>
    public XexHeader Header { get; private set; }

    /// <summary>
    /// Gets the parsed security information.
    /// Contains RSA signatures, AES keys, hashes, and region locks.
    /// </summary>
    public XexSecurityInfo SecurityInfo { get; private set; }

    /// <summary>
    /// Gets the parsed execution information.
    /// Contains TitleID, MediaID, version, and disc information.
    /// May be null if execution info was not found in the optional headers.
    /// </summary>
    public XexExecutionInfo? Execution { get; private set; }

    /// <summary>
    /// Gets the Media ID as a formatted hex string (e.g., "42C67824").
    /// <para>
    /// The Media ID identifies the physical media type and disc information.
    /// It is used to verify that the executable is running from an authorized media source.
    /// Multiple Media IDs can be specified for multi-disc games via the Multidisc Media IDs optional header (0x406FF).
    /// </para>
    /// </summary>
    public string MediaId => Execution.HasValue ? $"{Execution.Value.MediaId:X8}" : string.Empty;

    /// <summary>
    /// Gets the Title ID as a formatted hex string (e.g., "4D530910").
    /// <para>
    /// The Title ID uniquely identifies the game or application.
    /// Format: PPPPNNNN where PPPP is the publisher ID and NNNN is the game ID.
    /// This ID is used for achievement tracking, save game compatibility, and title synchronization.
    /// Alternative Title IDs can be specified via the Alternate Title IDs optional header (0x407FF).
    /// </para>
    /// </summary>
    public string TitleId => Execution.HasValue ? $"{Execution.Value.TitleId:X8}" : string.Empty;

    /// <summary>
    /// Gets whether the XEX file was successfully parsed.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the validation error message if the file is invalid.
    /// Returns null if the file is valid.
    /// </summary>
    public string? ValidationError { get; private set; }

    /// <summary>
    /// Private constructor to enforce factory methods.
    /// Initializes the instance as invalid by default.
    /// </summary>
    private XexFile()
    {
        IsValid = false;
        Header = default;
        SecurityInfo = default;
        Execution = null;
    }

    /// <summary>
    /// Loads a XEX file from the specified path.
    /// </summary>
    /// <param name="filePath">The path to the XEX file to load.</param>
    /// <returns>A new XexFile instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static XexFile Load(string filePath)
    {
        Logger.Debug<XexFile>($"Loading XEX file from {filePath}");

        if (!File.Exists(filePath))
        {
            Logger.Error<XexFile>($"XEX file does not exist: {filePath}");
            throw new FileNotFoundException($"XEX file does not exist at {filePath}", filePath);
        }

        byte[] fileData = File.ReadAllBytes(filePath);
        Logger.Info<XexFile>($"Loaded XEX file: {filePath} ({fileData.Length} bytes)");
        return FromBytes(fileData);
    }

    /// <summary>
    /// Parses a XEX file from raw bytes.
    /// <para>
    /// Validation steps:
    /// 1. Verifies minimum size (24 bytes for XEX header)
    /// 2. Validates magic bytes ("XEX2")
    /// 3. Parses security info at the specified offset
    /// 4. Searches optional headers for Execution Info (header ID 0x40006)
    /// 5. Extracts TitleID and MediaID from execution info
    /// </para>
    /// <para>
    /// The Execution Info is found by searching the optional header directory.
    /// Search ID formula: (header_id << 8) | (size >> 2)
    /// For Execution Info: (0x400 << 8) | (24 >> 2) = 0x40006
    /// </para>
    /// </summary>
    /// <param name="data">The raw byte data of the XEX file.</param>
    /// <returns>A new XexFile instance. IsValid will be false if parsing fails.</returns>
    public static XexFile FromBytes(byte[] data)
    {
        Logger.Trace<XexFile>($"Parsing XEX from bytes ({data.Length} bytes)");

        XexFile xexFile = new XexFile();

        try
        {
            // Validate minimum size for XEX header (24 bytes)
            if (data.Length < 0x18)
            {
                xexFile.ValidationError = "Data too short for XEX header";
                Logger.Error<XexFile>(xexFile.ValidationError);
                return xexFile;
            }

            // Parse and store header
            xexFile.Header = ParseXexHeader(data);
            Logger.Debug<XexFile>($"XEX Magic: {GetString(xexFile.Header.Magic)}, Security Info Offset: 0x{xexFile.Header.SecurityInfo:X8}");

            // Validate magic
            string magic = GetString(xexFile.Header.Magic);
            if (magic != "XEX2")
            {
                xexFile.ValidationError = $"Invalid XEX magic: {magic} (expected XEX2)";
                Logger.Error<XexFile>(xexFile.ValidationError);
                return xexFile;
            }

            // Parse security info
            if (xexFile.Header.SecurityInfo >= data.Length)
            {
                xexFile.ValidationError = "Invalid security info offset";
                Logger.Error<XexFile>(xexFile.ValidationError);
                return xexFile;
            }

            xexFile.SecurityInfo = ParseSecurityInfo(data, (int)xexFile.Header.SecurityInfo);
            Logger.Debug<XexFile>($"Image Size: 0x{xexFile.SecurityInfo.ImageSize:X8}, Game Region: 0x{xexFile.SecurityInfo.ImageInfo.GameRegion:X8}");

            // Find and parse execution info (contains both TitleID and MediaID)
            // Execution info header ID: 0x40006 (search ID: 0x400 << 8 | 24 >> 2 = 0x40006)
            xexFile.Execution = FindExecutionInfo(data, xexFile.Header);
            if (xexFile.Execution.HasValue)
            {
                Logger.Debug<XexFile>($"TitleID: {xexFile.TitleId}, MediaID: {xexFile.MediaId}");
            }
            else
            {
                xexFile.ValidationError = "Unable to find execution info";
                Logger.Warning<XexFile>(xexFile.ValidationError);
                return xexFile;
            }

            xexFile.IsValid = true;
            Logger.Info<XexFile>($"Successfully parsed XEX file - TitleID: {xexFile.TitleId}, MediaID: {xexFile.MediaId}");
        }
        catch (Exception ex)
        {
            xexFile.ValidationError = $"Failed to parse XEX: {ex.Message}";
            Logger.Error<XexFile>(xexFile.ValidationError);
            Logger.LogExceptionDetails<XexFile>(ex);
        }

        return xexFile;
    }

    /// <summary>
    /// Parses the XEX header from raw bytes.
    /// The header contains magic bytes, module flags, header sizes, and directory information.
    /// </summary>
    /// <param name="data">The raw byte data containing the XEX header.</param>
    /// <returns>A populated XexHeader structure.</returns>
    private static XexHeader ParseXexHeader(byte[] data)
    {
        return new XexHeader
        {
            Magic = data.Take(4).ToArray(),
            ModuleFlags = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4)),
            SizeOfHeaders = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(8)),
            SizeOfDiscardableHeaders = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(12)),
            SecurityInfo = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(16)),
            HeaderDirectoryEntryCount = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(20))
        };
    }

    /// <summary>
    /// Parses the security info structure from raw bytes.
    /// The security info contains image size, signature, hashes, and media type restrictions.
    /// </summary>
    /// <param name="data">The raw byte data containing the security info.</param>
    /// <param name="offset">The offset in the data where the security info begins.</param>
    /// <returns>A populated XexSecurityInfo structure.</returns>
    /// <exception cref="ArgumentException">Thrown when the data is too short for security info.</exception>
    private static XexSecurityInfo ParseSecurityInfo(byte[] data, int offset)
    {
        if (data.Length < offset + 0x1A0)
        {
            throw new ArgumentException("Data too short for security info");
        }

        HvImageInfo imageInfo = new HvImageInfo
        {
            Signature = data.Skip(offset + 0x4).Take(0x100).ToArray(),
            InfoSize = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x104)),
            ImageFlags = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x108)),
            LoadAddress = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x10C)),
            ImageHash = data.Skip(offset + 0x110).Take(0x14).ToArray(),
            ImportTableCount = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x124)),
            ImportDigest = data.Skip(offset + 0x128).Take(0x14).ToArray(),
            MediaId = data.Skip(offset + 0x13C).Take(0x10).ToArray(),
            ImageKey = data.Skip(offset + 0x14C).Take(0x10).ToArray(),
            ExportTableAddress = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x15C)),
            HeaderHash = data.Skip(offset + 0x160).Take(0x14).ToArray(),
            GameRegion = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x174))
        };

        return new XexSecurityInfo
        {
            Size = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset)),
            ImageSize = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 4)),
            ImageInfo = imageInfo,
            AllowedMediaTypes = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x178)),
            PageDescriptorCount = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x17C))
        };
    }

    /// <summary>
    /// Finds and parses the execution info from the header directory.
    /// The search ID is calculated as (id << 8) | (size >> 2) per XEX specification.
    /// Execution info contains TitleID, MediaID, version, and disc information.
    /// </summary>
    /// <param name="data">The raw byte data containing the header directory.</param>
    /// <param name="header">The parsed XEX header with directory entry count.</param>
    /// <returns>An XexExecution structure if found, null otherwise.</returns>
    private static XexExecutionInfo? FindExecutionInfo(byte[] data, XexHeader header)
    {
        int headerDirectoryOffset = 0x18; // After the main header
        uint entryCount = header.HeaderDirectoryEntryCount;

        // Search ID for execution info: (0x400 << 8) | (24 >> 2) = 0x40006
        // 0x400 is the execution info ID, 24 is the size of XexExecution structure
        uint executionSearchId = 0x400 << 8 | 24 >> 2;

        for (int i = 0; i < entryCount; i++)
        {
            int entryOffset = headerDirectoryOffset + (i * 8);
            if (entryOffset + 8 > data.Length)
            {
                break;
            }

            uint value = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(entryOffset));
            uint offset = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(entryOffset + 4));

            // Check if this is the execution info entry
            if (value == executionSearchId && offset > 0 && offset < data.Length - 20)
            {
                return new XexExecutionInfo
                {
                    MediaId = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)offset)),
                    Version = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)offset + 4)),
                    BaseVersion = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)offset + 8)),
                    TitleId = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan((int)offset + 12)),
                    Platform = data[offset + 16],
                    ExecutableType = data[offset + 17],
                    DiscNum = data[offset + 18],
                    DiscTotal = data[offset + 19]
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Converts a byte array to an ASCII string.
    /// </summary>
    /// <param name="bytes">The byte array to convert.</param>
    /// <returns>The ASCII string representation with null terminators trimmed.</returns>
    private static string GetString(byte[] bytes)
    {
        return Encoding.ASCII.GetString(bytes).Trim('\0');
    }
}