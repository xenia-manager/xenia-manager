using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Models.Files.Stfs;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Manage;

/// <summary>
/// Manages save game export and import operations.
/// </summary>
public class SaveManager
{
    /// <summary>
    /// Gets all save game header files for a specific game.
    /// </summary>
    /// <param name="game">The game to get save files for.</param>
    /// <param name="xuid">Optional XUID in hex string format to filter saves for a specific account. If null or empty, saves for all accounts are returned.</param>
    /// <returns>An enumerable collection of header files representing the save games.</returns>
    public static IEnumerable<HeaderFile> GetSaveFiles(Game game, string? xuid = null)
    {
        Logger.Trace<SaveManager>($"Getting save files for game: '{game.Title}' (TitleId: {game.GameId}, XUID: {xuid ?? "all"})");

        // Get the Xenia content folder path for the game's Xenia version
        XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(game.XeniaVersion);
        string xeniaContentFolder = AppPathResolver.GetFullPath(versionInfo.ContentFolderLocation);
        Logger.Debug<SaveManager>($"Xenia content folder: '{xeniaContentFolder}'");

        List<HeaderFile> saveFiles = [];

        // Get all XUID folders in the content folder
        if (!Directory.Exists(xeniaContentFolder))
        {
            Logger.Warning<SaveManager>($"Xenia content folder does not exist: '{xeniaContentFolder}'");
            return saveFiles;
        }

        string[] xuidFolders = Directory.GetDirectories(xeniaContentFolder);
        Logger.Debug<SaveManager>($"Found {xuidFolders.Length} XUID folders");

        foreach (string xuidFolder in xuidFolders)
        {
            string folderXuid = Path.GetFileName(xuidFolder);

            // Filter by XUID if specified
            if (!string.IsNullOrEmpty(xuid) && !folderXuid.Equals(xuid, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Trace<SaveManager>($"Skipping XUID '{folderXuid}' (filter: '{xuid}')");
                continue;
            }

            Logger.Trace<SaveManager>($"Processing XUID: '{folderXuid}'");

            // Get the game's title folder for this XUID
            string titleFolder = Path.Combine(xuidFolder, game.GameId.ToUpperInvariant());
            if (!Directory.Exists(titleFolder))
            {
                Logger.Trace<SaveManager>($"Title folder not found for XUID '{folderXuid}': '{titleFolder}'");
                continue;
            }

            // Get the saved games folder
            string savedGamesFolder = Path.Combine(titleFolder, ContentType.SavedGame.ToHexString());
            string savedGamesHeaderFolder = Path.Combine(titleFolder, "Headers", ContentType.SavedGame.ToHexString());

            Logger.Trace<SaveManager>($"Saved games folder: '{savedGamesFolder}'");
            Logger.Trace<SaveManager>($"Saved games header folder: '{savedGamesHeaderFolder}'");

            if (!Directory.Exists(savedGamesFolder))
            {
                Logger.Trace<SaveManager>($"Saved games folder does not exist: '{savedGamesFolder}'");
                continue;
            }

            // Get all save files in the saved games folder
            string[] saveFilesPaths =
            [
                ..Directory.GetDirectories(savedGamesFolder),
                ..Directory.GetFiles(savedGamesFolder)
            ];

            Logger.Debug<SaveManager>($"Found {saveFilesPaths.Length} save files for XUID '{folderXuid}'");

            foreach (string saveFilePath in saveFilesPaths)
            {
                string saveFileName = Path.GetFileName(saveFilePath);
                string headerFilePath = Path.Combine(savedGamesHeaderFolder, $"{saveFileName}.header");

                try
                {
                    Logger.Trace<SaveManager>($"Loading header file for '{saveFileName}'");
                    HeaderFile header = HeaderFile.Load(headerFilePath);
                    saveFiles.Add(header);
                    Logger.Debug<SaveManager>($"Successfully loaded header for '{saveFileName}'");
                }
                catch (Exception ex)
                {
                    Logger.Warning<SaveManager>($"Failed to load header file for '{saveFileName}': {ex.Message}");
                    Logger.LogExceptionDetails<SaveManager>(ex);

                    // Create a temporary header file as a fallback
                    Logger.Info<SaveManager>($"Creating temporary header file for '{saveFileName}'");
                    HeaderFile tempHeader = new HeaderFile
                    {
                        FileName = saveFileName,
                        DisplayName = saveFileName,
                        ContentType = ContentType.SavedGame,
                        TitleId = uint.Parse(game.GameId, NumberStyles.HexNumber),
                        AccountXuid = new AccountXuid(ulong.Parse(folderXuid, NumberStyles.HexNumber)),
                        HeaderSize = HeaderFile.FullHeaderSize,
                        FilePath = headerFilePath
                    };
                    saveFiles.Add(tempHeader);
                }
            }
        }

        Logger.Info<SaveManager>($"Found {saveFiles.Count} save files for game '{game.Title}' (TitleId: {game.GameId}, XUID: {xuid ?? "all"})");
        return saveFiles;
    }
    /// <summary>
    /// Reconstructs the actual content file path from a header file.
    /// </summary>
    /// <param name="headerFile">The header file to get the path from.</param>
    /// <returns>The path to the actual content file, or empty string if not found.</returns>
    private static string GetActualFilePath(HeaderFile headerFile)
    {
        Logger.Trace<SaveManager>($"Reconstructing file path for header: '{headerFile.FileName}'");
        Logger.Debug<SaveManager>($"Header file path: '{headerFile.FilePath}'");

        // Split the path to get the base directory (remove "\Headers\...")
        string[] parts = Regex.Split(headerFile.FilePath, @"\\Headers", RegexOptions.IgnoreCase);
        string basePath = parts[0];
        Logger.Debug<SaveManager>($"Base path: '{basePath}'");

        // Primary reconstructed path using ContentType hex
        string contentTypeHex = headerFile.ContentType.ToHexString();
        string primaryPath = Path.Combine(basePath, contentTypeHex, headerFile.FileName);
        Logger.Debug<SaveManager>($"Primary reconstructed path: '{primaryPath}'");

        if (File.Exists(primaryPath) || Directory.Exists(primaryPath))
        {
            Logger.Info<SaveManager>($"Found actual file using primary path: '{primaryPath}'");
            return primaryPath;
        }

        // Backup path (remove \Headers\ and .header)
        string backupPath = headerFile.FilePath
            .Replace(@"\Headers\", @"\", StringComparison.OrdinalIgnoreCase)
            .Replace(".header", "", StringComparison.OrdinalIgnoreCase);
        Logger.Debug<SaveManager>($"Backup reconstructed path: '{backupPath}'");

        if (File.Exists(backupPath) || Directory.Exists(backupPath))
        {
            Logger.Info<SaveManager>($"Found actual file using backup path: '{backupPath}'");
            return backupPath;
        }

        Logger.Warning<SaveManager>($"Failed to reconstruct file path for header: '{headerFile.FileName}'");
        // Return an empty string if both methods fail
        return string.Empty;
    }
    /// <summary>
    /// Exports save games to a zip file.
    /// </summary>
    /// <param name="headerFiles">The header files to export.</param>
    /// <param name="zipPath">The path to the zip file to create.</param>
    /// <returns>True if the export was successful; otherwise, false.</returns>
    public static async Task<bool> ExportSave(IEnumerable<HeaderFile> headerFiles, string zipPath)
    {
        List<HeaderFile> headerFileList = headerFiles.ToList();

        Logger.Trace<SaveManager>($"Starting ExportSave operation");
        Logger.Debug<SaveManager>($"Number of header files to export: {headerFileList.Count}");
        Logger.Debug<SaveManager>($"Destination zip path: '{zipPath}'");

        if (headerFileList.Count == 0)
        {
            Logger.Warning<SaveManager>("No header files to export");
            return false;
        }

        try
        {
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Logger.Debug<SaveManager>($"Timestamp: '{timeStamp}'");

            // Create a temporary directory for export structure
            string tempDir = Path.Combine(Path.GetTempPath(), $"XeniaSaveExport_{timeStamp}");
            Logger.Trace<SaveManager>($"Creating temporary directory: '{tempDir}'");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Get the TitleId from the first header file
                string titleId = headerFileList.First().TitleId.ToString("X8");
                Logger.Info<SaveManager>($"Starting export of {headerFileList.Count} save games with TitleId: 0x{titleId}");
                Logger.Debug<SaveManager>($"First header file: '{headerFileList.First().FileName}'");

                // Create the export structure
                foreach (HeaderFile headerFile in headerFileList)
                {
                    string contentTypeHex = headerFile.ContentType.ToHexString();
                    Logger.Trace<SaveManager>($"Processing header file: '{headerFile.FileName}' (ContentType: 0x{contentTypeHex})");

                    // Create directory structure: TitleId/ContentType/
                    string contentDir = Path.Combine(tempDir, titleId, contentTypeHex);
                    Logger.Trace<SaveManager>($"Creating content directory: '{contentDir}'");
                    Directory.CreateDirectory(contentDir);

                    // Create Headers directory: TitleId/Headers/ContentType/
                    string headersDir = Path.Combine(tempDir, titleId, "Headers", contentTypeHex);
                    Logger.Trace<SaveManager>($"Creating headers directory: '{headersDir}'");
                    Directory.CreateDirectory(headersDir);

                    // Get the actual content file path (not the .header file path)
                    string sourcePath = GetActualFilePath(headerFile);

                    if (Directory.Exists(sourcePath))
                    {
                        // Copy directory contents
                        Logger.Debug<SaveManager>($"Copying directory from '{sourcePath}' to '{Path.Combine(contentDir, Path.GetFileName(sourcePath))}'");
                        StorageUtilities.CopyDirectory(sourcePath, Path.Combine(contentDir, Path.GetFileName(sourcePath)), true);
                    }
                    else if (File.Exists(sourcePath))
                    {
                        // Copy a single file
                        Logger.Debug<SaveManager>($"Copying file from '{sourcePath}' to '{Path.Combine(contentDir, Path.GetFileName(sourcePath))}'");
                        File.Copy(sourcePath, Path.Combine(contentDir, Path.GetFileName(sourcePath)), true);
                    }
                    else
                    {
                        Logger.Warning<SaveManager>($"Source path not found for header file: '{headerFile.FileName}'");
                    }

                    // Copy header file to the Headers directory
                    if (File.Exists(headerFile.FilePath))
                    {
                        Logger.Trace<SaveManager>($"Copying header file from '{headerFile.FilePath}' to '{Path.Combine(headersDir, Path.GetFileName(headerFile.FilePath))}'");
                        File.Copy(headerFile.FilePath, Path.Combine(headersDir, Path.GetFileName(headerFile.FilePath)), true);
                    }
                    else
                    {
                        Logger.Warning<SaveManager>($"Header file not found: '{headerFile.FilePath}'");
                    }
                }

                // Create the zip file
                if (File.Exists(zipPath))
                {
                    Logger.Trace<SaveManager>($"Deleting existing zip file: '{zipPath}'");
                    File.Delete(zipPath);
                }

                Logger.Info<SaveManager>($"Creating zip archive at '{zipPath}'");
                await ZipFile.CreateFromDirectoryAsync(tempDir, zipPath);

                Logger.Info<SaveManager>($"Successfully exported {headerFileList.Count} save games to '{zipPath}'");
                return true;
            }
            finally
            {
                // Clean up temporary directory
                if (Directory.Exists(tempDir))
                {
                    Logger.Trace<SaveManager>($"Cleaning up temporary directory: '{tempDir}'");
                    Directory.Delete(tempDir, true);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error<SaveManager>($"Failed to export save games: {ex.Message}");
            Logger.LogExceptionDetails<SaveManager>(ex);
            return false;
        }
    }

    /// <summary>
    /// Imports save games from a zip file.
    /// </summary>
    /// <param name="zipPath">The path to the zip file to import.</param>
    /// <param name="destinationBase">The base destination path for the imported saves.</param>
    /// <returns>True if the import was successful; otherwise, false.</returns>
    public static async Task<bool> ImportSave(string zipPath, string destinationBase)
    {
        Logger.Trace<SaveManager>($"Starting ImportSave operation");
        Logger.Debug<SaveManager>($"Source zip path: '{zipPath}'");
        Logger.Debug<SaveManager>($"Destination base path: '{destinationBase}'");

        if (!File.Exists(zipPath))
        {
            Logger.Error<SaveManager>($"Zip file not found: '{zipPath}'");
            return false;
        }

        try
        {
            // Create a temporary directory for extraction
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string tempDir = Path.Combine(Path.GetTempPath(), $"XeniaSaveImport_{timeStamp}");
            Logger.Trace<SaveManager>($"Creating temporary directory: '{tempDir}'");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Extract the zip file
                Logger.Info<SaveManager>($"Extracting zip file from '{zipPath}' to '{tempDir}'");
                await ZipFile.ExtractToDirectoryAsync(zipPath, tempDir);

                // Find the TitleId folder in the extracted content
                string? titleIdFolder = Directory.GetDirectories(tempDir).FirstOrDefault();
                if (titleIdFolder == null)
                {
                    Logger.Error<SaveManager>("Invalid zip structure: no TitleId folder found");
                    return false;
                }

                string titleId = Path.GetFileName(titleIdFolder);
                Logger.Info<SaveManager>($"Found TitleId folder: '{titleId}'");

                // Validate TitleId (should be 8 characters hex)
                if (!uint.TryParse(titleId, NumberStyles.HexNumber, null, out _))
                {
                    Logger.Error<SaveManager>($"Invalid TitleId format: '{titleId}' (expected 8-character hex)");
                    return false;
                }

                // Copy content folders (excluding Headers)
                Logger.Debug<SaveManager>($"Copying content folders to '{Path.Combine(destinationBase, titleId)}'");
                foreach (string sourceDir in Directory.GetDirectories(titleIdFolder))
                {
                    string dirName = Path.GetFileName(sourceDir);
                    if (!dirName.Equals("Headers", StringComparison.OrdinalIgnoreCase))
                    {
                        string destDir = Path.Combine(destinationBase, titleId, dirName);
                        Logger.Trace<SaveManager>($"Copying folder '{dirName}' from '{sourceDir}' to '{destDir}'");
                        StorageUtilities.CopyDirectory(sourceDir, destDir, true);
                    }
                    else
                    {
                        Logger.Trace<SaveManager>($"Skipping 'Headers' folder during content copy: '{sourceDir}'");
                    }
                }

                // Copy header files if the Headers folder exists
                string headersSourceDir = Path.Combine(titleIdFolder, "Headers");
                if (Directory.Exists(headersSourceDir))
                {
                    string headersDestDir = Path.Combine(destinationBase, titleId, "Headers");
                    Logger.Debug<SaveManager>($"Copying headers from '{headersSourceDir}' to '{headersDestDir}'");
                    StorageUtilities.CopyDirectory(headersSourceDir, headersDestDir, true);
                }
                else
                {
                    Logger.Warning<SaveManager>($"Headers folder not found in zip: '{headersSourceDir}'");
                }

                Logger.Info<SaveManager>($"Successfully imported save games from '{zipPath}' to '{Path.Combine(destinationBase, titleId)}'");
                return true;
            }
            finally
            {
                // Clean up temporary directory
                if (Directory.Exists(tempDir))
                {
                    Logger.Trace<SaveManager>($"Cleaning up temporary directory: '{tempDir}'");
                    Directory.Delete(tempDir, true);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error<SaveManager>($"Failed to import save games: {ex.Message}");
            Logger.LogExceptionDetails<SaveManager>(ex);
            return false;
        }
    }
}