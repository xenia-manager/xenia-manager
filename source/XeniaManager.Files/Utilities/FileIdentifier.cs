using System.Buffers.Binary;
using XeniaManager.Files;
using XeniaManager.Logging;
using XeniaManager.Files.Models;

namespace XeniaManager.Files.Utilities;

/// <summary>
/// Utility class for identifying Xbox file types by reading their headers.
/// Based on Xenia's Emulator::GetFileSignature implementation.
/// </summary>
public class FileIdentifier
{
    /// <summary>
    /// Magic bytes for XEX0 files ("XEX0").
    /// </summary>
    private const uint Xex0Magic = 0x58455830;

    /// <summary>
    /// Magic bytes for XEX? files ("XEX?").
    /// </summary>
    private const uint XexQMagic = 0x5845583F;

    /// <summary>
    /// Magic bytes for XEX- files ("XEX-").
    /// </summary>
    private const uint XexHMagic = 0x5845582D;

    /// <summary>
    /// Magic bytes for XEX% files ("XEX%").
    /// </summary>
    private const uint Xex25Magic = 0x58455825;

    /// <summary>
    /// Magic bytes for XEX1 files ("XEX1").
    /// </summary>
    private const uint Xex1Magic = 0x58455831;

    /// <summary>
    /// Magic bytes for XEX2 files ("XEX2").
    /// </summary>
    private const uint Xex2Magic = 0x58455832;

    /// <summary>
    /// Magic bytes for ELF files (0x7F "ELF").
    /// </summary>
    private const uint ElfMagic = 0x7F454C46;

    /// <summary>
    /// Magic bytes for XBE files ("XBEH").
    /// </summary>
    private const uint XbeMagic = 0x58424548;

    /// <summary>
    /// Magic bytes for GDFX/XISO discs ("XSF\x1A").
    /// </summary>
    private const uint XsfMagic = 0x5853461A;

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
    /// ZAR footer magic, read from the last 4 bytes of the file.
    /// </summary>
    private const uint ZarMagic = 0x169F52D6;

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

        // Handle SVOD directories (GOD / Installed Game) containing Data0000 etc.
        if (Directory.Exists(filePath))
        {
            if (IsSvodDirectory(filePath))
            {
                Logger.Info<FileIdentifier>($"File identified as SVOD by directory structure: {filePath}");
                return FileSignature.SVOD;
            }
        }

        if (!File.Exists(filePath))
        {
            Logger.Error<FileIdentifier>($"File does not exist: {filePath}");
            throw new FileNotFoundException($"File does not exist at {filePath}", filePath);
        }

        // Read the header to detect file types (magic-first: extension must not shadow magic,
        // e.g. an XEX1 file named .xex). Extension fallbacks below are leniency for unknown content.
        byte[] headerBytes = ReadFirstBytes(filePath, 4);
        Logger.Trace<FileIdentifier>($"File header: 0x{(headerBytes.Length < 4 ? 0 : BinaryPrimitives.ReadUInt32BigEndian(headerBytes)):X8}");

        FileSignature detectedSignature = FileSignature.Unknown;
        if (headerBytes.Length >= 4)
        {
            uint header = BinaryPrimitives.ReadUInt32BigEndian(headerBytes);
            detectedSignature = header switch
            {
                Xex0Magic => FileSignature.XEX0,
                XexQMagic => FileSignature.XEXQ,
                XexHMagic => FileSignature.XEXH,
                Xex25Magic => FileSignature.XEX25,
                Xex1Magic => FileSignature.XEX1,
                Xex2Magic => FileSignature.XEX2,
                ElfMagic => FileSignature.ELF,
                XbeMagic => FileSignature.XBE,
                XsfMagic => FileSignature.XISO,
                ConMagic => FileSignature.CON,
                LiveMagic => FileSignature.LIVE,
                PirsMagic => FileSignature.PIRS,
                _ => FileSignature.Unknown
            };
        }

        // "MZ" header (only the first 2 bytes are compared).
        if (detectedSignature == FileSignature.Unknown && headerBytes.Length >= 2 && headerBytes[0] == (byte)'M' && headerBytes[1] == (byte)'Z')
        {
            Logger.Info<FileIdentifier>($"File identified as EXE by header: {filePath}");
            return FileSignature.EXE;
        }

