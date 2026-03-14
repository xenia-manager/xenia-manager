using System.Buffers.Binary;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Utility class for identifying Xbox file types by reading their headers.
/// Based on Xenia's Emulator::GetFileSignature implementation.
/// </summary>
public class FileIdentifier
{
    /// <summary>
    /// Magic bytes for XEX1 files ("XEX1").
    /// </summary>
    private const uint Xex1Magic = 0x58455831;

    /// <summary>
    /// Magic bytes for XEX2 files ("XEX2").
    /// </summary>
    private const uint Xex2Magic = 0x58455832;

    /// <summary>
    /// Magic bytes for CON STFS packages ("CON ").
    /// </summary>
    private const uint ConMagic = 0x434F4E20;

    /// <summary>
    /// Magic bytes for LIVE STFS packages ("LIVE").
    /// </summary>
    private const uint LiveMagic = 0x4C495645;

    /// <summary>
    /// Magic bytes for PIRS STFS packages ("PIRS").
    /// </summary>
    private const uint PirsMagic = 0x50495253;

    /// <summary>
    /// ISO file extensions.
    /// </summary>
    private static readonly HashSet<string> IsoExtensions = [".iso", ".xiso"];

    /// <summary>
    /// XEX file extension.
    /// </summary>
    private static readonly HashSet<string> XexExtensions = [".xex"];

    /// <summary>
    /// ZAR file extension.
    /// </summary>
    private static readonly HashSet<string> ZarExtensions = [".zar"];

    /// <summary>
    /// Identifies the type of file by reading its header (and footer for some formats).
    /// </summary>
    /// <param name="filePath">The path to the file to identify.</param>
    /// <returns>The detected <see cref="FileSignature"/>.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static FileSignature IdentifyFileType(string filePath)
    {
        Logger.Trace<FileIdentifier>($"Identifying file type for: {filePath}");

        if (!File.Exists(filePath))
        {
            Logger.Error<FileIdentifier>($"File does not exist: {filePath}");
            throw new FileNotFoundException($"File does not exist at {filePath}", filePath);
        }

        // First check by extension for ISO and XEX files
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        Logger.Debug<FileIdentifier>($"File extension: {extension}");

        if (IsoExtensions.Contains(extension))
        {
            Logger.Info<FileIdentifier>($"File identified as ISO/XISO by extension: {filePath}");
            return FileSignature.ISO;
        }

        if (XexExtensions.Contains(extension))
        {
            Logger.Info<FileIdentifier>($"File identified as XEX by extension: {filePath}");
            return FileSignature.XEX2;
        }

        if (ZarExtensions.Contains(extension))
        {
            Logger.Info<FileIdentifier>($"File identified as ZAR by extension: {filePath}");
            return FileSignature.ZAR;
        }

        // Read the header to detect file types
        uint header = ReadHeaderAsUInt32(filePath);
        Logger.Trace<FileIdentifier>($"File header: 0x{header:X8}");

        FileSignature detectedSignature = header switch
        {
            Xex1Magic => FileSignature.XEX1,
            Xex2Magic => FileSignature.XEX2,
            ConMagic => FileSignature.CON,
            LiveMagic => FileSignature.LIVE,
            PirsMagic => FileSignature.PIRS,
            _ => FileSignature.Unknown
        };

        // Check if it might be an XISO by validating the structure
        if (detectedSignature == FileSignature.Unknown)
        {
            if (IsPossibleXiso(filePath))
            {
                Logger.Info<FileIdentifier>($"File identified as XISO by structure: {filePath}");
                return FileSignature.XISO;
            }
        }

        if (detectedSignature != FileSignature.Unknown)
        {
            Logger.Info<FileIdentifier>($"File identified as {detectedSignature} by header: {filePath}");
        }
        else
        {
            Logger.Warning<FileIdentifier>($"Unable to identify file type. Header: 0x{header:X8}, Extension: {extension}");
        }

        return detectedSignature;
    }

    /// <summary>
    /// Reads the first 4 bytes of a file and returns them as a big-endian UInt32.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <returns>The first 4 bytes of the file as a big-endian UInt32.</returns>
    private static uint ReadHeaderAsUInt32(string filePath)
    {
        using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using BinaryReader reader = new BinaryReader(stream);

        byte[] headerBytes = reader.ReadBytes(4);
        return BinaryPrimitives.ReadUInt32BigEndian(headerBytes);
    }

    /// <summary>
    /// Checks if a file might be an XISO by attempting to load it as an IsoFile.
    /// </summary>
    /// <param name="filePath">The path to the file to check.</param>
    /// <returns>True if the file can be loaded as a valid IsoFile, false otherwise.</returns>
    private static bool IsPossibleXiso(string filePath)
    {
        try
        {
            using IsoFile iso = IsoFile.Load(filePath);
            return iso.IsValid;
        }
        catch (Exception ex)
        {
            Logger.Trace<FileIdentifier>($"XISO validation failed: {ex.Message}");
            return false;
        }
    }
}