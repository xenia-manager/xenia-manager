using System.Globalization;
using System.Text;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Models.Files.Stfs;

namespace XeniaManager.Core.Files;

/// <summary>
/// Represents an STFS header file structure.
/// Header files are metadata files created when extracting STFS packages to Xenia's directory structure.
/// </summary>
/// <remarks>
/// Supported header file formats:
/// <list type="bullet">
///   <item><description>0x134 (308 bytes) XCONTENT_DATA: DeviceId, ContentType, DisplayName, FileName</description></item>
///   <item><description>0x138 (312 bytes) - XCONTENT_CROSS_TITLE_DATA: XCONTENT_DATA + TitleId</description></item>
///   <item><description>0x148 (328 bytes) - XCONTENT_AGGREGATE_DATA: XCONTENT_DATA + XUID, TitleId</description></item>
///   <item><description>0x14C (332 bytes) - Full Header: XCONTENT_AGGREGATE_DATA + LicenseMask</description></item>
/// </list>
/// Full header structure (0x14C = 332 bytes):
/// <list type="bullet">
///   <item>0x00-0x03: DeviceId (4 bytes, big endian) - typically 1 for HDD</item>
///   <item>0x04-0x07: ContentType (4 bytes, big endian)</item>
///   <item>0x08-0x107: DisplayName (256 bytes, UTF-16 BE, 128 characters)</item>
///   <item>0x108-0x131: FileName (42 bytes, ASCII)</item>
///   <item>0x132-0x133: Padding (2 bytes)</item>
///   <item>0x134-0x13B: XUID (8 bytes, big endian) - only in 0x148+ headers</item>
///   <item>0x13C-0x13F: Padding (4 bytes, zeros)</item>
///   <item>0x140-0x143: TitleId (4 bytes, big endian) - only in 0x138+ headers</item>
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
    public bool HasXuid => HeaderSize >= AggregateDataSize;

    /// <summary>
    /// Gets whether the title_id field was present in the header.
    /// True only for headers >= 0x138 bytes.
    /// </summary>
    public bool HasTitleId => HeaderSize >= CrossTitleDataSize;

    /// <summary>
    /// Gets whether the license_mask field was present in the header.
    /// True only for headers >= 0x14C bytes.
    /// </summary>
    public bool HasLicenseMask => HeaderSize >= FullHeaderSize;

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
    {
        if (uint.TryParse(titleId, NumberStyles.HexNumber, null, out uint titleIdValue))
        {
            TitleId = titleIdValue;
        }

        if (uint.TryParse(contentTypeHex, NumberStyles.HexNumber, null, out uint contentTypeValue))
        {
            ContentType = (ContentType)contentTypeValue;
        }

        FileName = packageName;
        DisplayName = displayName ?? packageName;
        AccountXuid = xuid;
        HeaderSize = FullHeaderSize;
    }

    /// <summary>
    /// Converts the header file to a byte array.
    /// </summary>
    /// <returns>The header data as a byte array.</returns>
    public byte[] ToBytes()
    {
        Logger.Trace<HeaderFile>($"Starting ToBytes conversion for header: '{FileName}' (DisplayName: '{DisplayName}')");

        byte[] headerData = new byte[HeaderSize];
        int offset = 0;

        // 0x00-0x03: device_id (big endian)
        headerData[offset++] = (byte)((DeviceId >> 24) & 0xFF);
        headerData[offset++] = (byte)((DeviceId >> 16) & 0xFF);
        headerData[offset++] = (byte)((DeviceId >> 8) & 0xFF);
        headerData[offset++] = (byte)(DeviceId & 0xFF);
        Logger.Debug<HeaderFile>($"DeviceId: {DeviceId}");

        // 0x04-0x07: content_type (big endian)
        uint contentTypeValue = (uint)ContentType;
        headerData[offset++] = (byte)((contentTypeValue >> 24) & 0xFF);
        headerData[offset++] = (byte)((contentTypeValue >> 16) & 0xFF);
        headerData[offset++] = (byte)((contentTypeValue >> 8) & 0xFF);
        headerData[offset++] = (byte)(contentTypeValue & 0xFF);
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
            headerData[0x134] = (byte)((xuidValue >> 56) & 0xFF);
            headerData[0x135] = (byte)((xuidValue >> 48) & 0xFF);
            headerData[0x136] = (byte)((xuidValue >> 40) & 0xFF);
            headerData[0x137] = (byte)((xuidValue >> 32) & 0xFF);
            headerData[0x138] = (byte)((xuidValue >> 24) & 0xFF);
            headerData[0x139] = (byte)((xuidValue >> 16) & 0xFF);
            headerData[0x13A] = (byte)((xuidValue >> 8) & 0xFF);
            headerData[0x13B] = (byte)(xuidValue & 0xFF);
            Logger.Debug<HeaderFile>($"AccountXuid: {AccountXuid} (0x{xuidValue:X16})");
        }

        // 0x13C-0x13F: reserved/padding (4 bytes, zeros)

        // 0x140-0x143: title_id (4 bytes, big endian) - in 0x138+ headers
        if (HeaderSize >= CrossTitleDataSize)
        {
            headerData[0x140] = (byte)((TitleId >> 24) & 0xFF);
            headerData[0x141] = (byte)((TitleId >> 16) & 0xFF);
            headerData[0x142] = (byte)((TitleId >> 8) & 0xFF);
            headerData[0x143] = (byte)(TitleId & 0xFF);
            Logger.Debug<HeaderFile>($"TitleId: 0x{TitleId:X8}");
        }

        // 0x144-0x147: padding (4 bytes, zeros) - only in 0x14C headers

        // 0x148-0x14B: license_mask (4 bytes, big endian) - only in 0x14C headers
        if (HeaderSize >= FullHeaderSize)
        {
            headerData[0x148] = (byte)((LicenseMask >> 24) & 0xFF);
            headerData[0x149] = (byte)((LicenseMask >> 16) & 0xFF);
            headerData[0x14A] = (byte)((LicenseMask >> 8) & 0xFF);
            headerData[0x14B] = (byte)(LicenseMask & 0xFF);
            Logger.Debug<HeaderFile>($"LicenseMask: 0x{LicenseMask:X8}");
        }

        Logger.Info<HeaderFile>($"Successfully converted header to bytes ({HeaderSize} bytes)");
        Logger.Trace<HeaderFile>($"Header data (first 64 bytes): {BitConverter.ToString(headerData.Take(64).ToArray())}");

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
        Logger.Trace<HeaderFile>($"First 64 bytes: {BitConverter.ToString(data.Take(64).ToArray())}");

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
        header.DeviceId = BitConverter.ToUInt32(data, 0);
        if (BitConverter.IsLittleEndian)
        {
            header.DeviceId = SwapEndian(header.DeviceId);
        }
        Logger.Debug<HeaderFile>($"DeviceId: {header.DeviceId}");

        // 0x04-0x07: content_type (always present)
        uint contentTypeValue = BitConverter.ToUInt32(data, 4);
        if (BitConverter.IsLittleEndian)
        {
            contentTypeValue = SwapEndian(contentTypeValue);
        }
        header.ContentType = (ContentType)contentTypeValue;
        Logger.Debug<HeaderFile>($"ContentType: {header.ContentType} (0x{contentTypeValue:X8})");

        // 0x08-0x107: display_name_raw (UTF-16 BE, always present)
        byte[] displayNameBytes = new byte[256];
        Array.Copy(data, 8, displayNameBytes, 0, 256);
        header.DisplayName = Encoding.BigEndianUnicode.GetString(displayNameBytes).TrimEnd('\0');
        Logger.Info<HeaderFile>($"DisplayName: '{header.DisplayName}'");

        // 0x108-0x131: file_name_raw (ASCII, always present)
        byte[] fileNameBytes = new byte[42];
        Array.Copy(data, 0x108, fileNameBytes, 0, 42);
        header.FileName = Encoding.ASCII.GetString(fileNameBytes).TrimEnd('\0');
        Logger.Info<HeaderFile>($"FileName: '{header.FileName}'");

        // 0x134-0x13B: XUID (only in 0x148+ headers)
        if (header.HeaderSize >= AggregateDataSize)
        {
            byte[] xuidBytes = new byte[8];
            Array.Copy(data, 0x134, xuidBytes, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(xuidBytes);
            }
            header.AccountXuid = new AccountXuid(BitConverter.ToUInt64(xuidBytes, 0));
            Logger.Debug<HeaderFile>($"AccountXuid: {header.AccountXuid} (0x{header.AccountXuid.Value:X16})");
        }
        else
        {
            header.AccountXuid = new AccountXuid(0);
            Logger.Debug<HeaderFile>($"Header too small for XUID, using default (0)");
        }

        // 0x140-0x143: title_id (in 0x138+ headers)
        if (header.HeaderSize >= CrossTitleDataSize)
        {
            header.TitleId = BitConverter.ToUInt32(data, 0x140);
            if (BitConverter.IsLittleEndian)
            {
                header.TitleId = SwapEndian(header.TitleId);
            }
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
            header.LicenseMask = BitConverter.ToUInt32(data, 0x148);
            if (BitConverter.IsLittleEndian)
            {
                header.LicenseMask = SwapEndian(header.LicenseMask);
            }
            Logger.Debug<HeaderFile>($"LicenseMask: 0x{header.LicenseMask:X8}");
        }
        else
        {
            header.LicenseMask = 0; // Default value
            Logger.Warning<HeaderFile>($"Header too small for license_mask, using default (0)");
        }

        Logger.Info<HeaderFile>($"Successfully parsed header: '{header.DisplayName}' (Size: 0x{header.HeaderSize:X}, TitleId: 0x{header.TitleId:X8}, ContentType: {header.ContentType})");
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

    /// <summary>
    /// Converts a 32-bit unsigned integer from little-endian to big-endian or vice versa.
    /// </summary>
    /// <param name="value">The 32-bit unsigned integer value to swap the endian format of.</param>
    /// <returns>The 32-bit unsigned integer with its byte order reversed.</returns>
    private static uint SwapEndian(uint value)
    {
        return ((value & 0x000000FF) << 24) |
               ((value & 0x0000FF00) << 8) |
               ((value & 0x00FF0000) >> 8) |
               ((value & 0xFF000000) >> 24);
    }
}