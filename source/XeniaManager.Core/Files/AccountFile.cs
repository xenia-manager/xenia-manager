using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Account;
using XeniaManager.Core.Utilities;
using XeniaManager.Core.Utilities.Paths;

namespace XeniaManager.Core.Files;

/// <summary>
/// Handles the loading, saving, encryption, and decryption of Xbox 360 account files used by the Xenia emulator.
/// Account files are encrypted with HMAC-RC4 and contain profile information such as gamertag, XUID, online keys, etc.
/// </summary>
public class AccountFile
{
    /// <summary>
    /// Length of the confounder used in the encryption process (8 bytes).
    /// </summary>
    public const int CONFOUNDER_LENGTH = 8;

    /// <summary>
    /// Length of the HMAC used for integrity verification (16 bytes).
    /// </summary>
    public const int HMAC_LENGTH = 16;

    /// <summary>
    /// Length of the account data payload (380 bytes).
    /// </summary>
    public const int ACCOUNT_DATA_LENGTH = 380; // ProfileInfo.Size

    /// <summary>
    /// Total length of the payload (confounder and account data) = 388 bytes.
    /// </summary>
    public const int TOTAL_PAYLOAD_LENGTH = CONFOUNDER_LENGTH + ACCOUNT_DATA_LENGTH; // 388

    /// <summary>
    /// Loads an account file from the specified path and decrypts it.
    /// Automatically tries retail keys first, then devkit keys if retail fails.
    /// </summary>
    /// <param name="filePath">The path to the account file to load.</param>
    /// <returns>The decrypted AccountInfo object.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the specified file does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when the file is too short or invalid.</exception>
    /// <exception cref="IOException">Thrown when HMAC verification fails for both retail and devkit modes.</exception>
    public static AccountInfo Load(string filePath)
    {
        Logger.Debug<AccountFile>($"Loading account file from {filePath}");

        if (!File.Exists(filePath))
        {
            Logger.Error<AccountFile>($"Account file does not exist at {filePath}");
            throw new FileNotFoundException($"Account file does not exist at {filePath}", filePath);
        }

        byte[] file = File.ReadAllBytes(filePath);

        // First, try to decrypt with retail keys
        try
        {
            Logger.Info<AccountFile>($"Attempting to decrypt account file using retail keys...");
            AccountInfo result = Decrypt(file, devkit: false);
            Logger.Info<AccountFile>($"Successfully decrypted account file using retail keys");
            return result;
        }
        catch (IOException)
        {
            Logger.Warning<AccountFile>("Retail key decryption failed, attempting devkit key decryption...");
            // If retail fails, try devkit keys
            try
            {
                AccountInfo result = Decrypt(file, devkit: true);
                Logger.Info<AccountFile>($"Successfully decrypted account file using devkit keys");
                return result;
            }
            catch (IOException ex)
            {
                Logger.Error<AccountFile>($"Both retail and devkit key decryption attempts failed");
                throw new IOException("Account file could not be decrypted with either retail or devkit keys", ex);
            }
        }
    }

    /// <summary>
    /// Saves an account info object to the specified file path with encryption.
    /// </summary>
    /// <param name="info">The account information to save.</param>
    /// <param name="savePath">The path where the encrypted account file will be saved.</param>
    /// <param name="devkit">Whether to use devkit mode for encryption (default: false for retail mode).</param>
    /// <exception cref="ArgumentNullException">Thrown when info is null.</exception>
    /// <exception cref="ArgumentException">Thrown when savePath is null or empty.</exception>
    public static void Save(AccountInfo info, string savePath, bool devkit = false)
    {
        Logger.Trace<AccountFile>($"Starting Save operation for account '{info.Gamertag}' to path: {savePath} (Mode: {(devkit ? "Devkit" : "Retail")})");

        if (string.IsNullOrEmpty(savePath))
        {
            Logger.Error<AccountFile>("Save path is null or empty");
            throw new ArgumentException("Save path cannot be null or empty", nameof(savePath));
        }

        try
        {
            byte[] encryptedFile = Encrypt(info, devkit);
            Logger.Info<AccountFile>($"Successfully encrypted account data using {(devkit ? "devkit" : "retail")} keys. File size: {encryptedFile.Length} bytes");
            Logger.Debug<AccountFile>($"Saving encrypted profile file to {savePath}");

            // Ensure the directory exists
            string? directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Logger.Info<AccountFile>($"Creating directory: {directory}");
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(savePath, encryptedFile);
            Logger.Info<AccountFile>($"Account file saved successfully to {savePath}");
        }
        catch (Exception ex)
        {
            Logger.Error<AccountFile>($"Failed to save account file to {savePath}: {ex.Message}");
            Logger.LogExceptionDetails<AccountFile>(ex);
            throw;
        }
    }

