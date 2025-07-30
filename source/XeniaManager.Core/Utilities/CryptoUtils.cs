using System.Security.Cryptography;

namespace XeniaManager.Core.Utilities;
public static class CryptoUtils
{
    public static readonly byte[] RetailKey = { 0xE1, 0xBC, 0x15, 0x9C, 0x73, 0xB1, 0xEA, 0xE9, 0xAB, 0x31, 0x70, 0xF3, 0xAD, 0x47, 0xEB, 0xF3 };
    public static readonly byte[] DevkitKey = { 0xDA, 0xB6, 0x9A, 0xD9, 0x8E, 0x28, 0x76, 0x4F, 0x97, 0x7E, 0xE2, 0x48, 0x7E, 0x4F, 0x3F, 0x68 };

    public static byte[] GetKey(bool devkit) => devkit ? DevkitKey : RetailKey;

    // HMAC-SHA1, output length can be less than 20 bytes
    public static byte[] HmacSha1(byte[] key, byte[] data, int outputLen = 16)
    {
        using (var hmac = new HMACSHA1(key))
        {
            byte[] hash = hmac.ComputeHash(data);
            if (outputLen < hash.Length)
            {
                byte[] outHash = new byte[outputLen];
                Array.Copy(hash, outHash, outputLen);
                return outHash;
            }
            return hash;
        }
    }

    // HMAC-SHA1 with up to 3 input buffers
    public static byte[] HmacSha1(byte[] key, byte[] inp1, byte[]? inp2 = null, byte[]? inp3 = null, int outputLen = 16)
    {
        using (HMACSHA1 hmac = new HMACSHA1(key))
        {
            hmac.TransformBlock(inp1, 0, inp1.Length, inp1, 0);
            if (inp2 != null && inp2.Length > 0)
            {
                hmac.TransformBlock(inp2, 0, inp2.Length, inp2, 0);
            }
            if (inp3 != null && inp3.Length > 0)
            {
                hmac.TransformBlock(inp3, 0, inp3.Length, inp3, 0);
            }
            hmac.TransformFinalBlock(new byte[0], 0, 0);
            byte[]? hash = hmac.Hash;
            if (hash == null)
            {
                throw new InvalidOperationException("Hash computation failed.");
            }
            if (outputLen < hash.Length)
            {
                byte[] outHash = new byte[outputLen];
                Array.Copy(hash, outHash, outputLen);
                return outHash;
            }
            return hash;
        }
    }

    // RC4 implementation
    public static void RC4(byte[] key, byte[] data, int dataOffset, int dataLen, byte[] output)
    {
        byte[] s = new byte[256];
        for (int i = 0; i < 256; i++) s[i] = (byte)i;
        int j = 0;
        for (int i = 0; i < 256; i++)
        {
            j = (j + s[i] + key[i % key.Length]) & 0xFF;
            (s[i], s[j]) = (s[j], s[i]);
        }
        int i1 = 0, j1 = 0;
        for (int k = 0; k < dataLen; k++)
        {
            i1 = (i1 + 1) & 0xFF;
            j1 = (j1 + s[i1]) & 0xFF;
            (s[i1], s[j1]) = (s[j1], s[i1]);
            byte b = s[(s[i1] + s[j1]) & 0xFF];
            output[k] = (byte)(data[dataOffset + k] ^ b);
        }
    }
}