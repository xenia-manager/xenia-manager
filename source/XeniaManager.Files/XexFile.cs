using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using XeniaManager.Files.Models.Xex;
using XeniaManager.Files.Utilities;
using XeniaManager.Logging;

namespace XeniaManager.Files;

/// <summary>
/// Handles loading and parsing of XEX (Xbox Executable) files, the Xbox 360's native PE container.
/// </summary>
/// <remarks>
/// <para>
/// XEX2 wraps a PowerPC PE image with a 24-byte header, optional header directory, security info (RSA, AES key, flags),
/// and encrypted/compressed PE sections.
/// </para>
/// <para>
/// File structure (big-endian header, little-endian PE):
/// <list type="bullet">
/// <item><c>0x00</c> 24-byte XEX header: magic "XEX2" (<c>0x58455832</c>), module flags, <c>header_size</c> (PE offset, typically 0x2000), reserved, <c>security_offset</c>, <c>header_count</c>.</item>
/// <item><c>0x18</c> Optional header directory: <c>header_count × 8</c> bytes, each <c>{key:u32, value/offset:u32}</c> where <c>key = (id &lt;&lt; 8) | (size &gt;&gt; 2)</c>.</item>
/// <item><c>security_offset</c> Security info: image size, RSA signature, SHA hashes, AES key seed, region, allowed media, page descriptors.</item>
/// <item><c>header_size</c> Encrypted PE bytes (AES-CBC zero IV, retail/devkit key) + optional Basic (zero-fill) or Normal (LZX, window 15-21) compression.</item>
/// </list>
/// </para>
/// <para>
/// This class parses only the header/security/execution info by default (<see cref="FromBytes"/>). SPA/XDBF extraction
/// (<see cref="TryGetSpaFile"/>, <see cref="TryGetIcon"/>) decrypts/decompresses the PE on demand via
/// <see cref="Utilities.LzxDecoder"/> and locates the PE section named <c>"{TitleId:08X}"</c> which holds the XDBF SPA (see <c>SpaFile</c>).
/// </para>
/// </remarks>
public sealed class XexFile
{
    /// <summary>
    /// XEX2 magic "XEX2" as big-endian <c>uint32</c> (<c>0x58455832</c>).
    /// </summary>
    private const uint Xex2Magic = 0x58455832;

    /// <summary>
    /// Raw XEX bytes as loaded (retained for on-demand PE/SPA extraction). Never exposed mutably; see <see cref="RawData"/>.
    /// </summary>
    private byte[] _rawData = Array.Empty<byte>();

    /// <summary>
    /// Retail AES key used to unwrap the per-file <c>ImageKey</c> at <c>security_offset+0x150</c>.
    /// </summary>
    private static readonly byte[] RetailKey =
    [
        0x20, 0xB1, 0x85, 0xA5, 0x9D, 0x28, 0xFD, 0xC3,
        0x40, 0x58, 0x3F, 0xBB, 0x08, 0x96, 0xBF, 0x91
    ];

    /// <summary>
    /// Devkit AES key (all zeroes). Tried second if retail fails.
    /// </summary>
    private static readonly byte[] DevkitKey = new byte[16];

    /// <summary>
    /// Gets the parsed XEX header (magic, module flags, header size, security offset, directory count).
    /// </summary>
    public XexHeader Header { get; private set; }

    /// <summary>
    /// Gets the parsed security information (image size, RSA, SHA, AES seed, region, media types).
    /// </summary>
    public XexSecurityInfo SecurityInfo { get; private set; }

    /// <summary>
    /// Gets the parsed execution information (TitleId, MediaId, version, disc). Null if <c>0x40006</c> header missing.
    /// </summary>
    public XexExecutionInfo? Execution { get; private set; }

    /// <summary>
    /// Gets the raw XEX bytes as loaded (read-only view). Needed for <see cref="TryGetSpaFile"/> PE extraction.
    /// </summary>
    public IReadOnlyList<byte> RawData
    {
        get
        {
            return _rawData;
        }
    }

    /// <summary>
    /// Gets the Media ID as 8-hex-digit string (e.g., "42C67824") from <see cref="Execution"/> or empty if unavailable.
    /// </summary>
    public string MediaId
    {
        get
        {
            return Execution.HasValue ? $"{Execution.Value.MediaId:X8}" : string.Empty;
        }
    }

    /// <summary>
    /// Gets the Title ID as 8-hex-digit string <c>PPPPNNNN</c> (publisher + game) from <see cref="Execution"/> or empty.
    /// </summary>
    public string TitleId
    {
        get
        {
            return Execution.HasValue ? $"{Execution.Value.TitleId:X8}" : string.Empty;
        }
    }

    /// <summary>
    /// Gets whether the XEX was successfully parsed (magic valid, security info and execution info found).
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the validation error for invalid files; null when <see cref="IsValid"/> is true.
    /// </summary>
    public string? ValidationError { get; private set; }

