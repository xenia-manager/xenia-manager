using System.Security.Cryptography;
using System.Text;

namespace XeniaManager.Core;

/// <summary>
/// Used to store logged in gamer profiles when running the game (Useful for backing up saves)
/// </summary>
public class GamerProfile
{
    /// <summary>
    /// XUID of the profile
    /// </summary>
    public string? Xuid { get; set; }

    /// <summary>
    /// Gamertag of the profile
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Slot where the profile is loaded
    /// </summary>
    public string? Slot { get; set; }

    /// <summary>
    /// Override so when it's converted to string it will display the gamertag
    /// </summary>
    public override string ToString()
    {
        if (Name != null)
        {
            return $"{Name} ({Xuid})";
        }
        else
        {
            return Xuid;
        }
    }
}

public static class XboxProfileManager
{
    private const int XE_KEY_MIN_LENGTH = 0x10;
    private const int RC4_KEY_LENGTH = 0x14;
    private const int HMAC_BLOCK_SIZE = 0x40;
    private const int SBOX_SIZE = 0x100;
    private const int ACCOUNT_HEADER_SIZE = 0x10;
    private const int DECRYPTED_DATA_SIZE = 388;
    private const int GAMERTAG_OFFSET = 16;
    private const int GAMERTAG_BUFFER_SIZE = 30;

    // Xbox encryption keys
    private static readonly byte[] RetailKey = { 0xE1, 0xBC, 0x15, 0x9C, 0x73, 0xB1, 0xEA, 0xE9, 0xAB, 0x31, 0x70, 0xF3, 0xAD, 0x47, 0xEB, 0xF3 };
    private static readonly byte[] DevkitKey = { 0xDA, 0xB6, 0x9A, 0xD9, 0x8E, 0x28, 0x76, 0x4F, 0x97, 0x7E, 0xE2, 0x48, 0x7E, 0x4F, 0x3F, 0x68 };

    /// <summary>
    /// Creates RC4Key used for decrypting "Account" file using HMAC-SHA1
    /// </summary>
    /// <param name="xeKey">Key for creating the RC4Key.</param>
    /// <param name="data">Account file as a byte array.</param>
    /// <param name="dataSize">Size of the account file.</param>
    /// <param name="rc4Key">RC4Key used for decryption</param>
    /// <seealso href="https://github.com/xenia-canary/xenia-canary/blob/canary_experimental/src/xenia/kernel/util/crypto_utils.cc">Link to the source code for further reference</seealso>
    public static void ComputeHmacSha(ReadOnlySpan<byte> xeKey, ReadOnlySpan<byte> data, Span<byte> rc4Key)
    {
        if (xeKey.Length < XE_KEY_MIN_LENGTH)
        {
            throw new ArgumentException($"XeKey must be at least {XE_KEY_MIN_LENGTH} bytes long.");
        }

        if (rc4Key.Length < RC4_KEY_LENGTH)
        {
            throw new ArgumentException($"RC4 key buffer must be at least {RC4_KEY_LENGTH} bytes long.");
        }

        using SHA1 sha = SHA1.Create();

        // Prepare key for HMAC (truncate or pad to 64 bytes)
        Span<byte> key = stackalloc byte[HMAC_BLOCK_SIZE];
        if (xeKey.Length > HMAC_BLOCK_SIZE)
        {
            // Hash the key if it's longer than block size
            sha.TryComputeHash(xeKey[..XE_KEY_MIN_LENGTH], key, out _);
        }
        else
        {
            // Copy and pad with zeros
            xeKey.CopyTo(key);
            key[xeKey.Length..].Clear();
        }

        // Prepare padded keys
        Span<byte> innerKey = stackalloc byte[HMAC_BLOCK_SIZE];
        Span<byte> outerKey = stackalloc byte[HMAC_BLOCK_SIZE];

        for (int i = 0; i < HMAC_BLOCK_SIZE; i++)
        {
            innerKey[i] = (byte)(key[i] ^ 0x36);
            outerKey[i] = (byte)(key[i] ^ 0x5C);
        }

        // Compute inner hash
        sha.TransformBlock(innerKey.ToArray(), 0, innerKey.Length, null, 0);
        sha.TransformFinalBlock(data.ToArray(), 0, data.Length);
        var innerHash = sha.Hash.AsSpan();

        // Compute outer hash
        sha.Initialize();
        sha.TransformBlock(outerKey.ToArray(), 0, outerKey.Length, null, 0);
        sha.TransformFinalBlock(innerHash.ToArray(), 0, innerHash.Length);

        // Copy result to output buffer
        Span<byte> finalHash = sha.Hash.AsSpan();
        finalHash[..Math.Min(RC4_KEY_LENGTH, finalHash.Length)].CopyTo(rc4Key);
    }

