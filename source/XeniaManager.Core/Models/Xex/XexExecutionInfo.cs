using System.Runtime.InteropServices;

namespace XeniaManager.Core.Models.Xex;

/// <summary>
/// XEX execution information structure.
/// <para>
/// Contains metadata about the executable including TitleID, MediaID, version information,
/// platform, disc number, and executable type.
/// </para>
/// <para>
/// This structure is found in the optional header directory with header ID 0x40006.
/// The search ID is calculated as: (0x400 << 8) | (24 >> 2) = 0x40006
/// where 0x400 is the execution info ID and 24 is the size of this structure.
/// </para>
/// <para>
/// The TitleID format is PPPPNNNN where:
/// - PPPP (high word): Publisher ID (e.g., 0x4D53 = Microsoft, 0x4541 = EA)
/// - NNNN (low word): Game ID (unique within publisher's catalog)
/// </para>
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct XexExecutionInfo
{
    /// <summary>
    /// Media ID identifying the physical media type and disc.
    /// Used to verify the executable is running from authorized media.
    /// Offset: 0x00 (4 bytes)
    /// </summary>
    [FieldOffset(0)] public uint MediaId;

    /// <summary>
    /// Current version of the executable.
    /// Used for title updates and version checking.
    /// Offset: 0x04 (4 bytes)
    /// </summary>
    [FieldOffset(4)] public uint Version;

    /// <summary>
    /// Base version of the executable (original release version).
    /// This represents the version of the game on the original disc.
    /// Offset: 0x08 (4 bytes)
    /// </summary>
    [FieldOffset(8)] public uint BaseVersion;

    /// <summary>
    /// Title ID uniquely identifying the game or application.
    /// Offset: 0x0C (4 bytes)
    /// </summary>
    [FieldOffset(12)] public uint TitleId;

    /// <summary>
    /// Publisher ID (high word of TitleId).
    /// Offset: 0x0C (high word)
    /// </summary>
    [FieldOffset(12)] public ushort PublisherId;

    /// <summary>
    /// Game ID (low word of TitleId).
    /// Unique identifier for the specific game within a publisher's catalog.
    /// Offset: 0x0E (low word)
    /// </summary>
    [FieldOffset(14)] public ushort GameId;

    /// <summary>
    /// Platform code indicating the target hardware.
    /// Offset: 0x10 (1 byte)
    /// </summary>
    [FieldOffset(16)] public byte Platform;

    /// <summary>
    /// Executable type indicating the kind of content.
    /// Values:
    /// - 0x00: Retail
    /// - 0x01: Debug
    /// - 0x02: Debug Retail
    /// Offset: 0x11 (1 byte)
    /// </summary>
    [FieldOffset(17)] public byte ExecutableType;

    /// <summary>
    /// Disc number for multi-disc games (1-based).
    /// For single-disc games, this is typically 1.
    /// Offset: 0x12 (1 byte)
    /// </summary>
    [FieldOffset(18)] public byte DiscNum;

    /// <summary>
    /// Total number of discs for multi-disc games.
    /// For single-disc games, this is typically 1.
    /// Offset: 0x13 (1 byte)
    /// </summary>
    [FieldOffset(19)] public byte DiscTotal;

    /// <summary>
    /// Save game ID used for identifying compatible save files.
    /// This allows save games to be shared between different versions of the same title.
    /// Offset: 0x14 (4 bytes)
    /// </summary>
    [FieldOffset(20)] public uint SaveGameId;
}