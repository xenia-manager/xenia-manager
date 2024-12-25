using Serilog;

namespace XeniaManager
{
    public static partial class ProfileManager
    {
        /// <summary>
        /// Used to decrypt the `Account` File
        /// </summary>
        /// Ref -> https://github.com/xenia-canary/xenia-canary/blob/canary_experimental/src/xenia/kernel/xam/profile_manager.cc
        /// <returns></returns>
        public static bool DecryptAccountFile(byte[] accountFile, ref GamerProfile profile, bool devkit = false)
        {
            // Checking if the account file is valid
            if (accountFile.Length < 0x10)
            {
                Log.Error("Invalid account file");
                return false;
            }
            
            try
            {
                // XeKey
                byte[] key = devkit
                    ? new byte[] { 0xDA, 0xB6, 0x9A, 0xD9, 0x8E, 0x28, 0x76, 0x4F, 0x97, 0x7E, 0xE2, 0x48, 0x7E, 0x4F, 0x3F, 0x68 }
                    : new byte[] { 0xE1, 0xBC, 0x15, 0x9C, 0x73, 0xB1, 0xEA, 0xE9, 0xAB, 0x31, 0x70, 0xF3, 0xAD, 0x47, 0xEB, 0xF3 };

                // Generating RC4 Key using HMAC-SHA1
                byte[] rc4Key = new byte[0x14];
                ComputeHmacSha(key, accountFile, 0x10, rc4Key);
                
                // Decrypting Account File
                byte[] decryptedData = new byte[388];
                RC4Decrypt(rc4Key, accountFile.Skip(0x10).ToArray(), (uint)(accountFile.Length - 0x10), decryptedData, (uint)decryptedData.Length);
                
                // Verifying decrypted data with hash
                byte[] dataHash = new byte[0x14];
                ComputeHmacSha(key, decryptedData, decryptedData.Length, dataHash);
                
                // If different, return false aka try with devkit true, otherwise return decrypted gamertag
                if (!dataHash.Take(0x10).SequenceEqual(accountFile.Take(0x10)))
                {
                    Log.Error("Decrypted data doesn't match with Hash");
                    return false;
                }
                // Converting decrypted data into gamertag
                char[] gamertag = decryptedData.Skip(16).Take(30).Select(b => (char)b).ToArray(); // Skipping first 16 bytes and taking 30 because 15 chars is the limit
                profile.Name = new string(gamertag).TrimEnd('\0');
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                return false;
            }
        }
    }
}