    /// <summary>
    /// Decrypts data using RC4 algorithm
    /// </summary>
    /// <param name="rc4Key">RC4Key used for decryption.</param>
    /// <param name="input">Input data to decrypt.</param>
    /// <param name="output">Output buffer for decrypted data.</param>
    /// <seealso href="https://github.com/xenia-canary/xenia-canary/blob/canary_experimental/src/xenia/kernel/util/crypto_utils.cc">Link to the source code for further reference</seealso>
    public static void RC4Decrypt(ReadOnlySpan<byte> rc4Key, ReadOnlySpan<byte> input, Span<byte> output)
    {
        if (rc4Key.Length < XE_KEY_MIN_LENGTH)
        {
            throw new ArgumentException($"RC4 key must be at least {XE_KEY_MIN_LENGTH} bytes long.");
        }

        if (output.Length < input.Length)
        {
            throw new ArgumentException("Output buffer is too small for input data.");
        }

        // Initialize S-box
        Span<byte> sbox = stackalloc byte[SBOX_SIZE];
        for (int i = 0; i < SBOX_SIZE; i++)
        {
            sbox[i] = (byte)i;
        }

        // Key-scheduling algorithm (KSA)
        int j = 0;
        for (int i = 0; i < SBOX_SIZE; i++)
        {
            j = (j + sbox[i] + rc4Key[i % XE_KEY_MIN_LENGTH]) % SBOX_SIZE;
            (sbox[i], sbox[j]) = (sbox[j], sbox[i]);
        }

        // Pseudo-random generation algorithm (PRGA)
        int x = 0, y = 0;
        int length = Math.Min(input.Length, output.Length);

        for (int index = 0; index < length; index++)
        {
            x = (x + 1) % SBOX_SIZE;
            y = (y + sbox[x]) % SBOX_SIZE;
            (sbox[x], sbox[y]) = (sbox[y], sbox[x]);

            byte keyByte = sbox[(sbox[x] + sbox[y]) % SBOX_SIZE];
            output[index] = (byte)(input[index] ^ keyByte);
        }
    }

    /// <summary>
    /// Decrypts the Xbox 360 Account file to retrieve the gamertag and profile information.
    /// Uses RC4 encryption with HMAC-SHA1 for decryption and integrity verification.
    /// </summary>
    /// <param name="accountFile">The encrypted account file data.</param>
    /// <param name="profile">Reference to the GamerProfile object to populate.</param>
    /// <param name="devkit">Whether to use devkit encryption keys.</param>
    /// <returns>True if decryption succeeds and gamertag is extracted; false otherwise.</returns>
    /// <seealso cref="GamerProfile"/>
    /// <seealso href="https://github.com/xenia-canary/xenia-canary/blob/canary_experimental/src/xenia/kernel/xam/profile_manager.cc">Link to the source code for further reference</seealso>
    public static bool DecryptAccountFile(ReadOnlySpan<byte> accountFile, ref GamerProfile profile, bool devkit = false)
    {
        if (accountFile.Length < ACCOUNT_HEADER_SIZE)
        {
            Logger.Error("Account file is too small - invalid format");
            return false;
        }

        try
        {
            Span<byte> key = devkit ? DevkitKey.AsSpan() : RetailKey.AsSpan();
            ReadOnlySpan<byte> header = accountFile[..ACCOUNT_HEADER_SIZE];
            ReadOnlySpan<byte> encryptedData = accountFile[ACCOUNT_HEADER_SIZE..];

            // Generate RC4 decryption key
            Span<byte> rc4Key = stackalloc byte[RC4_KEY_LENGTH];
            ComputeHmacSha(key, header, rc4Key);

            // Decrypt the account data
            Span<byte> decryptedData = stackalloc byte[DECRYPTED_DATA_SIZE];
            int decryptSize = Math.Min(encryptedData.Length, DECRYPTED_DATA_SIZE);
            RC4Decrypt(rc4Key, encryptedData[..decryptSize], decryptedData[..decryptSize]);

            // Verify data integrity
            Span<byte> computedHash = stackalloc byte[RC4_KEY_LENGTH];
            ComputeHmacSha(key, decryptedData[..decryptSize], computedHash);

            if (!computedHash[..ACCOUNT_HEADER_SIZE].SequenceEqual(header))
            {
                Logger.Error("Data integrity check failed - computed hash doesn't match header");
                return false;
            }

            // Slice only gamertag bytes
            Span<byte> gamertagBytes = decryptedData.Slice(GAMERTAG_OFFSET, GAMERTAG_BUFFER_SIZE);
            Logger.Debug($"Gamertag bytes at offset {GAMERTAG_OFFSET}: {Convert.ToHexString(gamertagBytes.ToArray())}");

            // Convert from bytes to string (ASCII)
            profile.Name = Encoding.ASCII.GetString(gamertagBytes).TrimEnd('\0');

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to decrypt account file: {ex.Message}");
            Logger.Debug($"Full exception details: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Attempts to decrypt account file with both retail and devkit keys
    /// </summary>
    /// <param name="accountFile">The encrypted account file data.</param>
    /// <param name="profile">Reference to the GamerProfile object to populate.</param>
    /// <returns>True if decryption succeeds with either key; false otherwise.</returns>
    public static bool TryDecryptAccountFile(ReadOnlySpan<byte> accountFile, ref GamerProfile profile)
    {
        // Try the retail key first (most common)
        if (DecryptAccountFile(accountFile, ref profile, devkit: false))
        {
            return true;
        }

        // Fallback to the devkit key
        Logger.Info("Retail key failed, trying devkit key...");
        return DecryptAccountFile(accountFile, ref profile, devkit: true);
    }
}