    /// <summary>
    /// Creates a new Xbox 360 account with the specified parameters and saves it to the appropriate location.
    /// </summary>
    /// <param name="version">The Xenia version for which to create the account.</param>
    /// <param name="gamertag">The gamertag to assign to the new account.</param>
    /// <param name="defaultXuid">Whether to use a default XUID or generate a random offline XUID (default: false).</param>
    /// <returns>The newly created AccountInfo object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when gamertag is null.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory for the account file cannot be found.</exception>
    public static AccountInfo CreateAccount(XeniaVersion version, string gamertag, bool defaultXuid = false)
    {
        // Create Account
        AccountInfo info = new AccountInfo
        {
            // Set the gamertag
            Gamertag = gamertag,
            // Set XUID - either generate a random offline XUID or use default based on a parameter
            Xuid = defaultXuid ? AccountXuid.CreateDefault() : AccountXuid.GenerateOfflineXuid(),
            // Set all other fields to default/disabled values (0s or empty)
            ReservedFlags = ReservedFlags.None,
            LiveFlags = 0,
            CachedUserFlags = 0,
            ServiceProvider = string.Empty,
            Passcode = [PasscodeButton.Null, PasscodeButton.Null, PasscodeButton.Null, PasscodeButton.Null],
            OnlineDomain = string.Empty,
            OnlineKerberosRealm = string.Empty,
            OnlineKey = new byte[16], // All zeros by default
            UserPassportMembername = string.Empty,
            UserPassportPassword = string.Empty,
            OwnerPassportMembername = string.Empty
        };
        
        Logger.Trace<AccountFile>($"Starting CreateAccount operation for gamertag: '{info.Gamertag}' (XUID: {info.Xuid})");
        Logger.Debug<AccountFile>($"Setting gamertag to: '{gamertag}'");
        Logger.Debug<AccountFile>($"Setting XUID - defaultXuid: {defaultXuid}");

        Logger.Debug<AccountFile>("Set default values for account properties");
        Logger.Info<AccountFile>($"Created account with gamertag: '{info.Gamertag}' and XUID: {info.Xuid.ToString()}");

        // Create the directory
        Logger.Debug<AccountFile>($"Retrieving Xenia version info for: {version}");
        XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);

        // TODO: Replace FFFE07D1 with an enum or constant
        // Content Folder + Xuid + FFFE07D1 (Dashboard) + 00010000 (Xbox 360 Title) + Xuid + Account
        Logger.Debug<AccountFile>($"Constructing account file path using version content folder: {versionInfo.ContentFolderLocation}");
        string accountFileLocation = Path.Combine(AppPathResolver.GetFullPath(versionInfo.ContentFolderLocation),
            info.Xuid.ToString(), "FFFE07D1", ContentType.Xbox360Title.ToHexString(), info.Xuid.ToString(), "Account");

        Logger.Debug<AccountFile>($"Account file will be saved to: {accountFileLocation}");

        string? directoryPath = Path.GetDirectoryName(accountFileLocation);
        if (string.IsNullOrEmpty(directoryPath))
        {
            Logger.Error<AccountFile>($"Couldn't find a directory for the account file {directoryPath}.");
            throw new DirectoryNotFoundException($"Couldn't find a directory for the account file {directoryPath}.");
        }

        Logger.Info<AccountFile>($"Creating directory structure for account file: {directoryPath}");
        Directory.CreateDirectory(directoryPath);

        Logger.Info<AccountFile>($"Saving new account file to: {accountFileLocation}");
        Save(info, accountFileLocation);

        Logger.Info<AccountFile>($"Successfully created and saved account for gamertag: '{info.Gamertag}' at {accountFileLocation}");
        Logger.Trace<AccountFile>("CreateAccount operation completed successfully");