    /// <summary>
    /// Private constructor — use <see cref="Load"/> or <see cref="FromBytes"/> factories. Initializes as invalid.
    /// </summary>
    private XexFile()
    {
        IsValid = false;
        Header = default;
        SecurityInfo = default;
        Execution = null;
    }

    /// <summary>
    /// Loads a XEX file from disk and parses it via <see cref="FromBytes"/>.
    /// </summary>
    /// <param name="filePath">Absolute or relative path to a <c>.xex</c> file.</param>
    /// <returns>A new <see cref="XexFile"/> (check <see cref="IsValid"/> before use).</returns>
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
    /// Parses a XEX file from raw bytes without touching disk.
    /// </summary>
    /// <param name="data">Complete XEX file bytes (any size ≥ 24).</param>
    /// <returns>A new <see cref="XexFile"/>; <see cref="IsValid"/> false and <see cref="ValidationError"/> set on failure.</returns>
    /// <remarks>
    /// Validation:
    /// <list type="number">
    /// <item>≥ 24 bytes for header.</item>
    /// <item>Magic "XEX2" (<c>0x58455832</c>).</item>
    /// <item>Security offset in-bounds and ≥ 0x1A0 bytes for <c>xex2_security_info</c>.</item>
    /// <item>Optional header <c>0x40006</c> (execution info, <c>(0x400 &lt;&lt; 8)|(24&gt;&gt;2)</c>) present.</item>
    /// </list>
    /// Stores <paramref name="data"/> in <see cref="_rawData"/> for later <see cref="TryGetSpaFile"/> decryption.
    /// </remarks>
    public static XexFile FromBytes(byte[] data)
    {
        Logger.Trace<XexFile>($"Parsing XEX from bytes ({data.Length} bytes)");

        XexFile xexFile = new XexFile
        {
            _rawData = data
        };

        try
        {
            if (data.Length < 0x18)
            {
                xexFile.ValidationError = "Data too short for XEX header";
                Logger.Error<XexFile>(xexFile.ValidationError);
                return xexFile;
            }

            xexFile.Header = ParseXexHeader(data);
            Logger.Debug<XexFile>($"XEX Magic: {GetString(xexFile.Header.Magic)}, Security Info Offset: 0x{xexFile.Header.SecurityInfo:X8}");

            string magic = GetString(xexFile.Header.Magic);
            if (magic != "XEX2")
            {
                xexFile.ValidationError = $"Invalid XEX magic: {magic} (expected XEX2)";
                Logger.Error<XexFile>(xexFile.ValidationError);
                return xexFile;
            }

            if (xexFile.Header.SecurityInfo >= data.Length)
            {
                xexFile.ValidationError = "Invalid security info offset";
                Logger.Error<XexFile>(xexFile.ValidationError);
                return xexFile;
            }

            xexFile.SecurityInfo = ParseSecurityInfo(data, (int)xexFile.Header.SecurityInfo);
            Logger.Debug<XexFile>($"Image Size: 0x{xexFile.SecurityInfo.ImageSize:X8}, Game Region: 0x{xexFile.SecurityInfo.ImageInfo.GameRegion:X8}");

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
    /// Tries to extract the SPA (XDBF) file embedded in the XEX's PE section <c>"{TitleId:08X}"</c>.
    /// </summary>
    /// <param name="spaFile">The parsed <see cref="SpaFile"/> (caller must <c>Dispose()</c>) if found; otherwise null.</param>
    /// <returns>True if SPA was found and parsed, false otherwise (logs trace, never throws).</returns>
    /// <remarks>
    /// Decrypts (retail then devkit, AES-CBC zero IV) and decompresses (Basic zero-fill or LZX via <see cref="LzxDecoder"/>)
    /// the PE at <c>Header.SizeOfHeaders</c>, then searches the PE section table for <c>TitleId</c> hex name.
    /// Falls back to scanning the PE for XDBF magic <c>0x58444246</c> if the named section is missing.
    /// </remarks>
    public bool TryGetSpaFile(out SpaFile? spaFile)
    {
        spaFile = null;
        if (!IsValid || _rawData.Length < 0x18)
        {
            return false;
        }

        byte[]? spaBytes = TryGetSpaBytes(_rawData);
        if (spaBytes == null)
        {
            return false;
        }

        SpaFile spa = SpaFile.FromBytes(spaBytes);
        if (!spa.IsValid)
        {
            Logger.Trace<XexFile>($"XEX SPA bytes found but SpaFile invalid: {spa.ValidationError}");
            spa.Dispose();
            return false;
        }

        spaFile = spa;
        return true;
    }

    /// <summary>
    /// Tries to extract the dashboard title icon PNG from the SPA section (XDBF image <c>0x8000</c>).
    /// </summary>
    /// <returns>PNG bytes if found (valid <c>89 50 4E 47</c> header), null otherwise. Never throws.</returns>
    /// <remarks>
    /// Convenience wrapper around <see cref="TryGetSpaFile"/> → <see cref="SpaFile.GetTitleIcon"/> with fallback to
    /// <see cref="SpaFile.GetAnyValidIcon"/> and raw PNG scan inside the SPA bytes. Logs at Trace on miss.
    /// </remarks>
    public byte[]? TryGetIcon()
    {
        try
        {
            if (!IsValid || _rawData.Length < 0x18)
            {
                return null;
            }

            byte[]? spaBytes = TryGetSpaBytes(_rawData);
            if (spaBytes == null)
            {
                return null;
            }

            using SpaFile spa = SpaFile.FromBytes(spaBytes);
            if (!spa.IsValid)
            {
                return ScanForPng(spaBytes);
            }

            byte[]? icon = spa.GetTitleIcon();
            if (icon != null)
            {
                Logger.Debug<XexFile>($"XEX title_icon 0x8000 extracted ({icon.Length} bytes)");
                return icon;
            }

            icon = spa.GetAnyValidIcon();
            if (icon != null)
            {
                return icon;
            }

            return ScanForPng(spaBytes);
        }
        catch (Exception ex)
        {
            Logger.Trace<XexFile>($"TryGetIcon failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Extracts raw SPA bytes by decrypting/decompressing the PE image and locating the title section.
    /// </summary>
    /// <param name="xexData">Complete XEX bytes (from <see cref="_rawData"/>).</param>
    /// <returns>SPA/XDBF bytes if the PE section was found, null otherwise.</returns>
    /// <remarks>
    /// Steps: validate <see cref="Xex2Magic"/>, read <c>header_size</c>/<c>security_offset</c>/<c>header_count</c>,
    /// find execution info <c>0x40006</c> for <c>TitleId</c> and file-format info <c>0x3FF</c>, unwrap
    /// <c>ImageKey</c> at <c>security_offset+0x150</c>, call <see cref="TryGetPeImage"/> to obtain the PE,
    /// then <see cref="TryFindPeSection"/> for <c>"{TitleId:X8}"</c> or <see cref="ScanForXdbf"/> fallback.
    /// </remarks>
    private static byte[]? TryGetSpaBytes(byte[] xexData)
    {
        try
        {
            if (xexData.Length < 0x18)
            {
                return null;
            }

            uint magic = BinaryPrimitives.ReadUInt32BigEndian(xexData.AsSpan(0));
            if (magic != Xex2Magic)
            {
                return null;
            }

            uint headerSize = BinaryPrimitives.ReadUInt32BigEndian(xexData.AsSpan(8));
            uint securityOffset = BinaryPrimitives.ReadUInt32BigEndian(xexData.AsSpan(16));
            uint headerCount = BinaryPrimitives.ReadUInt32BigEndian(xexData.AsSpan(20));

            uint titleId = 0;
            int fileFormatOffset = -1;
            int dirOffset = 0x18;
            for (int i = 0; i < headerCount; i++)
            {
                int entryOff = dirOffset + i * 8;
                if (entryOff + 8 > xexData.Length)
                {
                    break;
                }

                uint key = BinaryPrimitives.ReadUInt32BigEndian(xexData.AsSpan(entryOff));
                uint value = BinaryPrimitives.ReadUInt32BigEndian(xexData.AsSpan(entryOff + 4));
                if (key == 0x00040006)
                {
                    if (value + 0x14 <= xexData.Length)
                    {
                        titleId = BinaryPrimitives.ReadUInt32BigEndian(xexData.AsSpan((int)value + 12));
                    }
                }
                else if (key == 0x000003FF)
                {
                    fileFormatOffset = (int)value;
                }
            }

            if (titleId == 0)
            {
                Logger.Trace<XexFile>("XEX SPA: titleId not found");
                return null;
            }

            string sectionName = $"{titleId:X8}";
            Logger.Trace<XexFile>($"XEX SPA: searching section {sectionName}");

            if (securityOffset + 0x180 > xexData.Length)
            {
                return null;
            }

            byte[] encryptedAesKey = new byte[16];
            Buffer.BlockCopy(xexData, (int)securityOffset + 0x150, encryptedAesKey, 0, 16);

            byte[]? peImage = TryGetPeImage(xexData, headerSize, securityOffset, fileFormatOffset, encryptedAesKey);
            if (peImage == null || peImage.Length < 0x40)
            {
                Logger.Trace<XexFile>("XEX SPA: failed to get PE image");
                return null;
            }

            byte[]? spaBytes = TryFindPeSection(peImage, sectionName);
            if (spaBytes == null)
            {
                spaBytes = ScanForXdbf(peImage);
                if (spaBytes == null)
                {
                    Logger.Trace<XexFile>($"XEX SPA: section {sectionName} not found and no XDBF scan hit");
                    return null;
                }
            }

            return spaBytes;
        }
        catch (Exception ex)
        {
            Logger.Trace<XexFile>($"TryGetSpaBytes failed: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Decrypts and decompresses the PE image bytes at <c>headerSize</c>.
    /// </summary>
    /// <param name="xexData">Complete XEX bytes.</param>
    /// <param name="headerSize">PE offset (<c>Header.SizeOfHeaders</c>).</param>
    /// <param name="securityOffset">Security info offset (<c>Header.SecurityInfo</c>).</param>
    /// <param name="fileFormatOffset">Offset of <c>xex2_opt_file_format_info</c> (<c>0x3FF</c>) or -1.</param>
    /// <param name="encryptedAesKey">16-byte <c>ImageKey</c> at <c>securityOffset+0x150</c> to unwrap.</param>
    /// <returns>Decompressed PE bytes (starts <c>4D 5A</c> "MZ") or null on failure.</returns>
    /// <remarks>
    /// Reads <c>encryptionType</c>/<c>compressionType</c> from file-format info
    /// (<c>0=none,1=basic,2=normal/LZX</c>), tries retail then devkit unwrap
    /// (<see cref="AesDecryptEcb"/> + <see cref="AesDecryptCbc"/> zero IV), then
    /// <see cref="TryDecompress"/>; validates result starts with MZ before returning.
    /// </remarks>
    private static byte[]? TryGetPeImage(byte[] xexData, uint headerSize, uint securityOffset, int fileFormatOffset, byte[] encryptedAesKey)
    {
        int encryptionType = 0;
        int compressionType = 0;
        int infoSize = 0;
        byte[]? basicBlocksData = null;
        int windowBits = 17;
        byte[]? firstBlockHash = null;
        int firstBlockSize = 0;

        if (fileFormatOffset >= 0 && fileFormatOffset + 8 <= xexData.Length)
        {
            infoSize = (int)BinaryPrimitives.ReadUInt32BigEndian(xexData.AsSpan(fileFormatOffset));
            if (fileFormatOffset + infoSize <= xexData.Length && infoSize >= 8)
            {
                encryptionType = BinaryPrimitives.ReadUInt16BigEndian(xexData.AsSpan(fileFormatOffset + 4));
                compressionType = BinaryPrimitives.ReadUInt16BigEndian(xexData.AsSpan(fileFormatOffset + 6));
                if (compressionType == 2)
                {
                    if (fileFormatOffset + 12 <= xexData.Length)
                    {
                        uint windowSize = BinaryPrimitives.ReadUInt32BigEndian(xexData.AsSpan(fileFormatOffset + 8));
                        windowBits = (int)Math.Log(windowSize, 2);
                        if (windowBits < 15 || windowBits > 21)
                        {
                            windowBits = 17;
                        }
                    }

                    if (fileFormatOffset + 16 <= xexData.Length)
                    {
                        firstBlockSize = (int)BinaryPrimitives.ReadUInt32BigEndian(xexData.AsSpan(fileFormatOffset + 12));
                        firstBlockHash = new byte[20];
                        Buffer.BlockCopy(xexData, fileFormatOffset + 16, firstBlockHash, 0, 20);
                    }
                }
                else if (compressionType == 1)
                {
                    basicBlocksData = new byte[infoSize - 8];
                    Buffer.BlockCopy(xexData, fileFormatOffset + 8, basicBlocksData, 0, infoSize - 8);
                }

                Logger.Trace<XexFile>(
                    $"XEX format: enc={encryptionType} comp={compressionType} infoSize={infoSize} windowBits={windowBits} firstBlock={firstBlockSize}");
            }
        }

        int dataOffset = (int)headerSize;
        if (dataOffset >= xexData.Length)
        {
            return null;
        }

        int dataLen = xexData.Length - dataOffset;
        byte[] encryptedData = new byte[dataLen];
        Buffer.BlockCopy(xexData, dataOffset, encryptedData, 0, dataLen);

        byte[]? decrypted = null;
        foreach (byte[] key in new[]
                 {
                     RetailKey, DevkitKey
                 })
        {
            try
            {
                byte[] sessionKey = AesDecryptEcb(key, encryptedAesKey);
                byte[] attempt = AesDecryptCbc(sessionKey, encryptedData);
                byte[]? decompressed = TryDecompress(attempt, compressionType, basicBlocksData, windowBits, xexData, securityOffset, firstBlockSize,
                    firstBlockHash);
                if (decompressed != null && decompressed.Length > 0x40 && decompressed[0] == 0x4D && decompressed[1] == 0x5A)
                {
                    Logger.Debug<XexFile>($"XEX decrypt succeeded with {(key == RetailKey ? "retail" : "devkit")} key");
                    return decompressed;
                }

                if (encryptionType == 0)
                {
                    decompressed = TryDecompress(encryptedData, compressionType, basicBlocksData, windowBits, xexData, securityOffset, firstBlockSize,
                        firstBlockHash);
                    if (decompressed != null && decompressed.Length > 0x40 && decompressed[0] == 0x4D && decompressed[1] == 0x5A)
                    {
                        return decompressed;
                    }
                }

                decrypted = attempt;
            }
            catch (Exception ex)
            {
                Logger.Trace<XexFile>($"AES decrypt/decompress attempt failed: {ex.Message}");
            }
        }

        if (decrypted != null)
        {
            byte[]? decompressed = TryDecompress(decrypted, compressionType, basicBlocksData, windowBits, xexData, securityOffset, firstBlockSize,
                firstBlockHash);
            if (decompressed != null)
            {
                return decompressed;
            }

            // For uncompressed PE, decrypted bytes should be the PE itself (MZ header).
            // Only return directly when valid; otherwise decompression failed.
            if (compressionType == 0 && decrypted.Length > 0x40 && decrypted[0] == 0x4D && decrypted[1] == 0x5A)
            {
                return decrypted;
            }

            Logger.Trace<XexFile>("Decompression failed and decrypted data is not a valid PE, returning null");
            return null;
        }

        if (encryptionType == 0)
        {
            return TryDecompress(encryptedData, compressionType, basicBlocksData, windowBits, xexData, securityOffset, firstBlockSize, firstBlockHash);
        }

        return null;
    }

    /// <summary>
    /// Decompresses PE bytes according to XEX compression type.
    /// </summary>
    /// <param name="data">Decrypted (or raw) PE bytes at <c>headerSize</c>.</param>
    /// <param name="compressionType">0=none, 1=Basic (zero-fill blocks), 2=Normal/LZX.</param>
    /// <param name="basicBlocks">Basic block table (<c>infoSize-8</c> bytes, 8 per entry) or null.</param>
    /// <param name="windowBits">LZX window bits (15-21) from file-format info.</param>
    /// <param name="xexData">Complete XEX (for <c>image_size</c> at <c>securityOffset+4</c>).</param>
    /// <param name="securityOffset">Security info offset.</param>
    /// <param name="firstBlockSize">First LZX block size (normal only).</param>
    /// <param name="firstBlockHash">First LZX block SHA (unused, for validation).</param>
    /// <returns>Decompressed PE bytes or null.</returns>
    /// <remarks>
    /// Basic: iterates <c>{dataSize, zeroSize}</c> pairs, copying and zero-filling to <c>image_size</c>.
    /// Normal: tries direct <see cref="LzxDecoder"/> on decrypted data, then de-blocked stream via <see cref="DeblockXexLzx"/>.
    /// Output size comes from <c>security_info.image_size</c>; capped at 60 MiB.
    /// </remarks>
    private static byte[]? TryDecompress(byte[] data, int compressionType, byte[]? basicBlocks, int windowBits, byte[] xexData, uint securityOffset,
        int firstBlockSize = 0, byte[]? firstBlockHash = null)
    {
        try
        {
            if (compressionType == 0)
            {
                return data;
            }

            if (compressionType == 1)
            {
                if (basicBlocks == null || basicBlocks.Length == 0)
                {
                    return data;
                }

                int blockCount = basicBlocks.Length / 8;
                long uncompressedSize = 0;
                for (int i = 0; i < blockCount; i++)
                {
                    uint dataSize = BinaryPrimitives.ReadUInt32BigEndian(basicBlocks.AsSpan(i * 8));
                    uint zeroSize = BinaryPrimitives.ReadUInt32BigEndian(basicBlocks.AsSpan(i * 8 + 4));
                    uncompressedSize += dataSize + zeroSize;
                }

                uint imageSize = 0;
                if (securityOffset + 8 <= xexData.Length)
                {
                    imageSize = BinaryPrimitives.ReadUInt32BigEndian(xexData.AsSpan((int)securityOffset + 4));
                    if (imageSize > 0 && imageSize < uncompressedSize + 0x1000)
                    {
                        uncompressedSize = imageSize;
                    }
                }

                byte[] output = new byte[uncompressedSize];
                int srcOff = 0;
                int dstOff = 0;
                for (int i = 0; i < blockCount; i++)
                {
                    uint dataSize = BinaryPrimitives.ReadUInt32BigEndian(basicBlocks.AsSpan(i * 8));
                    uint zeroSize = BinaryPrimitives.ReadUInt32BigEndian(basicBlocks.AsSpan(i * 8 + 4));
                    if (dataSize > 0)
                    {
                        if (srcOff + dataSize > data.Length || dstOff + dataSize > output.Length)
                        {
                            break;
                        }

                        Buffer.BlockCopy(data, srcOff, output, dstOff, (int)dataSize);
                        srcOff += (int)dataSize;
                        dstOff += (int)dataSize;
                    }

                    if (zeroSize > 0)
                    {
                        if (dstOff + zeroSize > output.Length)
                        {
                            break;
                        }

                        dstOff += (int)zeroSize;
                    }
                }

                return output;
            }

            if (compressionType == 2)
            {
                int outputSize = 0;
                if (securityOffset + 8 <= xexData.Length)
                {
                    outputSize = (int)BinaryPrimitives.ReadUInt32BigEndian(xexData.AsSpan((int)securityOffset + 4));
                }

                if (outputSize <= 0 || outputSize > 60 * 1024 * 1024)
                {
                    outputSize = data.Length * 4;
                }

                try
                {
                    LzxDecoder decoder = new LzxDecoder(windowBits);
                    byte[] decompressed = decoder.Decompress(data, outputSize);
                    if (decompressed.Length > 0x40 && decompressed[0] == 0x4D && decompressed[1] == 0x5A)
                    {
                        return decompressed;
                    }

                    Logger.Trace<XexFile>($"LZX direct decompress produced invalid PE (size {decompressed.Length}), trying deblocked stream");
                }
                catch (Exception ex)
                {
                    Logger.Trace<XexFile>($"LZX direct decompress failed (windowBits={windowBits}): {ex.Message}");
                }

                try
                {
                    byte[] lzxStream = DeblockXexLzx(data, firstBlockSize);
                    if (lzxStream.Length == 0)
                    {
                        return null;
                    }

                    LzxDecoder decoder2 = new LzxDecoder(windowBits);
                    byte[] decompressed2 = decoder2.Decompress(lzxStream, outputSize);
                    if (decompressed2.Length > 0x40 && decompressed2[0] == 0x4D && decompressed2[1] == 0x5A)
                    {
                        return decompressed2;
                    }

                    Logger.Trace<XexFile>($"LZX deblocked decompress produced invalid PE (size {decompressed2.Length})");
                    return null;
                }
                catch (Exception ex)
                {
                    Logger.Trace<XexFile>($"LZX decompress failed windowBits={windowBits}: {ex.Message}");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Trace<XexFile>($"Decompress failed: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// De-blocks an XEX LZX stream by stripping per-block 24-byte headers and 2-byte chunk framing.
    /// </summary>
    /// <param name="data">Decrypted PE bytes as stored (with block headers).</param>
    /// <param name="firstBlockSize">Size of first block from file-format info; 0 means already deblocked.</param>
    /// <returns>Raw LZX bitstream for <see cref="LzxDecoder"/> or <paramref name="data"/> if deblocking not needed.</returns>
    /// <remarks>
    /// XEX LZX is stored as linked blocks: each block starts with 4-byte next-size + 20-byte SHA, then
    /// <c>{u16 chunkLen, chunkBytes}*</c> terminated by 0.
    /// </remarks>
    private static byte[] DeblockXexLzx(byte[] data, int firstBlockSize)
    {
        try
        {
            if (firstBlockSize <= 0 || firstBlockSize > data.Length)
            {
                return data;
            }

            using MemoryStream output = new MemoryStream();
            int p = 0;
            int curSize = firstBlockSize;
            while (curSize != 0)
            {
                int pnext = p + curSize;
                if (pnext > data.Length)
                {
                    break;
                }

                int nextSize = 0;
                if (p + 4 <= data.Length)
                {
                    nextSize = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(p));
                }

                p += 24;
                if (p > pnext)
                {
                    break;
                }

                while (p + 2 <= pnext)
                {
                    int chunkSize = (data[p] << 8) | data[p + 1];
                    p += 2;
                    if (chunkSize == 0)
                    {
                        break;
                    }

                    if (p + chunkSize > pnext)
                    {
                        chunkSize = pnext - p;
                    }

                    output.Write(data, p, chunkSize);
                    p += chunkSize;
                }

                p = pnext;
                curSize = nextSize;
                if (curSize == 0)
                {
                    break;
                }

                if (output.Length > 60 * 1024 * 1024)
                {
                    break;
                }
            }

            if (output.Length == 0)
            {
                return data;
            }

            return output.ToArray();
        }
        catch
        {
            return data;
        }
    }

    /// <summary>
    /// Unwraps the per-file AES session key (ECB, no padding) with the retail or devkit key.
    /// </summary>
    /// <param name="key">16-byte <see cref="RetailKey"/> or <see cref="DevkitKey"/>.</param>
    /// <param name="data">16-byte <c>ImageKey</c> from <c>securityOffset+0x150</c>.</param>
    /// <returns>16-byte session key for <see cref="AesDecryptCbc"/>.</returns>
    private static byte[] AesDecryptEcb(byte[] key, byte[] data)
    {
        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = key;
        using ICryptoTransform dec = aes.CreateDecryptor();
        byte[] output = new byte[data.Length];
        dec.TransformBlock(data, 0, data.Length, output, 0);
        return output;
    }

    /// <summary>
    /// Decrypts PE bytes with the session key (CBC, zero IV, no padding) as stored on disk.
    /// </summary>
    /// <param name="sessionKey">16-byte key from <see cref="AesDecryptEcb"/>.</param>
    /// <param name="data">PE bytes at <c>headerSize</c> (multiple of 16).</param>
    /// <returns>Decrypted bytes (still possibly compressed).</returns>
    private static byte[] AesDecryptCbc(byte[] sessionKey, byte[] data)
    {
        using Aes aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        aes.Key = sessionKey;
        aes.IV = new byte[16];
        using ICryptoTransform dec = aes.CreateDecryptor();
        return dec.TransformFinalBlock(data, 0, data.Length);
    }

    /// <summary>
    /// Finds a PE section by name (case-insensitive, 8-char null-padded) in a decompressed PE image.
    /// </summary>
    /// <param name="peImage">Decompressed PE bytes (must start <c>4D 5A</c> "MZ").</param>
    /// <param name="sectionName">8-char name, e.g., <c>"4D530910"</c> TitleId hex.</param>
    /// <returns>Raw section bytes (trimmed to <c>VirtualSize</c> if smaller) or null.</returns>
    /// <remarks>
    /// Parses DOS <c>e_lfanew</c> at <c>0x3C</c>, validates <c>PE\0\0</c> (<c>0x4550</c>), then iterates
    /// <c>IMAGE_SECTION_HEADER[40]</c> entries. Handles <c>PointerToRawData</c> vs <c>VirtualAddress</c> fallback
    /// for in-memory vs on-disk PE layouts.
    /// </remarks>
    private static byte[]? TryFindPeSection(byte[] peImage, string sectionName)
    {
        try
        {
            if (peImage.Length < 0x40 || peImage[0] != 0x4D || peImage[1] != 0x5A)
            {
                return null;
            }

            int e_lfanew = BinaryPrimitives.ReadInt32LittleEndian(peImage.AsSpan(0x3C));
            if (e_lfanew < 0 || e_lfanew + 6 > peImage.Length)
            {
                return null;
            }

            uint ntSig = BinaryPrimitives.ReadUInt32LittleEndian(peImage.AsSpan(e_lfanew));
            if (ntSig != 0x00004550)
            {
                return null;
            }

            int fileHeaderOff = e_lfanew + 4;
            ushort numSections = BinaryPrimitives.ReadUInt16LittleEndian(peImage.AsSpan(fileHeaderOff + 2));
            ushort sizeOfOptionalHeader = BinaryPrimitives.ReadUInt16LittleEndian(peImage.AsSpan(fileHeaderOff + 16));
            int sectionHeaderOff = fileHeaderOff + 20 + sizeOfOptionalHeader;
            if (sectionHeaderOff + numSections * 40 > peImage.Length)
            {
                return null;
            }

            for (int i = 0; i < numSections; i++)
            {
                int off = sectionHeaderOff + i * 40;
                string name = Encoding.ASCII.GetString(peImage, off, 8).TrimEnd('\0');
                uint virtualSize = BinaryPrimitives.ReadUInt32LittleEndian(peImage.AsSpan(off + 8));
                uint virtualAddress = BinaryPrimitives.ReadUInt32LittleEndian(peImage.AsSpan(off + 12));
                uint rawSize = BinaryPrimitives.ReadUInt32LittleEndian(peImage.AsSpan(off + 16));
                uint rawAddr = BinaryPrimitives.ReadUInt32LittleEndian(peImage.AsSpan(off + 20));
                if (name.Equals(sectionName, StringComparison.OrdinalIgnoreCase))
                {
                    if (rawAddr + rawSize > peImage.Length)
                    {
                        if (virtualAddress + rawSize <= peImage.Length)
                        {
                            rawAddr = virtualAddress;
                        }
                        else
                        {
                            return null;
                        }
                    }

                    byte[] sectionData = new byte[rawSize];
                    Buffer.BlockCopy(peImage, (int)rawAddr, sectionData, 0, (int)rawSize);
                    if (virtualSize < rawSize)
                    {
                        Array.Resize(ref sectionData, (int)virtualSize);
                    }

                    Logger.Debug<XexFile>($"Found PE section {name} at raw 0x{rawAddr:X} size {rawSize}");
                    return sectionData;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Trace<XexFile>($"PE section search failed: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Scans a PE image for any embedded XDBF ("XDBF" <c>0x58444246</c> + version <c>0x10000</c>) as fallback.
    /// </summary>
    /// <param name="data">Decompressed PE bytes.</param>
    /// <returns>Candidate XDBF bytes from first plausible header to EOF, or null.</returns>
    /// <remarks>
    /// Used when the <c>{TitleId}</c> section name is missing (homebrew, stripped). Validates via
    /// <see cref="GpdFile.FromBytes"/> entry count &gt; 0 like <c>XexSpaExtractor.ScanForXdbf</c>.
    /// </remarks>
    private static byte[]? ScanForXdbf(byte[] data)
    {
        for (int i = 0; i + 4 < data.Length; i++)
        {
            if (data[i] == 0x58 && data[i + 1] == 0x44 && data[i + 2] == 0x42 && data[i + 3] == 0x46)
            {
                if (i + 24 <= data.Length)
                {
                    uint version = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(i + 4));
                    if (version == 0x00010000)
                    {
                        int len = data.Length - i;
                        byte[] candidate = new byte[len];
                        Buffer.BlockCopy(data, i, candidate, 0, len);
                        try
                        {
                            GpdFile gpd = GpdFile.FromBytes(candidate);
                            if (gpd.Entries.Count > 0)
                            {
                                Logger.Debug<XexFile>($"Found XDBF at offset 0x{i:X}");
                                return candidate;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Trace<XexFile>($"XDBF candidate at 0x{i:X} failed GPD parse: {ex.Message}");
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Scans arbitrary bytes for the largest embedded PNG (≥ 1024 bytes, valid IEND) as last-resort icon.
    /// </summary>
    /// <param name="data">SPA/XDBF bytes or raw XEX tail.</param>
    /// <returns>Largest PNG found or null.</returns>
    private static byte[]? ScanForPng(byte[] data)
    {
        for (int i = 0; i + 8 < data.Length; i++)
        {
            if (data[i] == 0x89 && data[i + 1] == 0x50 && data[i + 2] == 0x4E && data[i + 3] == 0x47)
            {
                int end = FindPngEnd(data, i);
                if (end > i + 67)
                {
                    int len = end - i;
                    if (len >= 1024)
                    {
                        byte[] png = new byte[len];
                        Buffer.BlockCopy(data, i, png, 0, len);
                        return png;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the end offset (after IEND chunk) of a PNG starting at <paramref name="start"/>.
    /// </summary>
    /// <param name="data">Bytes containing a PNG at <paramref name="start"/> (<c>89 50 4E 47 …</c>).</param>
    /// <param name="start">Offset of <c>89</c> (PNG signature).</param>
    /// <returns>Offset after <c>IEND</c> chunk, or -1 on malformed/truncated PNG. Caps scan at 2 MiB.</returns>
    private static int FindPngEnd(byte[] data, int start)
    {
        if (start + 8 > data.Length || data[start] != 0x89)
        {
            return -1;
        }

        int pos = start + 8;
        while (pos + 12 <= data.Length)
        {
            uint len = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pos));
            if (pos + 8 + len + 4 > data.Length)
            {
                break;
            }

            string type = Encoding.ASCII.GetString(data, pos + 4, 4);
            pos += 8 + (int)len + 4;
            if (type == "IEND")
            {
                return pos;
            }

            if (pos - start > 2_000_000)
            {
                break;
            }
        }

        return -1;
    }

    /// <summary>
    /// Parses the 24-byte XEX header (big-endian).
    /// </summary>
    /// <param name="data">XEX bytes at offset 0.</param>
    /// <returns>Populated <see cref="XexHeader"/> (magic as 4-byte array).</returns>
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
    /// Parses the <c>xex2_security_info</c> structure at <paramref name="offset"/>.
    /// </summary>
    /// <param name="data">XEX bytes.</param>
    /// <param name="offset">Absolute offset of security info (<c>Header.SecurityInfo</c>).</param>
    /// <returns>Populated <see cref="XexSecurityInfo"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <c>data.Length &lt; offset+0x1A0</c>.</exception>
    private static XexSecurityInfo ParseSecurityInfo(byte[] data, int offset)
    {
        if (data.Length < offset + 0x1A0)
        {
            throw new ArgumentException("Data too short for security info");
        }

        HvImageInfo imageInfo = new HvImageInfo
        {
            Signature = data.Skip(offset + 0x8).Take(0x100).ToArray(),
            InfoSize = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x108)),
            ImageFlags = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x10C)),
            LoadAddress = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x110)),
            ImageHash = data.Skip(offset + 0x114).Take(0x14).ToArray(),
            ImportTableCount = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x128)),
            ImportDigest = data.Skip(offset + 0x12C).Take(0x14).ToArray(),
            MediaId = data.Skip(offset + 0x140).Take(0x10).ToArray(),
            ImageKey = data.Skip(offset + 0x150).Take(0x10).ToArray(),
            ExportTableAddress = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x160)),
            HeaderHash = data.Skip(offset + 0x164).Take(0x14).ToArray(),
            GameRegion = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x178))
        };

        return new XexSecurityInfo
        {
            Size = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset)),
            ImageSize = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 4)),
            ImageInfo = imageInfo,
            AllowedMediaTypes = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x17C)),
            PageDescriptorCount = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x180))
        };
    }

    /// <summary>
    /// Finds and parses the execution info (<c>0x40006</c>) from the optional header directory.
    /// </summary>
    /// <param name="data">XEX bytes (header at 0x00, directory at <c>0x18</c>).</param>
    /// <param name="header">Already-parsed <see cref="XexHeader"/> with directory count.</param>
    /// <returns>Parsed <see cref="XexExecutionInfo"/> or null if not found/out-of-bounds.</returns>
    /// <remarks>
    /// Search key is <c>(0x400 &lt;&lt; 8) | (24 &gt;&gt; 2) = 0x40006</c> per XEX spec (ID 0x400, size 24).
    /// TitleId at <c>offset+12</c> drives <see cref="TryFindPeSection"/> section name.
    /// </remarks>
    private static XexExecutionInfo? FindExecutionInfo(byte[] data, XexHeader header)
    {
        int headerDirectoryOffset = 0x18;
        uint entryCount = header.HeaderDirectoryEntryCount;
        uint executionSearchId = (0x400 << 8) | (24 >> 2);

        for (int i = 0; i < entryCount; i++)
        {
            int entryOffset = headerDirectoryOffset + i * 8;
            if (entryOffset + 8 > data.Length)
            {
                break;
            }

            uint value = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(entryOffset));
            uint offset = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(entryOffset + 4));

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
    /// Converts a 4-byte magic array to ASCII string, trimming <c>\0</c>.
    /// </summary>
    private static string GetString(byte[] bytes) => Encoding.ASCII.GetString(bytes).Trim('\0');
}