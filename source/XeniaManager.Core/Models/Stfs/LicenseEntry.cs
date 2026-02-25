using System.Buffers.Binary;

namespace XeniaManager.Core.Models.Stfs;

/// <summary>
/// Represents a license entry in the STFS package licensing data.
/// </summary>
public struct LicenseEntry
{
    /// <summary>
    /// License ID (XUID / PUID / console id).
    /// </summary>
    public long LicenseId;

    /// <summary>
    /// License bits.
    /// </summary>
    public int LicenseBits;

    /// <summary>
    /// License flags.
    /// </summary>
    public int LicenseFlags;

    /// <summary>
    /// Size of a license entry (0x8 + 0x4 + 0x4 = 0x10).
    /// </summary>
    public const int Size = 0x10;

    /// <summary>
    /// Parses a license entry from raw bytes.
    /// </summary>
    /// <param name="data">The raw byte data containing the license entry.</param>
    /// <param name="offset">The offset in the data where the entry starts.</param>
    /// <returns>A populated LicenseEntry structure.</returns>
    public static LicenseEntry FromBytes(byte[] data, int offset = 0)
    {
        return new LicenseEntry
        {
            LicenseId = BinaryPrimitives.ReadInt64BigEndian(data.AsSpan(offset)),
            LicenseBits = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset + 0x8)),
            LicenseFlags = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset + 0xC))
        };
    }

    /// <summary>
    /// Converts the license entry to a byte array.
    /// </summary>
    /// <returns>The license entry as a byte array.</returns>
    public byte[] ToBytes()
    {
        byte[] data = new byte[Size];
        BinaryPrimitives.WriteInt64BigEndian(data.AsSpan(0x0), LicenseId);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0x8), LicenseBits);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0xC), LicenseFlags);
        return data;
    }
}
