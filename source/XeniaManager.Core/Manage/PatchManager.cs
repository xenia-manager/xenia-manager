using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Database.Patches;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;
using XeniaManager.Core.Utilities.Paths;

namespace XeniaManager.Core.Manage;

/// <summary>
/// Manages patch operations for games including downloading, installing, and maintaining patch files.
/// Handles patch file management across different Xenia emulator versions.
/// </summary>
public class PatchManager
{
    /// <summary>
    /// Shared download manager instance used for patch download operations.
    /// </summary>
    private static readonly DownloadManager _downloadManager = new DownloadManager();

    /// <summary>
    /// Downloads and installs a patch for the specified game.
    /// The patch file is downloaded to the appropriate patch folder based on the game's Xenia version.
    /// </summary>
    /// <param name="game">The game to download the patch for.</param>
    /// <param name="patch">The patch information containing download URL and metadata.</param>
    /// <returns>A task representing the asynchronous operation. The task completes when the patch has been downloaded and the library saved.</returns>
    /// <exception cref="IOException">Thrown when the patch file cannot be written to the patch folder.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access to the patch folder is denied.</exception>
    public static async Task DownloadPatchAsync(Game game, PatchInfo patch)
    {
        Logger.Trace<PatchManager>($"Starting DownloadPatchAsync operation for game: '{game.Title}', patch: '{patch.Name}'");
        Logger.Debug<PatchManager>($"Game Xenia version: {game.XeniaVersion}, Patch download URL: {patch.DownloadUrl}");

        // Get the patch folder location for the game's Xenia version
        string patchFolderLocation = XeniaVersionInfo.GetXeniaVersionInfo(game.XeniaVersion).PatchFolderLocation;
        Logger.Debug<PatchManager>($"Patch folder location for {game.XeniaVersion}: {patchFolderLocation}");

        try
        {
            // Validate patch has required information
            if (patch.DownloadUrl == null)
            {
                Logger.Error<PatchManager>($"Patch download URL is null for patch: '{patch.Name}'");
                throw new InvalidOperationException($"Patch '{patch.Name}' has no download URL");
            }

            if (string.IsNullOrWhiteSpace(patch.Name))
            {
                Logger.Error<PatchManager>($"Patch name is null or empty");
                throw new InvalidOperationException($"Patch has no valid name");
            }

            // Resolve the full path for the patch directory and file
            string patchDirectory = AppPathResolver.GetFullPath(patchFolderLocation);
            string patchFilePath = Path.Combine(patchDirectory, patch.Name);
            Logger.Info<PatchManager>($"Downloading patch '{patch.Name}' from {patch.DownloadUrl} to {patchFilePath}");

            // Ensure the patch directory exists
            Logger.Debug<PatchManager>($"Creating patch directory if it doesn't exist: {patchDirectory}");
            DirectoryInfo dirInfo = Directory.CreateDirectory(patchDirectory);
            Logger.Debug<PatchManager>($"Patch directory created/verified: {dirInfo.FullName}, Attributes: {dirInfo.Attributes}");

            // Download the patch file
            Logger.Debug<PatchManager>($"Initiating download via DownloadManager");
            await _downloadManager.DownloadFileAsync(patch.DownloadUrl, patchFilePath);
            Logger.Info<PatchManager>($"Successfully downloaded patch '{patch.Name}' to {patchFilePath}");

            // Save the game library to persist changes
            Logger.Debug<PatchManager>($"Saving game library to persist patch installation");
            game.FileLocations.Patch = Path.Combine(patchFolderLocation, patch.Name);
            GameManager.SaveLibrary();
            Logger.Info<PatchManager>($"Game library saved successfully");

            Logger.Trace<PatchManager>($"DownloadPatchAsync operation completed successfully for patch: '{patch.Name}'");
        }
        catch (IOException ioEx)
        {
            Logger.Error<PatchManager>($"IO error while downloading patch '{patch.Name}': {ioEx.Message}");
            Logger.LogExceptionDetails<PatchManager>(ioEx);
            throw;
        }
        catch (UnauthorizedAccessException unauthEx)
        {
            Logger.Error<PatchManager>($"Access denied while downloading patch '{patch.Name}': {unauthEx.Message}");
            Logger.LogExceptionDetails<PatchManager>(unauthEx);
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error<PatchManager>($"Unexpected error while downloading patch '{patch.Name}': {ex.Message}");
            Logger.LogExceptionDetails<PatchManager>(ex);
            throw;
        }
    }

