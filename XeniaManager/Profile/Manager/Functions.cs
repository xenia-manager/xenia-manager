using Serilog;

namespace XeniaManager
{
    public static partial class ProfileManager
    {
        /// <summary>
        /// Decrypts the `Account` file to retrieve the gamertag and other profile information.
        /// This method uses RC4 encryption with HMAC-SHA1 to decrypt the account file and extract the gamertag.
        /// The decryption process is dependent on whether a devkit is being used, with different keys being utilized.
        /// </summary>
        /// <param name="accountFile">The encrypted account file (byte array) that needs to be decrypted.</param>
        /// <param name="profile">A reference to the <see cref="GamerProfile"/> object, where the decrypted gamertag will be stored.</param>
        /// <param name="devkit">A flag indicating whether a devkit is being used. If true, a different decryption key is used.</param>
        /// <returns>
        /// Returns <c>false</c> if the decryption fails at any point, or if the decrypted data does not match the expected hash.
        /// Returns <c>true</c> if the decryption succeeds and the gamertag is extracted and assigned to the <see cref="profile"/> object.
        /// </returns>
        /// <remarks>
        /// The decryption process involves the following steps:
        /// 1. Verify that the account file has the expected minimum length.
        /// 2. Generate the RC4 decryption key using HMAC-SHA1 based on the provided account file and key.
        /// 3. Decrypt the account file's data using the RC4 key.
        /// 4. Verify the integrity of the decrypted data by comparing it with a hash computed from the decrypted data.
        /// 5. Extract the gamertag (up to 15 characters) from the decrypted data and assign it to the <see cref="profile"/> object.
        /// </remarks>
        /// <seealso cref="GamerProfile"/>
        /// <seealso href="https://github.com/xenia-canary/xenia-canary/blob/canary_experimental/src/xenia/kernel/xam/profile_manager.cc">Link to the source code for further reference</seealso>
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