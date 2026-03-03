using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Provides utility methods for storage-related operations such as copying directories.
/// </summary>
public class StorageUtilities
{
    /// <summary>
    /// Tracks whether we are inside a recursive call to suppress duplicate log messages.
    /// ThreadLocal so concurrent copies on different threads don't interfere.
    /// </summary>
    private static readonly ThreadLocal<bool> _isRecursing = new ThreadLocal<bool>(() => false);

    /// <summary>
    /// Copies all files and optionally subdirectories from a source directory
    /// to a destination directory.
    /// </summary>
    /// <param name="sourceDir">The path of the source directory to copy from.</param>
    /// <param name="destinationDir">The path of the destination directory to copy to.</param>
    /// <param name="recursive">
    /// True to copy subdirectories and their contents recursively;
    /// false to copy only files in the root directory.
    /// </param>
    /// <param name="overwrite">Whether to overwrite existing files. Defaults to true.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when source or destination is null/empty, or destination is inside the source.
    /// </exception>
    /// <exception cref="DirectoryNotFoundException">
    /// Thrown when the source directory does not exist.
    /// </exception>
    public static void CopyDirectory(string sourceDir, string destinationDir,
        bool recursive, bool overwrite = true, CancellationToken cancellationToken = default)
    {
        // Input validation
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDir);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDir);

        // Resolve to full paths at once so every comparison is consistent
        string fullSource = Path.GetFullPath(sourceDir);
        string fullDestination = Path.GetFullPath(destinationDir);
        Logger.Trace<StorageUtilities>($"Resolved paths — source: '{fullSource}', destination: '{fullDestination}'");

        // Guard against copying a directory into itself (infinite recursion)
        if (fullDestination.StartsWith(fullSource + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Destination directory cannot be inside the source directory.");
        }

        // Only log at the top-level call, not every recursive invocation
        Logger.Trace<StorageUtilities>($"Copying directory from '{fullSource}' to '{fullDestination}' " +
                                       $"(recursive: {recursive})");

        // Source Directory Check
        DirectoryInfo dir = new DirectoryInfo(fullSource);
        if (!dir.Exists)
        {
            Logger.Error<StorageUtilities>($"Source directory not found: {dir.FullName}");
            throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
        }

        // Create Destination
        Logger.Trace<StorageUtilities>($"Ensuring destination directory exists: '{fullDestination}'");
        Directory.CreateDirectory(fullDestination);

        // Copy files with lazy enumeration
        foreach (FileInfo file in dir.EnumerateFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();

            string targetFilePath = Path.Combine(fullDestination, file.Name);

            try
            {
                Logger.Trace<StorageUtilities>($"Copying file '{file.FullName}' to '{targetFilePath}'");
                file.CopyTo(targetFilePath, overwrite);
            }
            catch (IOException ex) when (!overwrite)
            {
                // File already exists, and overwrite is disabled — skip
                Logger.Warning<StorageUtilities>($"Skipped existing file: {targetFilePath} ({ex.Message})");
            }
        }

        // Recurse into subdirectories
        if (recursive)
        {
            try
            {
                foreach (DirectoryInfo subDir in dir.EnumerateDirectories())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string newDestination = Path.Combine(fullDestination, subDir.Name);
                    Logger.Trace<StorageUtilities>($"Entering subdirectory '{subDir.FullName}'");

                    CopyDirectory(subDir.FullName, newDestination, recursive: true, overwrite, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                // ignored
                Logger.LogExceptionDetails<StorageUtilities>(ex);
            }
        }

        Logger.Info<StorageUtilities>($"Successfully copied directory from " +
                                      $"'{fullSource}' to '{fullDestination}'");
    }
}