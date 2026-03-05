using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Models.Files.Stfs;
using XeniaManager.Core.Utilities.Paths;

namespace XeniaManager.Core.Manage;

/// <summary>
/// Manages Xbox 360 account profiles for Xenia emulator, including creation and management of account files.
/// </summary>
public class ProfileManager
{
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
        // Generate XUID for directory/file path usage (not stored in the account file)
        AccountXuid pathXuid = defaultXuid ? AccountXuid.CreateDefault() : AccountXuid.GenerateOfflineXuid();

        // Create Account
        AccountInfo info = new AccountInfo
        {
            // Set the gamertag
            Gamertag = gamertag,
            // Set XUID to 0 for offline accounts (XUID field is used for online XUID)
            // The generated XUID is only used for the file path structure
            Xuid = new AccountXuid(0),
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

        Logger.Trace<ProfileManager>($"Starting CreateAccount operation for gamertag: '{info.Gamertag}' (Path XUID: {pathXuid})");
        Logger.Debug<ProfileManager>($"Setting gamertag to: '{gamertag}'");
        Logger.Debug<ProfileManager>($"Setting XUID in AccountInfo to 0 (offline account)");
        Logger.Debug<ProfileManager>($"Generated Path XUID - defaultXuid: {defaultXuid}");

        Logger.Debug<ProfileManager>("Set default values for account properties");
        Logger.Info<ProfileManager>($"Created account with gamertag: '{info.Gamertag}' and XUID: {info.Xuid.ToString()} (stored as 0 for offline account)");

        // Create the directory
        Logger.Debug<ProfileManager>($"Retrieving Xenia version info for: {version}");
        XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);

        // TODO: Replace FFFE07D1 with an enum or constant
        // Content Folder + PathXuid (for directory structure) + FFFE07D1 (Dashboard) + 00010000 (Xbox 360 Title) + PathXuid + Account
        Logger.Debug<ProfileManager>($"Constructing account file path using version content folder: {versionInfo.ContentFolderLocation}");
        string accountFileLocation = Path.Combine(AppPathResolver.GetFullPath(versionInfo.ContentFolderLocation),
            pathXuid.ToString(), "FFFE07D1", ContentType.Profile.ToHexString(), pathXuid.ToString(), "Account");

        Logger.Debug<ProfileManager>($"Account file will be saved to: {accountFileLocation}");

        string? directoryPath = Path.GetDirectoryName(accountFileLocation);
        if (string.IsNullOrEmpty(directoryPath))
        {
            Logger.Error<ProfileManager>($"Couldn't find a directory for the account file {directoryPath}.");
            throw new DirectoryNotFoundException($"Couldn't find a directory for the account file {directoryPath}.");
        }

        Logger.Info<ProfileManager>($"Creating directory structure for account file: {directoryPath}");
        Directory.CreateDirectory(directoryPath);

        Logger.Info<ProfileManager>($"Saving new account file to: {accountFileLocation}");
        AccountFile.Save(info, accountFileLocation);

        Logger.Info<ProfileManager>($"Successfully created and saved account for gamertag: '{info.Gamertag}' at {accountFileLocation}");
        Logger.Trace<ProfileManager>("CreateAccount operation completed successfully");

        return info;
    }

    /// <summary>
    /// Loads all account profiles from the specified Xenia version's content folder.
    /// Profiles are identified by XUID-named directories that contain the account file structure.
    /// </summary>
    /// <param name="version">The Xenia version whose content folder will be scanned for profiles.</param>
    /// <returns>A list of AccountInfo objects representing all found profiles.</returns>
    public static List<AccountInfo> LoadProfiles(XeniaVersion version)
    {
        Logger.Trace<ProfileManager>($"Starting LoadProfiles operation for version: {version}");

        List<AccountInfo> profiles = [];
        try
        {
            XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);
            string contentFolderPath = AppPathResolver.GetFullPath(versionInfo.ContentFolderLocation);

            Logger.Debug<ProfileManager>($"Scanning content folder path: {contentFolderPath}");

            if (!Directory.Exists(contentFolderPath))
            {
                Logger.Warning<ProfileManager>($"Content folder does not exist: {contentFolderPath}. Returning empty profile list.");
                return profiles;
            }

            // Get all subdirectories in the content folder
            // These should be XUID-named directories representing individual profiles
            string[] xuidDirectories = Directory.GetDirectories(contentFolderPath);

            Logger.Debug<ProfileManager>($"Found {xuidDirectories.Length} potential XUID directories in content folder");

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
                    string expectedAccountPath = Path.Combine(xuidDir, "FFFE07D1", ContentType.Profile.ToHexString(), xuid, "Account");

                    if (File.Exists(expectedAccountPath))
                    {
                        Logger.Debug<ProfileManager>($"Loading profile with XUID: {xuid}");
                        try
                        {
                            AccountInfo profile = AccountFile.Load(expectedAccountPath);
                            profiles.Add(profile);
                            Logger.Info<ProfileManager>($"Loaded profile: '{profile.Gamertag}' ({profile.Xuid})");
                        }
                        catch (Exception ex)
                        {
                            Logger.Warning<ProfileManager>($"Failed to load profile at {expectedAccountPath}");
                            Logger.LogExceptionDetails<ProfileManager>(ex);
                        }
                    }
                    else
                    {
                        Logger.Debug<ProfileManager>($"Directory {xuid} exists but doesn't contain the expected account file structure at: {expectedAccountPath}");
                    }
                }
                else
                {
                    Logger.Debug<ProfileManager>($"Directory {xuid} does not appear to be a valid XUID format, skipping.");
                }
            }

            Logger.Info<ProfileManager>($"Loaded {profiles.Count} account profiles from content folder for version: {version}");
            Logger.Trace<ProfileManager>("LoadProfiles operation completed successfully");

            return profiles;
        }
        catch (Exception ex)
        {
            Logger.Error<ProfileManager>($"Error loading profiles for version {version}");
            Logger.LogExceptionDetails<ProfileManager>(ex);
            return profiles;
        }
    }
}