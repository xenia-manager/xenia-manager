using System.Security.Cryptography;

// Imported Libraries
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Profile;
public static class ProfileFile
{
    #region Constants
    public const int ConfounderLen = 8;
    public const int HmacLen = 16;
    public const int AccountDataLen = 380; // ProfileInfo.Size
    public const int TotalPayloadLen = ConfounderLen + AccountDataLen; // 388
    #endregion

    #region Functions
    /// <summary>
    /// Decrypts the profile file
    /// </summary>
    /// <param name="file">Profile file as byte array</param>
    /// <param name="devkit">Check if this profile file is from devkit</param>
    /// <returns>ProfileInfo, else null</returns>
    /// <exception cref="ArgumentException">If the provided profile file is too short</exception>
    public static ProfileInfo? Decrypt(byte[] file, bool devkit)
    {
        Logger.Debug("Decrypt: Starting decryption. File length: {Length}, Devkit: {Devkit}", file.Length, devkit);
        if (file.Length < HmacLen + TotalPayloadLen)
        {
            Logger.Error("Decrypt: File too short. Length: {Length}", file.Length);
            throw new ArgumentException("File too short");
        }

        byte[] key = CryptoUtils.GetKey(devkit);
        Logger.Debug("Decrypt: Key: {Key}", BitConverter.ToString(key));

        // 1. Get file HMAC (first 16 bytes)
        byte[] fileHmac = file.Take(HmacLen).ToArray();
        Logger.Debug("Decrypt: File HMAC: {Hmac}", BitConverter.ToString(fileHmac));

        // 2. Generate RC4 key from file HMAC
        byte[] rc4Key = CryptoUtils.HmacSha1(key, fileHmac, outputLen: 16);
        Logger.Debug("Decrypt: RC4 Key: {RC4Key}", BitConverter.ToString(rc4Key));

        // 3. Decrypt confounder + account data (388 bytes at offset 0x10)
        byte[] encryptedPayload = file.Skip(HmacLen).Take(TotalPayloadLen).ToArray();
        Logger.Debug("Decrypt: Encrypted Payload: {Payload}", BitConverter.ToString(encryptedPayload));

        byte[] decryptedPayload = new byte[TotalPayloadLen];
        CryptoUtils.RC4(rc4Key, encryptedPayload, 0, TotalPayloadLen, decryptedPayload);
        Logger.Debug("Decrypt: Decrypted Payload: {Payload}", BitConverter.ToString(decryptedPayload));

        // 4. Verify HMAC
        byte[] verifyHmac = CryptoUtils.HmacSha1(key, decryptedPayload, outputLen: 16);
        Logger.Debug("Decrypt: Verify HMAC: {VerifyHmac}", BitConverter.ToString(verifyHmac));

        if (!fileHmac.SequenceEqual(verifyHmac))
        {
            Logger.Warning("Decrypt: HMAC verification failed. File HMAC: {FileHmac}, Computed HMAC: {VerifyHmac}", BitConverter.ToString(fileHmac), BitConverter.ToString(verifyHmac));
            return null; // Invalid
        }

        // 5. Parse account data (after confounder)
        Console.WriteLine(BitConverter.ToString(decryptedPayload));
        byte[] accountData = decryptedPayload.Skip(ConfounderLen).Take(AccountDataLen).ToArray();
        Logger.Debug("Decrypt: Account Data: {AccountData}", BitConverter.ToString(accountData));
        ProfileInfo profile = ProfileInfo.FromBytes(accountData);
        Logger.Debug($"Decrypt: ProfileInfo parsed: {profile.Gamertag} ({profile.OnlineXuid})");

        return profile;
    }

    /// <summary>
    /// Encrypts the account file
    /// </summary>
    /// <param name="info">Loaded Profile</param>
    /// <param name="devkit">Check if this profile is from devkit</param>
    /// <returns>Encrypted byte array</returns>
    public static byte[] Encrypt(ProfileInfo info, bool devkit)
    {
        Logger.Debug("Encrypt: Starting encryption. Devkit: {Devkit}", devkit);

        byte[] key = CryptoUtils.GetKey(devkit);
        Logger.Debug("Encrypt: Key: {Key}", BitConverter.ToString(key));

        // 1. Prepare decrypted payload: confounder + account data
        byte[] confounder = new byte[ConfounderLen];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(confounder);
        }
        Logger.Debug("Encrypt: Confounder: {Confounder}", BitConverter.ToString(confounder));

        byte[] accountData = info.ToBytes();
        Logger.Debug("Encrypt: Account Data: {AccountData}", BitConverter.ToString(accountData));

        byte[] payload = new byte[TotalPayloadLen];
        Array.Copy(confounder, 0, payload, 0, ConfounderLen);
        Array.Copy(accountData, 0, payload, ConfounderLen, AccountDataLen);
        Logger.Debug("Encrypt: Payload (Confounder + AccountData): {Payload}", BitConverter.ToString(payload));

        // 2. HMAC of confounder + account data
        byte[] hmac = CryptoUtils.HmacSha1(key, payload, outputLen: 16);
        Logger.Debug("Encrypt: HMAC: {Hmac}", BitConverter.ToString(hmac));

        // 3. RC4 key from HMAC
        byte[] rc4Key = CryptoUtils.HmacSha1(key, hmac, outputLen: 16);
        Logger.Debug("Encrypt: RC4 Key: {RC4Key}", BitConverter.ToString(rc4Key));

        // 4. Encrypt confounder + account data
        byte[] encryptedPayload = new byte[TotalPayloadLen];
        CryptoUtils.RC4(rc4Key, payload, 0, TotalPayloadLen, encryptedPayload);
        Logger.Debug("Encrypt: Encrypted Payload: {EncryptedPayload}", BitConverter.ToString(encryptedPayload));

        // 5. Build file: HMAC + encrypted payload
        byte[] file = new byte[HmacLen + TotalPayloadLen];
        Array.Copy(hmac, 0, file, 0, HmacLen);
        Array.Copy(encryptedPayload, 0, file, HmacLen, TotalPayloadLen);
        Logger.Debug("Encrypt: Final File: {File}", BitConverter.ToString(file));

        return file;
    }
    #endregion
}