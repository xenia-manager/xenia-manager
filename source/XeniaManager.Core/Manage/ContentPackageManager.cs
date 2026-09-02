using XeniaManager.Logging;

namespace XeniaManager.Core.Manage;

/// <summary>
/// Installs STFS content packages into Xenia's content directory as package files.
/// Package files keep their original form (metadata stays embedded), which requires
/// a Xenia build with XContent package support.
/// </summary>
public class ContentPackageManager
{
    /// <summary>
    /// The size of a single copy chunk (1 MiB).
    /// </summary>
    private const int CopyChunkSize = 1024 * 1024;

    /// <summary>
    /// Installs an STFS package file into Xenia's content directory by copying it to:
    /// outputDirectory/TitleId/ContentType/[original file name]
    /// </summary>
    /// <param name="packageFilePath">Path to the STFS package file to install.</param>
    /// <param name="outputDirectory">The content directory of the target XUID (e.g. ContentFolder/0000000000000000).</param>
    /// <param name="titleIdHex">Title ID in hex string format (e.g. "4D5309C9").</param>
    /// <param name="contentTypeHex">Content type in hex string format (e.g. "00009000").</param>
    /// <param name="progressCallback">Optional callback reporting (bytes copied, total bytes).</param>
    public static void InstallPackageAsFile(string packageFilePath, string outputDirectory, string titleIdHex, string contentTypeHex,
        Action<long, long>? progressCallback = null)
    {
        Logger.Debug<ContentPackageManager>($"Installing package file: {packageFilePath}");

        if (!File.Exists(packageFilePath))
        {
            Logger.Error<ContentPackageManager>($"Package file does not exist: {packageFilePath}");
            throw new FileNotFoundException($"Package file does not exist at {packageFilePath}", packageFilePath);
        }

        // Create the content type folder: outputDirectory/TitleId/ContentType/
        string contentTypeFolderPath = Path.Combine(outputDirectory, titleIdHex.ToUpperInvariant(), contentTypeHex.ToUpperInvariant());
        Directory.CreateDirectory(contentTypeFolderPath);

        string destinationPath = Path.Combine(contentTypeFolderPath, Path.GetFileName(packageFilePath));
        long totalBytes = new FileInfo(packageFilePath).Length;
        Logger.Info<ContentPackageManager>($"Copying {totalBytes} bytes to {destinationPath}");

        long copiedBytes = 0;
        using (FileStream source = new FileStream(packageFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (FileStream destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            byte[] buffer = new byte[CopyChunkSize];
            int bytesRead;
            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                destination.Write(buffer, 0, bytesRead);
                copiedBytes += bytesRead;
                progressCallback?.Invoke(copiedBytes, totalBytes);
            }
        }

        if (copiedBytes != totalBytes)
        {
            Logger.Error<ContentPackageManager>($"Incomplete copy: {copiedBytes} out of {totalBytes} bytes written to {destinationPath}");
            throw new IOException($"Failed to copy the whole package file ({copiedBytes} out of {totalBytes} bytes)");
        }

        Logger.Info<ContentPackageManager>($"Successfully installed package file to {destinationPath} ({copiedBytes} bytes)");
    }
}