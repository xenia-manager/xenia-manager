using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using XeniaManager.Logging;
using XeniaManager.Files.Models.Account;
using XeniaManager.Files.Models.Stfs;

namespace XeniaManager.Files;

/// <summary>
/// Represents an STFS header file structure.
/// Header files are metadata files created when extracting STFS packages to Xenia's directory structure.
/// </summary>
/// <remarks>
/// Supported header file formats (matching Xenia's XCONTENT_DATA layouts):
/// <list type="bullet">
///   <item><description>0x134 (308 bytes) XCONTENT_DATA: DeviceId, ContentType, DisplayName, FileName</description></item>
///   <item><description>0x138 (312 bytes) - cross-title data: XCONTENT_DATA + TitleId at 0x134 (no XUID)</description></item>
///   <item><description>0x148 (328 bytes) - XCONTENT_DATA_AGGREGATE: XCONTENT_DATA + XUID at 0x134, TitleId at 0x13C</description></item>
///   <item><description>0x14C (332 bytes) - Full Header: XCONTENT_DATA_AGGREGATE + LicenseMask at 0x148</description></item>
/// </list>
/// Full header structure (0x14C = 332 bytes):
/// <list type="bullet">
///   <item>0x00-0x03: DeviceId (4 bytes, big endian) - typically 1 for HDD</item>
///   <item>0x04-0x07: ContentType (4 bytes, big endian)</item>
///   <item>0x08-0x107: DisplayName (256 bytes, UTF-16 BE, 128 characters)</item>
///   <item>0x108-0x131: FileName (42 bytes, ASCII)</item>
///   <item>0x132-0x133: Padding (2 bytes)</item>
///   <item>0x134-0x13B: XUID (8 bytes, big endian) - only in 0x148+ headers</item>
///   <item>0x13C-0x13F: TitleId (4 bytes, big endian) in XCONTENT_DATA_AGGREGATE layout; at 0x134 in 0x138 cross-title headers</item>
///   <item>0x140-0x143: TitleId in dashboard / XCONTENT_DATA_INTERNAL layout; always written and used as a read fallback</item>
///   <item>0x144-0x147: Padding (4 bytes, zeros)</item>
///   <item>0x148-0x14B: LicenseMask (4 bytes, big endian) - only in 0x14C headers</item>
/// </list>
/// </remarks>
public class HeaderFile
{
    /// <summary>
    /// The full size of the header file in bytes (0x14C = 332 bytes).
    /// </summary>
    public const int FullHeaderSize = 0x14C;

    /// <summary>
    /// The size of XCONTENT_AGGREGATE_DATA (0x148 = 328 bytes).
    /// Contains xuid and title_id but no license_mask.
    /// </summary>
    public const int AggregateDataSize = 0x148;

    /// <summary>
    /// The size of XCONTENT_CROSS_TITLE_DATA (0x138 = 312 bytes).
    /// Contains basic content data plus title_id.
    /// </summary>
    public const int CrossTitleDataSize = 0x138;

    /// <summary>
    /// The size of XCONTENT_DATA (0x134 = 308 bytes).
    /// Minimum valid header with device_id, content_type, display_name, and file_name.
    /// </summary>
    public const int MinimumHeaderSize = 0x134;

    /// <summary>
    /// Gets or sets the device ID (typically 1 for HDD).
    /// </summary>
    public uint DeviceId { get; set; } = 1;