        return info;
    }

    /// <summary>
    /// Counts the number of account profiles in the specified Xenia version's content folder.
    /// Profiles are identified by XUID-named directories that contain the account file structure.
    /// </summary>
    /// <param name="version">The Xenia version whose content folder will be scanned for profiles.</param>
    /// <returns>The number of account profiles found in the content folder.</returns>
    public static int CountProfiles(XeniaVersion version)
    {
        Logger.Trace<AccountFile>($"Starting CountProfiles operation for version: {version}");

        try
        {
            XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);
            string contentFolderPath = AppPathResolver.GetFullPath(versionInfo.ContentFolderLocation);

            Logger.Debug<AccountFile>($"Scanning content folder path: {contentFolderPath}");

            if (!Directory.Exists(contentFolderPath))
            {
                Logger.Warning<AccountFile>($"Content folder does not exist: {contentFolderPath}. Returning 0 profiles.");
                return 0;
            }

            // Get all subdirectories in the content folder
            // These should be XUID-named directories representing individual profiles
            string[] xuidDirectories = Directory.GetDirectories(contentFolderPath);

            Logger.Debug<AccountFile>($"Found {xuidDirectories.Length} potential XUID directories in content folder");

            int profileCount = 0;

            // Check each directory to see if it contains the expected account file structure
            foreach (string xuidDir in xuidDirectories)
            {
                string xuid = Path.GetFileName(xuidDir); // Get the directory name which should be the XUID

                // Validate that this looks like a proper XUID directory
                // A valid XUID should be a hex string of appropriate length
                if (AccountXuid.IsValidFormat(xuid))
                {
                    // Check if this directory follows the expected account file structure
                    // The structure should be: xuidDir/FFFE07D1/00010000/xuid/Account
                    string expectedAccountPath = Path.Combine(xuidDir, "FFFE07D1", ContentType.Xbox360Title.ToHexString(), xuid, "Account");

                    if (File.Exists(expectedAccountPath))
                    {
                        Logger.Debug<AccountFile>($"Valid profile found with XUID: {xuid}");
                        profileCount++;
                    }
                    else
                    {
                        Logger.Debug<AccountFile>($"Directory {xuid} exists but doesn't contain the expected account file structure at: {expectedAccountPath}");
                    }
                }
                else
                {
                    Logger.Debug<AccountFile>($"Directory {xuid} does not appear to be a valid XUID format, skipping.");
                }
            }

            Logger.Info<AccountFile>($"Found {profileCount} valid account profiles in content folder for version: {version}");
            Logger.Trace<AccountFile>("CountProfiles operation completed successfully");

            return profileCount;
        }
        catch (Exception ex)
        {
            Logger.Error<AccountFile>($"Error counting profiles for version {version}: {ex.Message}");
            Logger.LogExceptionDetails<AccountFile>(ex);
            // Return 0 in case of error, as we couldn't determine the actual count
            return 0;
        }
    }

    /// <summary>
    /// Decrypts an account file from raw byte data.
    /// </summary>
    /// <param name="file">The encrypted account files data as bytes.</param>
    /// <param name="devkit">Whether to use devkit mode for decryption (default: false for retail mode).</param>
    /// <returns>The decrypted AccountInfo object.</returns>
    /// <exception cref="ArgumentException">Thrown when the file is too short or invalid.</exception>
    /// <exception cref="IOException">Thrown when HMAC verification fails.</exception>
    private static AccountInfo Decrypt(byte[] file, bool devkit = false)
    {
        Logger.Trace<AccountFile>($"Starting decryption process (Mode: {(devkit ? "Devkit" : "Retail")})");
        Logger.Debug<AccountFile>($"Starting decryption. File length: {file.Length}, Devkit: {devkit}");

        if (file.Length < HMAC_LENGTH + TOTAL_PAYLOAD_LENGTH)
        {
            Logger.Error<AccountFile>($"File too short. Length: {file.Length}, Required minimum: {HMAC_LENGTH + TOTAL_PAYLOAD_LENGTH}");
            throw new ArgumentException($"File too short. Length: {file.Length}, Required minimum: {HMAC_LENGTH + TOTAL_PAYLOAD_LENGTH}");
        }

        try
        {
            // Grab the XeKey
            byte[] key = CryptoUtils.GetKey(devkit);
            Logger.Debug<AccountFile>($"Key: {BitConverter.ToString(key)}");

            // Get file HMAC (First 16 bytes)
            byte[] fileHmac = file.Take(HMAC_LENGTH).ToArray();
            Logger.Debug<AccountFile>($"File HMAC: {BitConverter.ToString(fileHmac)}");

            // Generate RC4 key from file HMAC
            byte[] rc4Key = CryptoUtils.HmacSha1(key, fileHmac, outputLen: 16);
            Logger.Debug<AccountFile>($"RC4 Key: {BitConverter.ToString(rc4Key)}");

            // Decrypt confounder + account data (388 bytes at offset 0x10)
            byte[] encryptedPayload = file.Skip(HMAC_LENGTH).Take(TOTAL_PAYLOAD_LENGTH).ToArray();
            Logger.Debug<AccountFile>($"Encrypted Payload: {BitConverter.ToString(encryptedPayload)}");
            byte[] decryptedPayload = new byte[TOTAL_PAYLOAD_LENGTH];
            CryptoUtils.RC4(rc4Key, encryptedPayload, 0, TOTAL_PAYLOAD_LENGTH, decryptedPayload);
            Logger.Debug<AccountFile>($"Decrypted Payload: {BitConverter.ToString(decryptedPayload)}");

            // Verify HMAC
            byte[] verifyHmac = CryptoUtils.HmacSha1(key, decryptedPayload, outputLen: 16);
            Logger.Debug<AccountFile>($"Verify HMAC: {BitConverter.ToString(verifyHmac)}");

            if (!fileHmac.SequenceEqual(verifyHmac))
            {
                Logger.Warning<AccountFile>($"HMAC verification failed. File HMAC: {BitConverter.ToString(fileHmac)}, Computed HMAC: {BitConverter.ToString(verifyHmac)}");
                Logger.Error<AccountFile>("Account file integrity verification failed - HMAC mismatch detected");

                // TODO: Replace with custom exception
                throw new IOException("HMAC verification failed.");
            }
            else
            {
                Logger.Info<AccountFile>("HMAC verification successful - account file integrity confirmed");
            }

            // Parse profile info
            byte[] accountData = decryptedPayload.Skip(CONFOUNDER_LENGTH).Take(ACCOUNT_DATA_LENGTH).ToArray();
            Logger.Debug<AccountFile>($"Account Data: {BitConverter.ToString(accountData)}");
            AccountInfo profile = ParseFromBytes(accountData);
            Logger.Debug<AccountFile>($"Account Info: {profile.Gamertag} ({profile.Xuid.ToString()})");
            Logger.Trace<AccountFile>("Decryption process completed successfully");

            return profile;
        }
        catch (Exception ex)
        {
            Logger.Error<AccountFile>($"Decryption failed: {ex.Message}");
            Logger.LogExceptionDetails<AccountFile>(ex);
            throw;
        }
    }

    /// <summary>
    /// Parses account information from raw byte data.
    /// </summary>
    /// <param name="data">The raw byte data containing account information.</param>
    /// <returns>An AccountInfo object with parsed data.</returns>
    /// <exception cref="ArgumentException">Thrown when the data is too short.</exception>
    private static AccountInfo ParseFromBytes(byte[] data)
    {
        Logger.Trace<AccountFile>($"Starting ParseFromBytes with {data.Length} bytes of data");

        if (data.Length < AccountInfo.DataSize)
        {
            Logger.Error<AccountFile>($"Data too short: expected {AccountInfo.DataSize} bytes, got {data.Length}");
            throw new ArgumentException($"Data too short: expected {AccountInfo.DataSize} bytes, got {data.Length}");
        }

        Logger.Debug<AccountFile>($"Beginning to parse account data. Expected size: {AccountInfo.DataSize} bytes");
        AccountInfo info = new AccountInfo();
        int offset = 0;

        // 0x00 - ReservedFlags (4 bytes)
        Logger.Trace<AccountFile>($"Parsing ReservedFlags at offset {offset:X2} (4 bytes)");
        info.ReservedFlags = (ReservedFlags)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset));
        Logger.Debug<AccountFile>($"Parsed ReservedFlags: {info.ReservedFlags} (Value: 0x{((uint)info.ReservedFlags):X8})");
        offset += 4;

        // 0x04 - LiveFlags (4 bytes)
        Logger.Trace<AccountFile>($"Parsing LiveFlags at offset {offset:X2} (4 bytes)");
        info.LiveFlags = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset));
        Logger.Debug<AccountFile>($"Parsed LiveFlags: {info.LiveFlags} (Value: 0x{info.LiveFlags:X8})");
        offset += 4;

        // 0x08 - Gamertag (32 bytes, UTF-16 BE → swap to LE for decoding)
        Logger.Trace<AccountFile>($"Parsing Gamertag at offset {offset:X2} (32 bytes, UTF-16 BE)");
        info.Gamertag = Encoding.BigEndianUnicode.GetString(data, offset, 32).TrimEnd('\0');
        Logger.Info<AccountFile>($"Parsed Gamertag: '{info.Gamertag}'");
        offset += 32;

        // 0x28 - XUID (8 bytes)
        Logger.Trace<AccountFile>($"Parsing XUID at offset {offset:X2} (8 bytes)");
        info.Xuid = new AccountXuid(BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset)));
        Logger.Info<AccountFile>($"Parsed XUID: {info.Xuid} (IsOnline: {info.Xuid.IsOnline}, IsOffline: {info.Xuid.IsOffline})");
        offset += 8;

        // 0x30 - CachedUserFlags (4 bytes)
        Logger.Trace<AccountFile>($"Parsing CachedUserFlags at offset {offset:X2} (4 bytes)");
        info.CachedUserFlags = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset));
        Logger.Debug<AccountFile>($"Parsed CachedUserFlags: {info.CachedUserFlags} (Value: 0x{info.CachedUserFlags:X8})");
        offset += 4;

        // 0x34 - ServiceProvider (4 bytes ASCII)
        Logger.Trace<AccountFile>($"Parsing ServiceProvider at offset {offset:X2} (4 bytes ASCII)");
        info.ServiceProvider = Encoding.ASCII.GetString(data, offset, 4).TrimEnd('\0');
        Logger.Info<AccountFile>($"Parsed ServiceProvider: '{info.ServiceProvider}'");
        offset += 4;

        // 0x38 - Passcode (4 bytes)
        Logger.Trace<AccountFile>($"Parsing Passcode at offset {offset:X2} (4 bytes)");
        for (int i = 0; i < 4; i++)
        {
            info.Passcode[i] = (PasscodeButton)data[offset + i];
        }
        Logger.Info<AccountFile>($"Parsed Passcode: [{string.Join(", ", info.Passcode.Select(p => p.ToString()))}]");
        offset += 4;

        // 0x3C - OnlineDomain (20 bytes ASCII)
        Logger.Trace<AccountFile>($"Parsing OnlineDomain at offset {offset:X2} (20 bytes ASCII)");
        info.OnlineDomain = Encoding.ASCII.GetString(data, offset, 20).TrimEnd('\0');
        Logger.Info<AccountFile>($"Parsed OnlineDomain: '{info.OnlineDomain}'");
        offset += 20;

        // 0x50 - OnlineKerberosRealm (24 bytes ASCII)
        Logger.Trace<AccountFile>($"Parsing OnlineKerberosRealm at offset {offset:X2} (24 bytes ASCII)");
        info.OnlineKerberosRealm = Encoding.ASCII.GetString(data, offset, 24).TrimEnd('\0');
        Logger.Info<AccountFile>($"Parsed OnlineKerberosRealm: '{info.OnlineKerberosRealm}'");
        offset += 24;

        // 0x68 - OnlineKey (16 bytes)
        Logger.Trace<AccountFile>($"Parsing OnlineKey at offset {offset:X2} (16 bytes)");
        Array.Copy(data, offset, info.OnlineKey, 0, 16);
        Logger.Debug<AccountFile>($"Parsed OnlineKey: {BitConverter.ToString(info.OnlineKey)}");
        offset += 16;

        // 0x78 - UserPassportMembername (114 bytes ASCII)
        Logger.Trace<AccountFile>($"Parsing UserPassportMembername at offset {offset:X2} (114 bytes ASCII)");
        info.UserPassportMembername = Encoding.ASCII.GetString(data, offset, 114).TrimEnd('\0');
        Logger.Info<AccountFile>($"Parsed UserPassportMembername: '{info.UserPassportMembername}'");
        offset += 114;

        // 0xEA - UserPassportPassword (32 bytes ASCII)
        Logger.Trace<AccountFile>($"Parsing UserPassportPassword at offset {offset:X2} (32 bytes ASCII)");
        info.UserPassportPassword = Encoding.ASCII.GetString(data, offset, 32).TrimEnd('\0');
        Logger.Info<AccountFile>($"Parsed UserPassportPassword: '{info.UserPassportPassword}'");
        offset += 32;

        // 0x10A - OwnerPassportMembername (114 bytes ASCII)
        Logger.Trace<AccountFile>($"Parsing OwnerPassportMembername at offset {offset:X2} (114 bytes ASCII)");
        info.OwnerPassportMembername = Encoding.ASCII.GetString(data, offset, 114).TrimEnd('\0');
        Logger.Info<AccountFile>($"Parsed OwnerPassportMembername: '{info.OwnerPassportMembername}'");
        offset += 114;

        Logger.Debug<AccountFile>($"Completed parsing account data. Total parsed: {offset} bytes");
        Logger.Info<AccountFile>($"Successfully parsed account: '{info.Gamertag}' (XUID: {info.Xuid})");

        // Log warnings for potentially concerning values
        if (string.IsNullOrEmpty(info.Gamertag))
        {
            Logger.Warning<AccountFile>("Gamertag is empty");
        }

        if (info.Xuid.Value == 0)
        {
            Logger.Warning<AccountFile>("XUID is zero, which may indicate an invalid account or this is an offline account");
        }

        Logger.Trace<AccountFile>($"ParseFromBytes completed successfully");
        return info;
    }

    /// <summary>
    /// Encrypts account information into an encrypted byte array.
    /// </summary>
    /// <param name="info">The account information to encrypt.</param>
    /// <param name="devkit">Whether to use devkit mode for encryption (default: false for retail mode).</param>
    /// <returns>The encrypted account file as a byte array.</returns>
    private static byte[] Encrypt(AccountInfo info, bool devkit = false)
    {
        Logger.Trace<AccountFile>($"Starting encryption process (Mode: {(devkit ? "Devkit" : "Retail")})");

        try
        {
            // Grab the XeKey
            byte[] key = CryptoUtils.GetKey(devkit);
            Logger.Debug<AccountFile>($"Key: {BitConverter.ToString(key)}");

            // Prepare a decrypted payload: confounder + account data
            byte[] confounder = new byte[CONFOUNDER_LENGTH];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(confounder);
            }
            Logger.Debug<AccountFile>($"Confounder: {BitConverter.ToString(confounder)}");

            byte[] accountData = AccountToBytes(info);
            Logger.Debug<AccountFile>($"Account Data: {BitConverter.ToString(accountData)}");
            byte[] payload = new byte[TOTAL_PAYLOAD_LENGTH];
            Array.Copy(confounder, 0, payload, 0, CONFOUNDER_LENGTH);
            Array.Copy(accountData, 0, payload, CONFOUNDER_LENGTH, ACCOUNT_DATA_LENGTH);
            Logger.Debug<AccountFile>($"Payload (Confounder + AccountData): {BitConverter.ToString(payload)}");

            // HMAC of confounder + account data
            byte[] hmac = CryptoUtils.HmacSha1(key, payload, outputLen: 16);
            Logger.Debug<AccountFile>($"HMAC: {BitConverter.ToString(hmac)}");

            // Generate RC4 key from HMAC
            byte[] rc4Key = CryptoUtils.HmacSha1(key, hmac, outputLen: 16);
            Logger.Debug<AccountFile>($"RC4 Key: {BitConverter.ToString(rc4Key)}");

            // Encrypt confounder + account data
            byte[] encryptedPayload = new byte[TOTAL_PAYLOAD_LENGTH];
            CryptoUtils.RC4(rc4Key, payload, 0, TOTAL_PAYLOAD_LENGTH, encryptedPayload);
            Logger.Debug<AccountFile>($"Encrypted Payload: {BitConverter.ToString(encryptedPayload)}");

            // Build file: HMAC + encrypted payload
            byte[] accountFile = new byte[HMAC_LENGTH + TOTAL_PAYLOAD_LENGTH];
            Array.Copy(hmac, 0, accountFile, 0, HMAC_LENGTH);
            Array.Copy(encryptedPayload, 0, accountFile, HMAC_LENGTH, TOTAL_PAYLOAD_LENGTH);
            Logger.Debug<AccountFile>($"Final File: {BitConverter.ToString(accountFile)}");

            Logger.Info<AccountFile>($"Encryption completed successfully. Total file size: {accountFile.Length} bytes");
            Logger.Trace<AccountFile>("Encryption process completed successfully");

            return accountFile;
        }
        catch (Exception ex)
        {
            Logger.Error<AccountFile>($"Encryption failed: {ex.Message}");
            Logger.LogExceptionDetails<AccountFile>(ex);
            throw;
        }
    }

    /// <summary>
    /// Converts an AccountInfo object to raw byte data for encryption.
    /// </summary>
    /// <param name="info">The account information to convert.</param>
    /// <returns>The account information as a byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when info is null.</exception>
    private static byte[] AccountToBytes(AccountInfo info)
    {
        Logger.Trace<AccountFile>($"Starting conversion of AccountInfo to bytes for account: {info.Gamertag}");

        byte[] data = new byte[AccountInfo.DataSize];
        int offset = 0;

        // 0x00 - ReservedFlags
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset), (uint)info.ReservedFlags);
        offset += 4;

        // 0x04 - LiveFlags
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset), info.LiveFlags);
        offset += 4;

        // 0x08 - Gamertag (UTF-16 LE → swap to BE)
        byte[] gtBytes = new byte[32];
        Encoding.BigEndianUnicode.GetBytes(info.Gamertag, gtBytes);
        Array.Copy(gtBytes, 0, data, offset, 32);
        offset += 32;

        // 0x28 - XUID
        BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(offset), info.Xuid.Value);
        offset += 8;

        // 0x30 - CachedUserFlags
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(offset), info.CachedUserFlags);
        offset += 4;

        // 0x34 - ServiceProvider
        WriteAsciiFixed(data, ref offset, info.ServiceProvider, 4);

        // 0x38 - Passcode
        for (int i = 0; i < 4; i++)
        {
            data[offset + i] = (byte)info.Passcode[i];
        }
        offset += 4;

        // 0x3C - OnlineDomain
        WriteAsciiFixed(data, ref offset, info.OnlineDomain, 20);

        // 0x50 - OnlineKerberosRealm
        WriteAsciiFixed(data, ref offset, info.OnlineKerberosRealm, 24);

        // 0x68 - OnlineKey
        Array.Copy(info.OnlineKey, 0, data, offset, 16);
        offset += 16;

        // 0x78 - UserPassportMembername
        WriteAsciiFixed(data, ref offset, info.UserPassportMembername, 114);

        // 0xEA - UserPassportPassword
        WriteAsciiFixed(data, ref offset, info.UserPassportPassword, 32);

        // 0x10A - OwnerPassportMembername
        WriteAsciiFixed(data, ref offset, info.OwnerPassportMembername, 114);

        Logger.Debug<AccountFile>($"AccountToBytes completed. Converted {offset} bytes of account data");
        Logger.Trace<AccountFile>("AccountInfo to bytes conversion completed successfully");

        return data;
    }

    /// <summary>
    /// Writes a fixed-length ASCII string to the destination byte array.
    /// </summary>
    /// <param name="dest">The destination byte array.</param>
    /// <param name="offset">Reference to the current offset in the destination array.</param>
    /// <param name="value">The string value to write.</param>
    /// <param name="fieldLen">The fixed length of the field.</param>
    private static void WriteAsciiFixed(byte[] dest, ref int offset, string value, int fieldLen)
    {
        Logger.Trace<AccountFile>($"Writing ASCII string '{value}' to offset {offset} with length {fieldLen}");

        byte[] encoded = Encoding.ASCII.GetBytes(value);
        int copyLen = Math.Min(encoded.Length, fieldLen);
        Array.Copy(encoded, 0, dest, offset, copyLen);
        // The remaining bytes are already 0 from array init

        Logger.Debug<AccountFile>($"Wrote {copyLen} bytes for field at offset {offset}");
        offset += fieldLen;
    }
}