using System.Text;

namespace XeniaManager.Core.Profile;

/// <summary>
/// Represents an Xbox 360 Profile Account structure.
/// <para>
/// <remarks>
/// See <see cref="https://free60.org/System-Software/Profile_Account/"/> for details
/// </remarks>
/// </para>
/// </summary>
public class ProfileInfo
{
    #region Fields
    public uint ReservedFlags; // 0x00
    public uint LiveFlags;     // 0x04
    public string Gamertag { get; set; } = string.Empty;    // 0x08 (16 chars, Unicode, 32 bytes)
    public ulong Xuid;         // 0x28
    public string XuidAsString => $"{Xuid:X16}";
    public uint CachedUserFlags; // 0x30
    public string ServiceProvider { get; set; } = string.Empty; // 0x34 (4 ASCII)
    public byte[] PasscodeKeys; // 0x38 (4 bytes)
    public string OnlineDomain { get; set; } = string.Empty; // 0x3C (20 ASCII)
    public string OnlineKerberosRealm { get; set; } = string.Empty; // 0x50 (24 ASCII)
    public byte[] OnlineKey; // 0x68 (16 bytes)
    public string UserPassportMembername { get; set; } = string.Empty; // 0x78 (114 ASCII)
    public string UserPassportPassword { get; set; } = string.Empty; // 0xEA (32 ASCII)
    public string OwnerPassportMembername { get; set; } = string.Empty; // 0x10A (114 ASCII)
    public static int Size => 8 + 32 + 8 + 4 + 4 + 4 + 20 + 24 + 16 + 114 + 32 + 114;
    #endregion

    #region Constructors
    public ProfileInfo()
    {
        PasscodeKeys = new byte[4];
        OnlineKey = new byte[16];
    }
    #endregion

    #region Methods
    public override string ToString() => $"{Gamertag} ({XuidAsString})";

    /// <summary>
    /// Constructs ProfileInfo from decrypted byte array
    /// </summary>
    /// <param name="data">Decrypted Profile file as byte array</param>
    /// <returns>ProfileInfo</returns>
    public static ProfileInfo FromBytes(byte[] data)
    {
        ProfileInfo info = new ProfileInfo();
        int offset = 0;
        info.ReservedFlags = BitConverter.ToUInt32(data, offset); offset += 4;
        info.LiveFlags = BitConverter.ToUInt32(data, offset); offset += 4;
        // --- Gamertag (swap bytes before decoding) ---
        byte[] gtBytes = new byte[32];
        Array.Copy(data, offset, gtBytes, 0, 32);
        for (int i = 0; i < 32; i += 2)
        {
            byte tmp = gtBytes[i];
            gtBytes[i] = gtBytes[i + 1];
            gtBytes[i + 1] = tmp;
        }
        info.Gamertag = Encoding.Unicode.GetString(gtBytes).TrimEnd('\0');
        offset += 32;
        info.Xuid = BitConverter.ToUInt64(data, offset); offset += 8;
        info.CachedUserFlags = BitConverter.ToUInt32(data, offset); offset += 4;
        info.ServiceProvider = Encoding.ASCII.GetString(data, offset, 4).TrimEnd('\0'); offset += 4;
        Array.Copy(data, offset, info.PasscodeKeys, 0, 4); offset += 4;
        info.OnlineDomain = Encoding.ASCII.GetString(data, offset, 20).TrimEnd('\0'); offset += 20;
        info.OnlineKerberosRealm = Encoding.ASCII.GetString(data, offset, 24).TrimEnd('\0'); offset += 24;
        Array.Copy(data, offset, info.OnlineKey, 0, 16); offset += 16;
        info.UserPassportMembername = Encoding.ASCII.GetString(data, offset, 114).TrimEnd('\0'); offset += 114;
        info.UserPassportPassword = Encoding.ASCII.GetString(data, offset, 32).TrimEnd('\0'); offset += 32;
        info.OwnerPassportMembername = Encoding.ASCII.GetString(data, offset, 114).TrimEnd('\0'); offset += 114;
        return info;
    }

    /// <summary>
    /// Convers the ProfileInfo to a byte array
    /// </summary>
    /// <returns>ProfileInfo as byte array</returns>
    public byte[] ToBytes()
    {
        byte[] data = new byte[Size];
        int offset = 0;
        Array.Copy(BitConverter.GetBytes(ReservedFlags), 0, data, offset, 4); offset += 4;
        Array.Copy(BitConverter.GetBytes(LiveFlags), 0, data, offset, 4); offset += 4;
        // Gamertag: convert to UTF-16LE, then swap to BE
        byte[] gtBytes = new byte[32];
        Encoding.Unicode.GetBytes(Gamertag.PadRight(16, '\0'), 0, 16, gtBytes, 0);
        // Swap every pair for BE
        for (int i = 0; i < 32; i += 2)
        {
            byte tmp = gtBytes[i];
            gtBytes[i] = gtBytes[i + 1];
            gtBytes[i + 1] = tmp;
        }
        Array.Copy(gtBytes, 0, data, offset, 32); offset += 32;
        Array.Copy(BitConverter.GetBytes(Xuid), 0, data, offset, 8); offset += 8;
        Array.Copy(BitConverter.GetBytes(CachedUserFlags), 0, data, offset, 4); offset += 4;
        byte[] spBytes = Encoding.ASCII.GetBytes(ServiceProvider.PadRight(4, '\0'));
        Array.Copy(spBytes, 0, data, offset, 4); offset += 4;
        Array.Copy(PasscodeKeys, 0, data, offset, 4); offset += 4;
        byte[] odBytes = Encoding.ASCII.GetBytes(OnlineDomain.PadRight(20, '\0'));
        Array.Copy(odBytes, 0, data, offset, 20); offset += 20;
        byte[] okrBytes = Encoding.ASCII.GetBytes(OnlineKerberosRealm.PadRight(24, '\0'));
        Array.Copy(okrBytes, 0, data, offset, 24); offset += 24;
        Array.Copy(OnlineKey, 0, data, offset, 16); offset += 16;
        byte[] upmBytes = Encoding.ASCII.GetBytes(UserPassportMembername.PadRight(114, '\0'));
        Array.Copy(upmBytes, 0, data, offset, 114); offset += 114;
        byte[] uppBytes = Encoding.ASCII.GetBytes(UserPassportPassword.PadRight(32, '\0'));
        Array.Copy(uppBytes, 0, data, offset, 32); offset += 32;
        byte[] opmBytes = Encoding.ASCII.GetBytes(OwnerPassportMembername.PadRight(114, '\0'));
        Array.Copy(opmBytes, 0, data, offset, 114); offset += 114;
        return data;
    }
    #endregion
}