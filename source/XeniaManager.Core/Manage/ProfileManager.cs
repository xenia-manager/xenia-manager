using System.Globalization;
using System.IO.Compression;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Models.Files.Stfs;
using XeniaManager.Core.Utilities;
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

        // Set the PathXuid for file path management
        info.PathXuid = pathXuid;

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
                            profile.PathXuid = new AccountXuid(ulong.Parse(xuid, NumberStyles.HexNumber));
                            profiles.Add(profile);
                            Logger.Info<ProfileManager>($"Loaded profile: '{profile.Gamertag}' (XUID: {profile.Xuid}, PathXuid: {profile.PathXuid})");
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

    /// <summary>
    /// Saves all modified account profiles back to their respective file locations.
    /// Iterates through the list of profiles and saves each one using its stored PathXuid and Version.
    /// </summary>
    /// <param name="profiles">The list of AccountInfo profiles to save.</param>
    /// <param name="version">Xenia Version</param>
    /// <returns>The number of profiles successfully saved.</returns>
    public static int SaveProfiles(List<AccountInfo> profiles, XeniaVersion version)
    {
        Logger.Trace<ProfileManager>($"Starting SaveProfiles operation for {profiles.Count} profiles");

        int savedCount = 0;
        int failedCount = 0;
        XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);

        foreach (AccountInfo profile in profiles)
        {
            try
            {
                // Validate that the profile has the required path information
                if (profile.PathXuid == null)
                {
                    Logger.Warning<ProfileManager>($"Cannot save profile '{profile.Gamertag}': PathXuid is not set");
                    failedCount++;
                    continue;
                }

                // Construct the file path using the stored PathXuid and Version
                string accountFileLocation = Path.Combine(AppPathResolver.GetFullPath(versionInfo.ContentFolderLocation),
                    profile.PathXuid.Value.ToString(), "FFFE07D1", ContentType.Profile.ToHexString(), profile.PathXuid.Value.ToString(), "Account");

                Logger.Debug<ProfileManager>($"Saving profile '{profile.Gamertag}' to: {accountFileLocation}");

                // Ensure the directory exists
                string? directoryPath = Path.GetDirectoryName(accountFileLocation);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Logger.Info<ProfileManager>($"Creating directory structure: {directoryPath}");
                    Directory.CreateDirectory(directoryPath);
                }

                // Save the profile
                AccountFile.Save(profile, accountFileLocation);
                Logger.Info<ProfileManager>($"Successfully saved profile '{profile.Gamertag}' ({profile.Xuid})");
                savedCount++;
            }
            catch (Exception ex)
            {
                Logger.Error<ProfileManager>($"Failed to save profile '{profile.Gamertag}'");
                Logger.LogExceptionDetails<ProfileManager>(ex);
                failedCount++;
            }
        }

        Logger.Info<ProfileManager>($"SaveProfiles completed: {savedCount} saved, {failedCount} failed");
        Logger.Trace<ProfileManager>("SaveProfiles operation completed");

        return savedCount;
    }

    /// <summary>
    /// Deletes an account profile and its entire folder from the Xenia emulator content location.
    /// This will remove all saves and content associated with this account.
    /// </summary>
    /// <param name="version">The Xenia version from which to delete the account.</param>
    /// <param name="profile">The account profile to delete.</param>
    /// <returns>True if the account was successfully deleted, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when profile is null.</exception>
    /// <exception cref="ArgumentException">Thrown when profile PathXuid is not set.</exception>
    public static bool DeleteAccount(XeniaVersion version, AccountInfo profile)
    {
        Logger.Trace<ProfileManager>($"Starting DeleteAccount operation for gamertag: '{profile?.Gamertag}'");

        if (profile == null)
        {
            Logger.Error<ProfileManager>("Cannot delete account: profile is null");
            throw new ArgumentNullException(nameof(profile), "Cannot delete a null profile");
        }

        if (profile.PathXuid == null)
        {
            Logger.Error<ProfileManager>($"Cannot delete profile '{profile.Gamertag}': PathXuid is not set");
            throw new ArgumentException("Profile PathXuid is not set", nameof(profile));
        }

        try
        {
            XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);
            string accountFolderPath = Path.Combine(AppPathResolver.GetFullPath(versionInfo.ContentFolderLocation), profile.PathXuid.Value.ToString());

            Logger.Debug<ProfileManager>($"Account folder path to delete: {accountFolderPath}");

            if (!Directory.Exists(accountFolderPath))
            {
                Logger.Warning<ProfileManager>($"Account folder does not exist: {accountFolderPath}");
                return false;
            }

            Logger.Info<ProfileManager>($"Deleting account folder: {accountFolderPath}");
            Logger.Warning<ProfileManager>($"This will permanently delete all saves and content for account '{profile.Gamertag}'");

            Directory.Delete(accountFolderPath, true);

            Logger.Info<ProfileManager>($"Successfully deleted account '{profile.Gamertag}' and all associated content");
            Logger.Trace<ProfileManager>("DeleteAccount operation completed successfully");

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error<ProfileManager>($"Failed to delete account '{profile.Gamertag}'");
            Logger.LogExceptionDetails<ProfileManager>(ex);
            return false;
        }
    }

    /// <summary>
    /// Exports an account profile to a zip file.
    /// </summary>
    /// <param name="version">The Xenia version from which to export the profile.</param>
    /// <param name="profile">The account profile to export.</param>
    /// <param name="exportSaves">True to export all content associated with the profile; false to export only the Dashboard data folder.</param>
    /// <param name="outputPath">The path where the zip file will be created.</param>
    /// <returns>True if the export was successful; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when profile is null or outputPath is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when profile PathXuid is not set.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the account folder does not exist.</exception>
    public static async Task<bool> ExportProfile(XeniaVersion version, AccountInfo? profile, bool exportSaves = false, string? outputPath = null)
    {
        if (profile == null)
        {
            Logger.Error<ProfileManager>("Cannot export account: profile is null");
            throw new ArgumentNullException(nameof(profile), "Cannot export a null profile");
        }

        if (profile.PathXuid == null)
        {
            Logger.Error<ProfileManager>($"Cannot export profile '{profile.Gamertag}': PathXuid is not set");
            throw new ArgumentException("Profile PathXuid is not set", nameof(profile));
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            Logger.Error<ProfileManager>("Cannot export profile: output path is null or empty");
            throw new ArgumentNullException(nameof(outputPath), "Output path must be provided");
        }

        try
        {
            XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);
            string accountFolderPath = Path.Combine(AppPathResolver.GetFullPath(versionInfo.ContentFolderLocation), profile.PathXuid.Value.ToString());

            Logger.Debug<ProfileManager>($"Exporting profile '{profile.Gamertag}' from: {accountFolderPath}");
            Logger.Debug<ProfileManager>($"Export saves: {exportSaves}, Output path: {outputPath}");

            if (!Directory.Exists(accountFolderPath))
            {
                Logger.Error<ProfileManager>($"Account folder does not exist: {accountFolderPath}");
                throw new DirectoryNotFoundException($"Account folder does not exist: {accountFolderPath}");
            }

            // Create a temporary directory for export structure
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string tempDir = Path.Combine(Path.GetTempPath(), $"XeniaProfileExport_{timeStamp}");
            Logger.Trace<ProfileManager>($"Creating temporary directory: '{tempDir}'");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create the export structure with PathXuid as root
                string exportRoot = Path.Combine(tempDir, profile.PathXuid.Value.ToString());
                Logger.Trace<ProfileManager>($"Creating export root: '{exportRoot}'");
                Directory.CreateDirectory(exportRoot);

                if (exportSaves)
                {
                    // Export all folders in accountFolderPath
                    Logger.Info<ProfileManager>($"Exporting all content for profile '{profile.Gamertag}'");
                    StorageUtilities.CopyDirectory(accountFolderPath, exportRoot, true);
                }
                else
                {
                    // Export only FFFE07D1 folder
                    string dashboardFolder = Path.Combine(accountFolderPath, "FFFE07D1");
                    if (Directory.Exists(dashboardFolder))
                    {
                        Logger.Info<ProfileManager>($"Exporting FFFE07D1 content for profile '{profile.Gamertag}'");
                        string dashboardExportFolder = Path.Combine(exportRoot, "FFFE07D1");
                        StorageUtilities.CopyDirectory(dashboardFolder, dashboardExportFolder, true);
                    }
                    else
                    {
                        Logger.Warning<ProfileManager>($"FFFE07D1 folder does not exist: {dashboardFolder}");
                    }
                }

                // Delete existing zip file if it exists
                if (File.Exists(outputPath))
                {
                    Logger.Trace<ProfileManager>($"Deleting existing zip file: '{outputPath}'");
                    File.Delete(outputPath);
                }

                // Create the zip file
                Logger.Info<ProfileManager>($"Creating zip archive at '{outputPath}'");
                await ZipFile.CreateFromDirectoryAsync(tempDir, outputPath);

                Logger.Info<ProfileManager>($"Successfully exported profile '{profile.Gamertag}' to {outputPath}");
                Logger.Trace<ProfileManager>("ExportProfile operation completed successfully");
                return true;
            }
            finally
            {
                // Clean up temporary directory
                if (Directory.Exists(tempDir))
                {
                    Logger.Trace<ProfileManager>($"Cleaning up temporary directory: '{tempDir}'");
                    Directory.Delete(tempDir, true);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error<ProfileManager>($"Failed to export profile '{profile.Gamertag}'");
            Logger.LogExceptionDetails<ProfileManager>(ex);
            return false;
        }
    }

    /// <summary>
    /// Checks if a profile with the given XUID already exists in the content folder.
    /// First checks if the root folder has the XUID directly, then checks inside FFFE07D1/00010000 structure.
    /// </summary>
    /// <param name="version">The Xenia version to check.</param>
    /// <param name="xuid">The XUID to check for.</param>
    /// <returns>The existing profile if found, null otherwise.</returns>
    public static AccountInfo? CheckForExistingProfile(XeniaVersion version, string xuid)
    {
        Logger.Trace<ProfileManager>($"Checking for existing profile with XUID: '{xuid}'");

        try
        {
            XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);
            string contentFolderPath = AppPathResolver.GetFullPath(versionInfo.ContentFolderLocation);

            Logger.Debug<ProfileManager>($"Content folder path: '{contentFolderPath}'");

            if (!Directory.Exists(contentFolderPath))
            {
                Logger.Warning<ProfileManager>($"Content folder does not exist: '{contentFolderPath}'");
                return null;
            }

            // First check: Direct XUID folder in content folder
            string directXuidFolder = Path.Combine(contentFolderPath, xuid);
            if (Directory.Exists(directXuidFolder))
            {
                Logger.Debug<ProfileManager>($"Found direct XUID folder: '{directXuidFolder}'");

                // Check if it has the expected account file structure
                string expectedAccountPath = Path.Combine(directXuidFolder, "FFFE07D1", ContentType.Profile.ToHexString(), xuid, "Account");
                if (File.Exists(expectedAccountPath))
                {
                    Logger.Info<ProfileManager>($"Found existing profile with XUID '{xuid}' in direct folder structure");
                    AccountInfo profile = AccountFile.Load(expectedAccountPath);
                    profile.PathXuid = new AccountXuid(ulong.Parse(xuid, NumberStyles.HexNumber));
                    return profile;
                }
            }

            // Second check: XUID folder inside FFFE07D1/00010000 structure
            string dashboardFolder = Path.Combine(contentFolderPath, "FFFE07D1");
            if (Directory.Exists(dashboardFolder))
            {
                Logger.Debug<ProfileManager>($"Found dashboard folder: '{dashboardFolder}'");

                string profileTypeFolder = Path.Combine(dashboardFolder, ContentType.Profile.ToHexString());
                if (Directory.Exists(profileTypeFolder))
                {
                    Logger.Debug<ProfileManager>($"Found profile type folder: '{profileTypeFolder}'");

                    string xuidInDashboardFolder = Path.Combine(profileTypeFolder, xuid);
                    if (Directory.Exists(xuidInDashboardFolder))
                    {
                        Logger.Debug<ProfileManager>($"Found XUID folder in dashboard structure: '{xuidInDashboardFolder}'");

                        string accountPath = Path.Combine(xuidInDashboardFolder, "Account");
                        if (File.Exists(accountPath))
                        {
                            Logger.Info<ProfileManager>($"Found existing profile with XUID '{xuid}' in dashboard folder structure");
                            AccountInfo profile = AccountFile.Load(accountPath);
                            profile.PathXuid = new AccountXuid(ulong.Parse(xuid, NumberStyles.HexNumber));
                            return profile;
                        }
                    }
                }
            }

            Logger.Debug<ProfileManager>($"No existing profile found with XUID: '{xuid}'");
            return null;
        }
        catch (Exception ex)
        {
            Logger.Error<ProfileManager>($"Error checking for existing profile with XUID '{xuid}'");
            Logger.LogExceptionDetails<ProfileManager>(ex);
            return null;
        }
    }

    /// <summary>
    /// Finds the XUID folder in the extracted content.
    /// First checks for XUID folders at the root level, then checks inside FFFE07D1/00010000 structure.
    /// </summary>
    /// <param name="tempDir">The temporary directory containing extracted content.</param>
    /// <param name="xuid">The XUID folder path if found at root level, null otherwise.</param>
    /// <param name="xuidValue">The XUID value if found, null otherwise.</param>
    /// <param name="dashboardOnly">True if only the dashboard folder structure was found (FFFE07D1/00010000/XUID), false if XUID folder is at root.</param>
    /// <returns>True if a XUID folder was found, false otherwise.</returns>
    private static bool FindXuidFolder(string tempDir, out string? xuid, out string? xuidValue, out bool dashboardOnly)
    {
        xuid = null;
        xuidValue = null;
        dashboardOnly = false;

        // First check: Look for XUID folders at the root level
        string[] rootDirectories = Directory.GetDirectories(tempDir);
        foreach (string dir in rootDirectories)
        {
            string dirName = Path.GetFileName(dir);
            if (AccountXuid.IsValidFormat(dirName))
            {
                xuid = dir;
                xuidValue = dirName;
                dashboardOnly = false;
                Logger.Info<ProfileManager>($"Found XUID folder at root level: '{xuid}'");
                return true;
            }
        }

        // Second check: Look for FFFE07D1/00010000/<XUID> structure (dashboard only export)
        string dashboardFolder = Path.Combine(tempDir, "FFFE07D1");
        if (Directory.Exists(dashboardFolder))
        {
            Logger.Debug<ProfileManager>($"Found dashboard folder in zip: '{dashboardFolder}'");

            string profileTypeFolder = Path.Combine(dashboardFolder, ContentType.Profile.ToHexString());
            if (Directory.Exists(profileTypeFolder))
            {
                Logger.Debug<ProfileManager>($"Found profile type folder in zip: '{profileTypeFolder}'");

                string[] xuidFolders = Directory.GetDirectories(profileTypeFolder);
                foreach (string xuidFolder in xuidFolders)
                {
                    string folderName = Path.GetFileName(xuidFolder);
                    if (AccountXuid.IsValidFormat(folderName))
                    {
                        xuidValue = folderName;
                        dashboardOnly = true;
                        Logger.Info<ProfileManager>($"Found XUID folder in dashboard structure (dashboard-only export): '{xuidValue}'");
                        return true;
                    }
                }
            }
        }

        Logger.Error<ProfileManager>("Invalid zip structure: no valid XUID folder found");
        return false;
    }

    /// <summary>
    /// Imports an account profile from a zip file.
    /// </summary>
    /// <param name="version">The Xenia version to import the profile into.</param>
    /// <param name="zipPath">The path to the zip file to import.</param>
    /// <param name="profiles">The list of profiles to update after import.</param>
    /// <returns>The imported profile if successful, null otherwise.</returns>
    public static async Task<AccountInfo?> ImportProfile(XeniaVersion version, string zipPath, List<AccountInfo> profiles)
    {
        Logger.Trace<ProfileManager>($"Starting ImportProfile operation");
        Logger.Debug<ProfileManager>($"Source zip path: '{zipPath}'");

        if (!File.Exists(zipPath))
        {
            Logger.Error<ProfileManager>($"Zip file not found: '{zipPath}'");
            return null;
        }

        try
        {
            // Create a temporary directory for extraction
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string tempDir = Path.Combine(Path.GetTempPath(), $"XeniaProfileImport_{timeStamp}");
            Logger.Trace<ProfileManager>($"Creating temporary directory: '{tempDir}'");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Extract the zip file
                Logger.Info<ProfileManager>($"Extracting zip file from '{zipPath}' to '{tempDir}'");
                await ZipFile.ExtractToDirectoryAsync(zipPath, tempDir);

                // Find the XUID folder in the extracted content
                if (!FindXuidFolder(tempDir, out string? xuidFolder, out string? xuidValue, out bool dashboardOnly))
                {
                    return null;
                }

                string xuid = xuidValue!;
                Logger.Info<ProfileManager>($"Found XUID: '{xuid}' (dashboard-only: {dashboardOnly})");

                // Validate XUID (should be 8 characters hex)
                if (!AccountXuid.IsValidFormat(xuid))
                {
                    Logger.Error<ProfileManager>($"Invalid XUID format: '{xuid}' (expected 8-character hex)");
                    return null;
                }

                // Check if a profile already exists
                AccountInfo? existingProfile = CheckForExistingProfile(version, xuid);
                if (existingProfile != null)
                {
                    Logger.Warning<ProfileManager>($"Profile with XUID '{xuid}' already exists: '{existingProfile.Gamertag}'");
                    return null; // Caller should handle the replacement logic
                }

                // Get destination base path
                XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);
                string destinationBase = AppPathResolver.GetFullPath(versionInfo.ContentFolderLocation);

                Logger.Debug<ProfileManager>($"Copying profile content to '{Path.Combine(destinationBase, xuid)}'");

                // Copy content based on the export format
                string destDir = Path.Combine(destinationBase, xuid);
                if (dashboardOnly)
                {
                    // Dashboard-only export: copy the entire tempDir content to the XUID destination folder
                    // The zip contains: FFFE07D1/00010000/<XUID>/Account
                    Logger.Info<ProfileManager>($"Importing dashboard-only export to '{destDir}'");
                    StorageUtilities.CopyDirectory(tempDir, destDir, true);
                }
                else
                {
                    // Full export: copy the XUID folder content to the destination
                    // The zip contains: <XUID>/FFFE07D1/00010000/<XUID>/Account
                    Logger.Info<ProfileManager>($"Importing full profile export from '{xuidFolder}' to '{destDir}'");
                    StorageUtilities.CopyDirectory(xuidFolder!, destDir, true);
                }

                // Load the imported profile
                string accountPath = Path.Combine(destDir, "FFFE07D1", ContentType.Profile.ToHexString(), xuid, "Account");
                if (!File.Exists(accountPath))
                {
                    Logger.Error<ProfileManager>($"Account file not found in imported profile: '{accountPath}'");
                    return null;
                }

                AccountInfo importedProfile = AccountFile.Load(accountPath);
                importedProfile.PathXuid = new AccountXuid(ulong.Parse(xuid, NumberStyles.HexNumber));

                // Add to the profiles list
                profiles.Add(importedProfile);

                Logger.Info<ProfileManager>($"Successfully imported profile '{importedProfile.Gamertag}' (XUID: {xuid}) from '{zipPath}'");
                return importedProfile;
            }
            finally
            {
                // Clean up temporary directory
                if (Directory.Exists(tempDir))
                {
                    Logger.Trace<ProfileManager>($"Cleaning up temporary directory: '{tempDir}'");
                    Directory.Delete(tempDir, true);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error<ProfileManager>($"Failed to import profile");
            Logger.LogExceptionDetails<ProfileManager>(ex);
            return null;
        }
    }

    /// <summary>
    /// Imports an account profile from a zip file with replacement handling.
    /// If a profile with the same XUID exists, the onProfileExists callback is invoked to handle the replacement logic.
    /// </summary>
    /// <param name="version">The Xenia version to import the profile into.</param>
    /// <param name="zipPath">The path to the zip file to import.</param>
    /// <param name="profiles">The list of profiles to update after import.</param>
    /// <param name="onProfileExists">Callback function that handles the case when a profile already exists. Should return true to replace, false to cancel.</param>
    /// <returns>The imported profile if successful, null otherwise.</returns>
    public static async Task<AccountInfo?> ImportProfileWithReplacement(XeniaVersion version, string zipPath, List<AccountInfo> profiles, Func<AccountInfo, Task<bool>> onProfileExists)
    {
        Logger.Trace<ProfileManager>($"Starting ImportProfileWithReplacement operation");
        Logger.Debug<ProfileManager>($"Source zip path: '{zipPath}'");

        if (!File.Exists(zipPath))
        {
            Logger.Error<ProfileManager>($"Zip file not found: '{zipPath}'");
            return null;
        }

        try
        {
            // Create a temporary directory for extraction
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string tempDir = Path.Combine(Path.GetTempPath(), $"XeniaProfileImport_{timeStamp}");
            Logger.Trace<ProfileManager>($"Creating temporary directory: '{tempDir}'");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Extract the zip file
                Logger.Info<ProfileManager>($"Extracting zip file from '{zipPath}' to '{tempDir}'");
                await ZipFile.ExtractToDirectoryAsync(zipPath, tempDir);

                // Find the XUID folder in the extracted content
                if (!FindXuidFolder(tempDir, out string? xuidFolder, out string? xuidValue, out bool dashboardOnly))
                {
                    return null;
                }

                string xuid = xuidValue!;
                Logger.Info<ProfileManager>($"Found XUID: '{xuid}' (dashboard-only: {dashboardOnly})");

                // Validate XUID (should be 8 characters hex)
                if (!AccountXuid.IsValidFormat(xuid))
                {
                    Logger.Error<ProfileManager>($"Invalid XUID format: '{xuid}' (expected 8-character hex)");
                    return null;
                }

                // Check if a profile already exists
                AccountInfo? existingProfile = CheckForExistingProfile(version, xuid);
                if (existingProfile != null)
                {
                    Logger.Warning<ProfileManager>($"Profile with XUID '{xuid}' already exists: '{existingProfile.Gamertag}'");

                    // Ask the callback if we should replace
                    bool replace = await onProfileExists(existingProfile);

                    if (!replace)
                    {
                        Logger.Info<ProfileManager>("User chose not to replace existing profile");
                        return null;
                    }

                    Logger.Info<ProfileManager>($"User chose to replace profile '{existingProfile.Gamertag}'");

                    // Delete the existing profile
                    if (!DeleteAccount(version, existingProfile))
                    {
                        Logger.Error<ProfileManager>($"Failed to delete existing profile '{existingProfile.Gamertag}'");
                        return null;
                    }

                    // Remove from the profiles list
                    profiles.Remove(existingProfile);
                }

                // Get destination base path
                XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);
                string destinationBase = AppPathResolver.GetFullPath(versionInfo.ContentFolderLocation);

                Logger.Debug<ProfileManager>($"Copying profile content to '{Path.Combine(destinationBase, xuid)}'");

                // Copy content based on the export format
                string destDir = Path.Combine(destinationBase, xuid);
                if (dashboardOnly)
                {
                    // Dashboard-only export: copy the entire tempDir content to the XUID destination folder
                    // The zip contains: FFFE07D1/00010000/<XUID>/Account
                    Logger.Info<ProfileManager>($"Importing dashboard-only export to '{destDir}'");
                    StorageUtilities.CopyDirectory(tempDir, destDir, true);
                }
                else
                {
                    // Full export: copy the XUID folder content to the destination
                    // The zip contains: <XUID>/FFFE07D1/00010000/<XUID>/Account
                    Logger.Info<ProfileManager>($"Importing full profile export from '{xuidFolder}' to '{destDir}'");
                    StorageUtilities.CopyDirectory(xuidFolder!, destDir, true);
                }

                // Load the imported profile
                string accountPath = Path.Combine(destDir, "FFFE07D1", ContentType.Profile.ToHexString(), xuid, "Account");
                if (!File.Exists(accountPath))
                {
                    Logger.Error<ProfileManager>($"Account file not found in imported profile: '{accountPath}'");
                    return null;
                }

                AccountInfo importedProfile = AccountFile.Load(accountPath);
                importedProfile.PathXuid = new AccountXuid(ulong.Parse(xuid, NumberStyles.HexNumber));

                // Add to the profiles list
                profiles.Add(importedProfile);

                Logger.Info<ProfileManager>($"Successfully imported profile '{importedProfile.Gamertag}' (XUID: {xuid}) from '{zipPath}'");
                return importedProfile;
            }
            finally
            {
                // Clean up temporary directory
                if (Directory.Exists(tempDir))
                {
                    Logger.Trace<ProfileManager>($"Cleaning up temporary directory: '{tempDir}'");
                    Directory.Delete(tempDir, true);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error<ProfileManager>($"Failed to import profile");
            Logger.LogExceptionDetails<ProfileManager>(ex);
            return null;
        }
    }
}