        // Differentiate STFS vs SVOD (GOD) when magic is CON/LIVE/PIRS – SVOD has DescriptorType == 1 at 0x3A9
        if (detectedSignature is FileSignature.CON or FileSignature.LIVE or FileSignature.PIRS)
        {
            if (IsSvodPackage(filePath))
            {
                Logger.Info<FileIdentifier>($"File identified as SVOD by descriptor: {filePath}");
                return FileSignature.SVOD;
            }

            Logger.Info<FileIdentifier>($"File identified as {detectedSignature} by header: {filePath}");
            return detectedSignature;
        }

        if (detectedSignature != FileSignature.Unknown)
        {
            Logger.Info<FileIdentifier>($"File identified as {detectedSignature} by header: {filePath}");
            return detectedSignature;
        }

        // ZAR footer magic (checked in the last 4 bytes, independent of extension).
        if (HasZarFooter(filePath))
        {
            Logger.Info<FileIdentifier>($"File identified as ZAR by footer: {filePath}");
            return FileSignature.ZAR;
        }

        // Extension fallbacks for files with unknown content (lenience, not magic detection).
        // NOTE: These intentionally run after magic/footer detection so magic always wins.
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

        // Check if it might be an XISO by validating the structure (sector probe).
        if (IsPossibleXiso(filePath))
        {
            Logger.Info<FileIdentifier>($"File identified as XISO by structure: {filePath}");
            return FileSignature.XISO;
        }

        Logger.Warning<FileIdentifier>($"Unable to identify file type. Extension: {extension}");
        return FileSignature.Unknown;
    }

    /// <summary>
    /// Reads up to <paramref name="count"/> bytes from the start of a file.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="count">Maximum number of bytes to read.</param>
    /// <returns>The bytes read (fewer than <paramref name="count"/> for short files).</returns>
    private static byte[] ReadFirstBytes(string filePath, int count)
    {
        using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        byte[] buffer = new byte[count];
        int read = stream.Read(buffer, 0, count);
        if (read < count)
        {
            Array.Resize(ref buffer, read);
        }

        return buffer;
    }

    /// <summary>
    /// Checks if a file ends with the ZAR footer magic.
    /// </summary>
    /// <param name="filePath">The path to the file to check.</param>
    /// <returns>True if the last 4 bytes equal <c>0x169F52D6</c> (big-endian), false otherwise.</returns>
    private static bool HasZarFooter(string filePath)
    {
        try
        {
            using FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length < 4)
            {
                return false;
            }

            byte[] footer = new byte[4];
            stream.Seek(-4, SeekOrigin.End);
            if (stream.Read(footer, 0, 4) < 4)
            {
                return false;
            }

            return BinaryPrimitives.ReadUInt32BigEndian(footer) == ZarMagic;
        }
        catch (Exception ex)
        {
            Logger.Trace<FileIdentifier>($"ZAR footer check failed for {filePath}: {ex.Message}");
            return false;
        }
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

    /// <summary>
    /// Checks if a file is an SVOD package (GOD / Installed Game) by reading DescriptorType at 0x3A9 (int32 BE).
    /// </summary>
    private static bool IsSvodPackage(string filePath)
    {
        try
        {
            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (fs.Length < 0x3AD)
            {
                return false;
            }

            byte[] buf = new byte[4];
            fs.Seek(0x3A9, SeekOrigin.Begin);
            int read = fs.Read(buf, 0, 4);
            if (read < 4)
            {
                return false;
            }

            int descriptorType = BinaryPrimitives.ReadInt32BigEndian(buf);
            // SVOD has DescriptorType == 1, STFS == 0
            return descriptorType == 1;
        }
        catch (Exception ex)
        {
            Logger.Trace<FileIdentifier>($"SVOD check failed for {filePath}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if a directory is an SVOD package (contains Data0000 etc. or header with SVOD descriptor).
    /// </summary>
    private static bool IsSvodDirectory(string dirPath)
    {
        try
        {
            // SvodFile.IsSvodPackage handles both file and directory, delegate to avoid duplication
            return SvodFile.IsSvodPackage(dirPath);
        }
        catch (Exception ex)
        {
            Logger.Trace<FileIdentifier>($"SVOD directory check failed for {dirPath}: {ex.Message}");
            return false;
        }
    }
}