    /// <summary>
    /// Gets or sets the content type.
    /// </summary>
    public ContentType ContentType { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file name (package name).
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path.
    /// For regular entries this is the sidecar .header file path.
    /// For package entries (<see cref="IsPackageEntry"/>) this is the package file itself.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this entry represents a package file kept intact in Xenia's content directory
    /// (XContent package support, Xenia Canary b11458e+) instead of an extracted directory with a sidecar .header file.
    /// </summary>
    public bool IsPackageEntry { get; set; }

    /// <summary>
    /// Gets or sets the package version (STFS metadata version field at offset 0x0358, e.g. title update version).
    /// Only populated for package entries (<see cref="IsPackageEntry"/>); 0 otherwise.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets whether <see cref="Version"/> was read from package metadata.
    /// </summary>
    public bool HasVersion { get; set; }

    /// <summary>
    /// Gets or sets the thumbnail image data embedded in the package.
    /// Only populated for package entries (<see cref="IsPackageEntry"/>); empty for extracted content.
    /// </summary>
    public byte[] ThumbnailImage { get; set; } = [];

    /// <summary>
    /// Gets or sets the XUID (defaults to 0 for installed content).
    /// Only present in headers >= 0x148 bytes.
    /// </summary>
    public AccountXuid AccountXuid { get; set; } = new AccountXuid(0);

    /// <summary>
    /// Gets or sets the title ID.
    /// Only present in headers >= 0x138 bytes.
    /// </summary>
    public uint TitleId { get; set; } = 0xFFFFFFFF; // Default value

    /// <summary>
    /// Gets or sets the license mask (typically 0 for most content).
    /// Only present in headers >= 0x14C bytes.
    /// </summary>
    public uint LicenseMask { get; set; }

    /// <summary>
    /// Gets or sets the detected header size.
    /// Automatically determined when loading from bytes but can be set manually.
    /// </summary>
    public int HeaderSize { get; set; } = FullHeaderSize;

    /// <summary>
    /// Gets whether the XUID field was present in the header.
    /// True only for headers >= 0x148 bytes.
    /// </summary>
    public bool HasXuid
    {
        get
        {
            return HeaderSize >= AggregateDataSize;
        }
    }

    /// <summary>
    /// Gets whether the title_id field was present in the header.
    /// True only for headers >= 0x138 bytes.
    /// </summary>
    public bool HasTitleId
    {
        get
        {
            return HeaderSize >= CrossTitleDataSize;
        }
    }

    /// <summary>
    /// Gets whether the license_mask field was present in the header.
    /// True only for headers >= 0x14C bytes.
    /// </summary>
    public bool HasLicenseMask
    {
        get
        {
            return HeaderSize >= FullHeaderSize;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderFile"/> class.
    /// </summary>
    public HeaderFile()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderFile"/> class with the specified parameters.
    /// </summary>
    /// <param name="titleId">The title ID in hex string format.</param>
    /// <param name="contentType">The content type.</param>
    /// <param name="packageName">The package name.</param>
    /// <param name="displayName">Optional display name (defaults to package name).</param>
    /// <param name="xuid">Optional XUID (defaults to 0 for installed content).</param>
    public HeaderFile(string titleId, ContentType contentType, string packageName, string? displayName = null, AccountXuid xuid = default)
    {
        if (uint.TryParse(titleId, NumberStyles.HexNumber, null, out uint titleIdValue))
        {
            TitleId = titleIdValue;
        }

        ContentType = contentType;
        FileName = packageName;
        DisplayName = displayName ?? packageName;
        AccountXuid = xuid;
        HeaderSize = FullHeaderSize;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderFile"/> class with the specified parameters.
    /// </summary>
    /// <param name="titleId">The title ID in hex string format.</param>
    /// <param name="contentTypeHex">The content type in hex string format.</param>
    /// <param name="packageName">The package name.</param>
    /// <param name="displayName">Optional display name (defaults to package name).</param>
    /// <param name="xuid">Optional XUID (defaults to 0 for installed content).</param>
    public HeaderFile(string titleId, string contentTypeHex, string packageName, string? displayName = null, AccountXuid xuid = default)
        : this(titleId,
            uint.TryParse(contentTypeHex, NumberStyles.HexNumber, null, out uint contentTypeValue) ? (ContentType)contentTypeValue : 0,
            packageName, displayName, xuid)
    {
    }

    /// <summary>
    /// Converts the header file to a byte array.
    /// </summary>
    /// <returns>The header data as a byte array.</returns>
    public byte[] ToBytes()
    {
        Logger.Trace<HeaderFile>($"Starting ToBytes conversion for header: '{FileName}' (DisplayName: '{DisplayName}')");

        byte[] headerData = new byte[HeaderSize];

        // 0x00-0x03: device_id (big endian)
        BinaryPrimitives.WriteUInt32BigEndian(headerData.AsSpan(0), DeviceId);
        Logger.Debug<HeaderFile>($"DeviceId: {DeviceId}");

        // 0x04-0x07: content_type (big endian)
        uint contentTypeValue = (uint)ContentType;
        BinaryPrimitives.WriteUInt32BigEndian(headerData.AsSpan(4), contentTypeValue);
        Logger.Debug<HeaderFile>($"ContentType: {ContentType} (0x{contentTypeValue:X8})");

        // 0x08-0x107: display_name_raw (UTF-16 BE, 128 characters = 256 bytes)
        byte[] nameBytes = Encoding.BigEndianUnicode.GetBytes(DisplayName);
        int nameLength = Math.Min(nameBytes.Length, 256);
        Array.Copy(nameBytes, 0, headerData, 0x08, nameLength);
        Logger.Debug<HeaderFile>($"DisplayName: '{DisplayName}' ({nameLength} bytes)");

        // 0x108-0x131: file_name_raw (42 bytes, ASCII)
        byte[] filenameBytes = Encoding.ASCII.GetBytes(FileName);
        int filenameLength = Math.Min(filenameBytes.Length, 42);
        Array.Copy(filenameBytes, 0, headerData, 0x108, filenameLength);
        Logger.Debug<HeaderFile>($"FileName: '{FileName}' ({filenameLength} bytes)");

        // 0x132-0x133: padding (2 bytes, already zeros)

        // 0x134-0x13B: XUID (8 bytes, big endian) - only in 0x148+ headers
        if (HeaderSize >= AggregateDataSize)
        {
            ulong xuidValue = AccountXuid.Value;
            BinaryPrimitives.WriteUInt64BigEndian(headerData.AsSpan(0x134), xuidValue);
            Logger.Debug<HeaderFile>($"AccountXuid: {AccountXuid} (0x{xuidValue:X16})");
        }

        // TitleId sits at 0x13C per Xenia's XCONTENT_DATA_AGGREGATE, but the
        // dashboard (and Xenia's XCONTENT_DATA_INTERNAL) stores it at 0x140.
        // Cross-title (0x138) headers carry it directly after the 0x134 base
        // since they have no XUID. Written to both 0x13C and 0x140 so every
        // producer and reader agrees (see FromBytes).
        if (HeaderSize >= AggregateDataSize)
        {
            BinaryPrimitives.WriteUInt32BigEndian(headerData.AsSpan(0x13C), TitleId);
            BinaryPrimitives.WriteUInt32BigEndian(headerData.AsSpan(0x140), TitleId);
            Logger.Debug<HeaderFile>($"TitleId: 0x{TitleId:X8}");
        }
        else if (HeaderSize >= CrossTitleDataSize)
        {
            BinaryPrimitives.WriteUInt32BigEndian(headerData.AsSpan(0x134), TitleId);
            Logger.Debug<HeaderFile>($"TitleId: 0x{TitleId:X8}");
        }

        // 0x148-0x14B: license_mask (4 bytes, big endian) - only in 0x14C headers
        if (HeaderSize >= FullHeaderSize)
        {
            BinaryPrimitives.WriteUInt32BigEndian(headerData.AsSpan(0x148), LicenseMask);
            Logger.Debug<HeaderFile>($"LicenseMask: 0x{LicenseMask:X8}");
        }

        Logger.Info<HeaderFile>($"Successfully converted header to bytes ({HeaderSize} bytes)");
        Logger.Trace<HeaderFile>($"Header data (first 64 bytes): {BitConverter.ToString(headerData, 0, Math.Min(64, headerData.Length))}");

        return headerData;
    }

    /// <summary>
    /// Creates a header file from a byte array.
    /// Supports header sizes: 0x134 (308), 0x138 (312), 0x148 (328), 0x14C (332).
    /// </summary>
    /// <param name="data">The header data.</param>
    /// <returns>A new <see cref="HeaderFile"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the data is too short.</exception>
    public static HeaderFile FromBytes(byte[] data)
    {
        Logger.Trace<HeaderFile>($"Parsing header from bytes ({data.Length} bytes)");
        Logger.Trace<HeaderFile>($"First 64 bytes: {BitConverter.ToString(data, 0, Math.Min(64, data.Length))}");

        if (data.Length < MinimumHeaderSize)
        {
            Logger.Error<HeaderFile>($"Data too short to contain valid header (minimum {MinimumHeaderSize} bytes, got {data.Length})");
            throw new ArgumentException(
                $"Data too short to contain valid header (minimum {MinimumHeaderSize} bytes for XCONTENT_DATA, got {data.Length})",
                nameof(data));
        }

        HeaderFile header = new HeaderFile
        {
            // Determine header size and round down to known sizes
            HeaderSize = data.Length switch
            {
                >= FullHeaderSize => FullHeaderSize,
                >= AggregateDataSize => AggregateDataSize,
                >= CrossTitleDataSize => CrossTitleDataSize,
                _ => MinimumHeaderSize
            }
        };

        Logger.Debug<HeaderFile>($"Detected header size: 0x{header.HeaderSize:X} ({header.HeaderSize} bytes)");

        // 0x00-0x03: device_id (always present)
        header.DeviceId = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0));

        Logger.Debug<HeaderFile>($"DeviceId: {header.DeviceId}");

        // 0x04-0x07: content_type (always present)
        uint contentTypeValue = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4));

        header.ContentType = (ContentType)contentTypeValue;
        Logger.Debug<HeaderFile>($"ContentType: {header.ContentType} (0x{contentTypeValue:X8})");

        // 0x08-0x107: display_name_raw (UTF-16 BE, always present)
        header.DisplayName = Encoding.BigEndianUnicode.GetString(data.AsSpan(8, 256)).TrimEnd('\0');
        Logger.Info<HeaderFile>($"DisplayName: '{header.DisplayName}'");

        // 0x108-0x131: file_name_raw (ASCII, always present)
        header.FileName = Encoding.ASCII.GetString(data.AsSpan(0x108, 42)).TrimEnd('\0');
        Logger.Info<HeaderFile>($"FileName: '{header.FileName}'");

        // 0x134-0x13B: XUID (only in 0x148+ headers)
        if (header.HeaderSize >= AggregateDataSize)
        {
            header.AccountXuid = new AccountXuid(BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(0x134)));
            Logger.Debug<HeaderFile>($"AccountXuid: {header.AccountXuid} (0x{header.AccountXuid.Value:X16})");
        }
        else
        {
            header.AccountXuid = new AccountXuid(0);
            Logger.Debug<HeaderFile>($"Header too small for XUID, using default (0)");
        }

