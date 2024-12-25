using System.Security.Cryptography;

namespace XeniaManager
{
    public static partial class ProfileManager
    {
        /// <summary>
        /// Creates RC4Key used for decrypting "Account" file
        /// </summary>
        /// <param name="xeKey">Key for creating the RC4Key.</param>
        /// <param name="data">Account file as byte array.</param>
        /// <param name="dataSize">Size of the account file.</param>
        /// <param name="rc4Key">RC4Key used for decryption</param>
        /// <seealso href="https://github.com/xenia-canary/xenia-canary/blob/canary_experimental/src/xenia/kernel/util/crypto_utils.cc">Link to the source code for further reference</seealso>
        public static void ComputeHmacSha(byte[] xeKey, byte[] data, int dataSize, byte[] rc4Key)
        {
            // Checking if XeKey and RC4Key are the correct length
            if (xeKey.Length < 0x10 || rc4Key.Length < 0x14)
            {
                // Validate all conditions in one go to avoid repetition
                List<string> errors = new List<string>();
                if (xeKey.Length < 0x10)
                {
                    errors.Add("XeKey is not the correct length.");
                }
                if (rc4Key.Length < 0x14)
                {
                    errors.Add("RC4 key is not the correct length.");
                }
                
                if (errors.Any())
                {
                    throw new ArgumentException(string.Join(" ", errors));
                }
            }
            using SHA1 sha = SHA1.Create();

            byte[] tempKey = xeKey.Length > 0x40 ? sha.ComputeHash(xeKey, 0, 0x10) : xeKey;
            tempKey = tempKey.Length > 0x40 ? tempKey.Take(0x40).ToArray() : tempKey.Concat(new byte[0x40 - tempKey.Length]).ToArray();
    
            // Inner padding
            byte[] kpadI = tempKey.Select(b => (byte)(b ^ 0x36)).Concat(new byte[0x40 - tempKey.Length]).ToArray();
            // Outer padding
            byte[] kpadO = tempKey.Select(b => (byte)(b ^ 0x5C)).Concat(new byte[0x40 - tempKey.Length]).ToArray();

            // Compute HMAC
            // Inner padding
            sha.TransformBlock(kpadI, 0, kpadI.Length, null, 0);
            sha.TransformFinalBlock(data, 0, dataSize);
            byte[] innerHash = sha.Hash;

            // Outer padding
            sha.Initialize();
            sha.TransformBlock(kpadO, 0, kpadO.Length, null, 0);
            sha.TransformFinalBlock(innerHash, 0, innerHash.Length);
    
            // Copying the result of Hash into RC4Key
            Array.Copy(sha.Hash, 0, rc4Key, 0, Math.Min(0x14, sha.Hash.Length));
        }
        
        /// <summary>
        /// Decrypts the data
        /// </summary>
        /// <param name="rc4Key">RC4Key used for decryption.</param>
        /// <param name="data">Account file as byte array.</param>
        /// <param name="dataSize">Size of the account file.</param>
        /// <param name="output">Decrypted account file</param>
        /// <param name="outSize">Size of the decrypted data</param>
        /// <seealso href="https://github.com/xenia-canary/xenia-canary/blob/canary_experimental/src/xenia/kernel/util/crypto_utils.cc">Link to the source code for further reference</seealso>
        public static void RC4Decrypt(byte[] rc4Key, byte[] data, uint dataSize, byte[] output, uint outSize)
        {
            if (rc4Key.Length < 0x10 || data.Length < dataSize || output.Length < outSize)
            {
                List<string> errors = new List<string>();

                if (rc4Key.Length < 0x10)
                {
                    errors.Add("RC4 key is not the correct length.");
                }

                if (data.Length < dataSize)
                {
                    errors.Add("Input data isn't the correct size.");
                }

                if (output.Length < outSize)
                {
                    errors.Add("Output isn't big enough for decryption.");
                }

                // If there are any errors, throw an aggregate exception
                if (errors.Any())
                {
                    throw new ArgumentException(string.Join(" ", errors));
                }
            }
    
            // Temp var of RC4Key
            byte[] tempKey = new byte[0x10];
            Array.Copy(rc4Key, tempKey, Math.Min(rc4Key.Length, tempKey.Length));
    
            byte[] sbox = new byte[0x100];
            for (uint x = 0; x < 0x100; x++)
            {
                sbox[x] = (byte)x;
            }

            uint idx1 = 0;
            for (uint x = 0; x < 0x100; x++)
            {
                idx1 = (idx1 + sbox[x] + tempKey[x % 0x10]) % 0x100;
                (sbox[x], sbox[idx1]) = (sbox[idx1], sbox[x]);
            }
    
            // Crypt data
            uint i = 0, j = 0;
            uint length = Math.Min(dataSize, outSize);
            for (uint index = 0; index < length; index++)
            {
                i = (i + 1) % 0x100;
                j = (j + sbox[i]) % 0x100;
                (sbox[i], sbox[j]) = (sbox[j], sbox[i]);

                byte a = data[index];
                byte b = sbox[(sbox[i] + sbox[j]) % 0x100];
                output[index] = (byte)(a ^ b);
            }
        }
    }
}