    /// <summary>
    /// Installs a local patch file for the specified game.
    /// The patch file is copied to the appropriate patch folder based on the game's Xenia version.
    /// </summary>
    /// <param name="game">The game to install the patch for.</param>
    /// <param name="patchFileLocation">The local file path of the patch file to install.</param>
    /// <returns>A task representing the asynchronous operation. The task completes when the patch has been installed and the library saved.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the source patch file does not exist.</exception>
    /// <exception cref="IOException">Thrown when the patch file cannot be copied to the patch folder.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access to the patch folder is denied.</exception>
    public static async Task InstallLocalPatchAsync(Game game, string patchFileLocation)
    {
        Logger.Trace<PatchManager>($"Starting InstallLocalPatchAsync operation for game: '{game.Title}', patch file: '{patchFileLocation}'");

        await Task.Run(() =>
        {
            try
            {
                // Verify the source file exists
                if (!File.Exists(patchFileLocation))
                {
                    Logger.Error<PatchManager>($"Source patch file does not exist: '{patchFileLocation}'");
                    throw new FileNotFoundException($"Patch file not found: {patchFileLocation}");
                }

                // Generate the destination filename using game ID and title
                string destinationFileName = $"{game.GameId} - {game.Title}.patch.toml";
                string destinationLocation = Path.Combine(
                    XeniaVersionInfo.GetXeniaVersionInfo(game.XeniaVersion).PatchFolderLocation,
                    destinationFileName);
                string resolvedDestinationPath = AppPathResolver.GetFullPath(destinationLocation);

                Logger.Debug<PatchManager>($"Copying patch file from '{patchFileLocation}' to '{resolvedDestinationPath}'");

                // Ensure the destination directory exists
                string destinationDirectory = Path.GetDirectoryName(resolvedDestinationPath)!;
                Logger.Debug<PatchManager>($"Creating destination directory if it doesn't exist: {destinationDirectory}");
                Directory.CreateDirectory(destinationDirectory);

                // Copy the patch file (overwrite if exists)
                File.Copy(patchFileLocation, resolvedDestinationPath, true);
                Logger.Info<PatchManager>($"Successfully copied patch file to '{resolvedDestinationPath}'");

                // Update the game's patch location and save the library
                game.FileLocations.Patch = destinationLocation;
                GameManager.SaveLibrary();
                Logger.Info<PatchManager>($"Game library updated with patch location: '{destinationLocation}'");

                Logger.Trace<PatchManager>($"InstallLocalPatchAsync operation completed successfully for patch: '{destinationFileName}'");
            }
            catch (IOException ioEx)
            {
                Logger.Error<PatchManager>($"IO error while installing local patch: {ioEx.Message}");
                Logger.LogExceptionDetails<PatchManager>(ioEx);
                throw;
            }
            catch (UnauthorizedAccessException unauthEx)
            {
                Logger.Error<PatchManager>($"Access denied while installing local patch: {unauthEx.Message}");
                Logger.LogExceptionDetails<PatchManager>(unauthEx);
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error<PatchManager>($"Unexpected error while installing local patch: {ex.Message}");
                Logger.LogExceptionDetails<PatchManager>(ex);
                throw;
            }
        });
    }

    /// <summary>
    /// Removes the installed patch for the specified game.
    /// The patch file is deleted from the patch folder, and the game's patch location is cleared.
    /// </summary>
    /// <param name="game">The game to remove the patch from.</param>
    /// <returns>A task representing the asynchronous operation. The task completes when the patch has been removed and the library saved.</returns>
    /// <exception cref="IOException">Thrown when the patch file cannot be deleted.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access to the patch file is denied.</exception>
    public static async Task RemovePatchAsync(Game game)
    {
        Logger.Trace<PatchManager>($"Starting RemovePatchAsync operation for game: '{game.Title}'");

        // Check if the game has a patch installed
        if (game.FileLocations.Patch == null)
        {
            Logger.Debug<PatchManager>($"No patch installed for game: '{game.Title}', skipping removal");
            return;
        }

        try
        {
            // Resolve the full path to the patch file
            string patchFilePath = AppPathResolver.GetFullPath(game.FileLocations.Patch);
            Logger.Debug<PatchManager>($"Removing patch file at: '{patchFilePath}'");

            // Delete the patch file if it exists
            if (File.Exists(patchFilePath))
            {
                File.Delete(patchFilePath);
                Logger.Info<PatchManager>($"Successfully deleted patch file: '{patchFilePath}'");
            }
            else
            {
                Logger.Warning<PatchManager>($"Patch file does not exist: '{patchFilePath}'");
            }

            // Clear the game's patch location and save the library
            game.FileLocations.Patch = null;
            GameManager.SaveLibrary();
            Logger.Info<PatchManager>($"Game library updated, patch location cleared for: '{game.Title}'");

            Logger.Trace<PatchManager>($"RemovePatchAsync operation completed successfully for game: '{game.Title}'");
        }
        catch (IOException ioEx)
        {
            Logger.Error<PatchManager>($"IO error while removing patch: {ioEx.Message}");
            Logger.LogExceptionDetails<PatchManager>(ioEx);
            throw;
        }
        catch (UnauthorizedAccessException unauthEx)
        {
            Logger.Error<PatchManager>($"Access denied while removing patch: {unauthEx.Message}");
            Logger.LogExceptionDetails<PatchManager>(unauthEx);
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error<PatchManager>($"Unexpected error while removing patch: {ex.Message}");
            Logger.LogExceptionDetails<PatchManager>(ex);
            throw;
        }

        await Task.CompletedTask;
    }
}