        // Prefer a nonzero TitleId at 0x13C (Xenia's XCONTENT_DATA_AGGREGATE);
        // the dashboard, Xenia's XCONTENT_DATA_INTERNAL, and older revisions
        // of this app store it at 0x140, so fall back to that when 0x13C is zero.
        // Cross-title (0x138) headers carry it directly after the 0x134 base
        // since they have no XUID.
        if (header.HeaderSize >= AggregateDataSize)
        {
            header.TitleId = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0x13C));
            if (header.TitleId == 0)
            {
                header.TitleId = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0x140));
            }

            Logger.Debug<HeaderFile>($"TitleId: 0x{header.TitleId:X8}");
        }
        else if (header.HeaderSize >= CrossTitleDataSize)
        {
            header.TitleId = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0x134));
            Logger.Debug<HeaderFile>($"TitleId: 0x{header.TitleId:X8}");
        }
        else
        {
            header.TitleId = 0xFFFFFFFF; // Default value
            Logger.Warning<HeaderFile>($"Header too small for title_id, using default (0xFFFFFFFF)");
        }

        // 0x148-0x14B: license_mask (only in 0x14C headers)
        if (header.HeaderSize >= FullHeaderSize)
        {
            header.LicenseMask = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0x148));
            Logger.Debug<HeaderFile>($"LicenseMask: 0x{header.LicenseMask:X8}");
        }
        else
        {
            header.LicenseMask = 0; // Default value
            Logger.Warning<HeaderFile>($"Header too small for license_mask, using default (0)");
        }

        Logger.Info<HeaderFile>(
            $"Successfully parsed header: '{header.DisplayName}' (Size: 0x{header.HeaderSize:X}, TitleId: 0x{header.TitleId:X8}, ContentType: {header.ContentType})");
        Logger.Trace<HeaderFile>($"FromBytes parsing completed successfully");

        return header;
    }

    /// <summary>
    /// Saves the header file to the specified path.
    /// </summary>
    /// <param name="filePath">The file path to save to.</param>
    public void Save(string filePath)
    {
        Logger.Trace<HeaderFile>($"Starting Save operation to path: {filePath}");
        Logger.Debug<HeaderFile>($"Header details - FileName: '{FileName}', DisplayName: '{DisplayName}', TitleId: 0x{TitleId:X8}, Size: {HeaderSize} bytes");

        try
        {
            byte[] headerData = ToBytes();
            Logger.Info<HeaderFile>($"Successfully converted header to bytes ({headerData.Length} bytes)");

            // Ensure the directory exists
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Logger.Info<HeaderFile>($"Creating directory: {directory}");
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(filePath, headerData);
            Logger.Info<HeaderFile>($"Header file saved successfully to {filePath}");
        }
        catch (Exception ex)
        {
            Logger.Error<HeaderFile>($"Failed to save header file to {filePath}: {ex.Message}");
            Logger.LogExceptionDetails<HeaderFile>(ex);
            throw;
        }
    }

    /// <summary>
    /// Loads a header file from the specified path.
    /// Supports header sizes: 0x134 (308), 0x138 (312), 0x148 (328), 0x14C (332).
    /// </summary>
    /// <param name="filePath">The file path to load from.</param>
    /// <returns>A new <see cref="HeaderFile"/> instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when the header file is an invalid size.</exception>
    public static HeaderFile Load(string filePath)
    {
        Logger.Debug<HeaderFile>($"Loading header file from {filePath}");

        if (!File.Exists(filePath))
        {
            Logger.Error<HeaderFile>($"Header file not found at {filePath}");
            throw new FileNotFoundException($"Header file not found: {filePath}", filePath);
        }

        try
        {
            // Check file size before loading
            long fileSize = new FileInfo(filePath).Length;
            Logger.Debug<HeaderFile>($"Header file size: {fileSize} bytes");

            if (fileSize < MinimumHeaderSize)
            {
                Logger.Error<HeaderFile>($"Header file too small: {fileSize} bytes (minimum {MinimumHeaderSize})");
                throw new ArgumentException(
                    $"Header file too small: {fileSize} bytes (minimum {MinimumHeaderSize} bytes for XCONTENT_DATA)",
                    nameof(filePath));
            }

            if (fileSize > FullHeaderSize)
            {
                Logger.Warning<HeaderFile>($"Header file larger than expected: {fileSize} bytes (expected max {FullHeaderSize})");
            }

            byte[] data = File.ReadAllBytes(filePath);
            Logger.Info<HeaderFile>($"Loaded header file from {filePath} ({data.Length} bytes)");

            HeaderFile header = FromBytes(data);
            header.FilePath = filePath;
            Logger.Debug<HeaderFile>($"Successfully loaded header: '{header.DisplayName}' (Size: 0x{header.HeaderSize:X})");

            return header;
        }
        catch (ArgumentException)
        {
            throw; // Re-throw argument exceptions as-is
        }
        catch (Exception ex)
        {
            Logger.Error<HeaderFile>($"Failed to load header file from {filePath}: {ex.Message}");
            Logger.LogExceptionDetails<HeaderFile>(ex);
            throw;
        }
    }
}