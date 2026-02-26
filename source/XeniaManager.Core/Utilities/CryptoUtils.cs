using System.Security.Cryptography;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Provides cryptographic utilities for various encryption and hashing operations.
/// Includes implementations for HMAC-SHA1 and RC4 algorithms used in the Xenia emulator context.
/// </summary>
public class CryptoUtils
{
    /// <summary>
    /// The retail key used for cryptographic operations in retail mode.
    /// </summary>
    public static readonly byte[] RetailKey = { 0xE1, 0xBC, 0x15, 0x9C, 0x73, 0xB1, 0xEA, 0xE9, 0xAB, 0x31, 0x70, 0xF3, 0xAD, 0x47, 0xEB, 0xF3 };

    /// <summary>
    /// The development kit key used for cryptographic operations in development mode.
    /// </summary>
    public static readonly byte[] DevkitKey = { 0xDA, 0xB6, 0x9A, 0xD9, 0x8E, 0x28, 0x76, 0x4F, 0x97, 0x7E, 0xE2, 0x48, 0x7E, 0x4F, 0x3F, 0x68 };

    /// <summary>
    /// Gets the appropriate cryptographic key based on the device type.
    /// </summary>
    /// <param name="devkit">True if the development kit key should be returned, false for retail key.</param>
    /// <returns>The appropriate key based on the device type.</returns>
    public static byte[] GetKey(bool devkit)
    {
        Logger.Debug<CryptoUtils>($"Getting key for {(devkit ? "devkit" : "retail")} mode");
        byte[] result = devkit ? DevkitKey : RetailKey;
        Logger.Debug<CryptoUtils>($"Returned key with length: {result.Length}");
        return result;
    }

    /// <summary>
    /// Computes an HMAC-SHA1 hash of the provided data with the specified key.
    /// The output length can be less than the full 20-byte hash.
    /// </summary>
    /// <param name="key">The key to use for the HMAC operation.</param>
    /// <param name="data">The data to hash.</param>
    /// <param name="outputLen">The desired length of the output hash (default is 16 bytes).</param>
    /// <returns>The computed HMAC-SHA1 hash with the specified output length.</returns>
    public static byte[] HmacSha1(byte[] key, byte[] data, int outputLen = 16)
    {
        Logger.Debug<CryptoUtils>($"Computing HMAC-SHA1 with key length: {key.Length}, data length: {data.Length}, output length: {outputLen}");

        using HMACSHA1 hmac = new HMACSHA1(key);
        byte[] hash = hmac.ComputeHash(data);
        Logger.Trace<CryptoUtils>($"Computed full hash length: {hash.Length}");

        if (outputLen < hash.Length)
        {
            byte[] outHash = new byte[outputLen];
            Array.Copy(hash, outHash, outputLen);
            Logger.Trace<CryptoUtils>($"Truncated hash to {outputLen} bytes");
            return outHash;
        }
        Logger.Debug<CryptoUtils>($"Returning full hash of {hash.Length} bytes");
        return hash;
    }

    /// <summary>
    /// Computes an HMAC-SHA1 hash of up to three input buffers with the specified key.
    /// The output length can be less than the full 20-byte hash.
    /// </summary>
    /// <param name="key">The key to use for the HMAC operation.</param>
    /// <param name="inp1">The first input buffer.</param>
    /// <param name="inp2">The second input buffer (optional).</param>
    /// <param name="inp3">The third input buffer (optional).</param>
    /// <param name="outputLen">The desired length of the output hash (default is 16 bytes).</param>
    /// <returns>The computed HMAC-SHA1 hash with the specified output length.</returns>
    public static byte[] HmacSha1(byte[] key, byte[] inp1, byte[]? inp2 = null, byte[]? inp3 = null, int outputLen = 16)
    {
        Logger.Debug<CryptoUtils>($"Computing HMAC-SHA1 with key length: {key.Length}, inp1 length: {inp1.Length}, inp2 length: {(inp2?.Length ?? 0)}, inp3 length: {(inp3?.Length ?? 0)}, output length: {outputLen}");

        using HMACSHA1 hmac = new HMACSHA1(key);
        hmac.TransformBlock(inp1, 0, inp1.Length, inp1, 0);
        Logger.Trace<CryptoUtils>($"Processed first input buffer of {inp1.Length} bytes");

        if (inp2 is { Length: > 0 })
        {
            hmac.TransformBlock(inp2, 0, inp2.Length, inp2, 0);
            Logger.Trace<CryptoUtils>($"Processed second input buffer of {inp2.Length} bytes");
        }
        if (inp3 is { Length: > 0 })
        {
            hmac.TransformBlock(inp3, 0, inp3.Length, inp3, 0);
            Logger.Trace<CryptoUtils>($"Processed third input buffer of {inp3.Length} bytes");
        }
        hmac.TransformFinalBlock([], 0, 0);
        byte[]? hash = hmac.Hash;
        if (hash == null)
        {
            Logger.Error<CryptoUtils>($"Hash computation failed, hash is null");
            throw new InvalidOperationException("Hash computation failed.");
        }
        Logger.Trace<CryptoUtils>($"Computed full hash length: {hash.Length}");

        if (outputLen < hash.Length)
        {
            byte[] outHash = new byte[outputLen];
            Array.Copy(hash, outHash, outputLen);
            Logger.Trace<CryptoUtils>($"Truncated hash to {outputLen} bytes");
            return outHash;
        }
        Logger.Debug<CryptoUtils>($"Returning full hash of {hash.Length} bytes");
        return hash;
    }

    /// <summary>
    /// Performs RC4 encryption/decryption on the provided data using the specified key.
    /// </summary>
    /// <param name="key">The key to use for the RC4 algorithm.</param>
    /// <param name="data">The input data to encrypt or decrypt.</param>
    /// <param name="dataOffset">The offset in the input data to start processing.</param>
    /// <param name="dataLen">The number of bytes to process.</param>
    /// <param name="output">The output buffer to store the result.</param>
    public static void RC4(byte[] key, byte[] data, int dataOffset, int dataLen, byte[] output)
    {
        Logger.Debug<CryptoUtils>($"Performing RC4 operation with key length: {key.Length}, data offset: {dataOffset}, data length: {dataLen}, output length: {output.Length}");
        byte[] s = new byte[256];
        for (int i = 0; i < 256; i++) s[i] = (byte)i;
        int j = 0;
        for (int i = 0; i < 256; i++)
        {
            j = (j + s[i] + key[i % key.Length]) & 0xFF;
            (s[i], s[j]) = (s[j], s[i]);
        }
        Logger.Trace<CryptoUtils>("RC4 key scheduling completed");

        int i1 = 0, j1 = 0;
        for (int k = 0; k < dataLen; k++)
        {
            i1 = (i1 + 1) & 0xFF;
            j1 = (j1 + s[i1]) & 0xFF;
            (s[i1], s[j1]) = (s[j1], s[i1]);
            byte b = s[(s[i1] + s[j1]) & 0xFF];
            output[k] = (byte)(data[dataOffset + k] ^ b);
        }
        Logger.Debug<CryptoUtils>($"RC4 operation completed, processed {dataLen} bytes");
    }
}