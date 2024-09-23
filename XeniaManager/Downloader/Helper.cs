using System;
using System.IO.Compression;

// Imported
using Serilog;

namespace XeniaManager.Downloader
{
    public static partial class DownloadManager
    {
        /// <summary>
        /// Extracts a ZIP file to the specified directory.
        /// </summary>
        private static void ExtractZipFile(string zipFilePath, string extractPath)
        {
            ZipFile.ExtractToDirectory(zipFilePath, extractPath, true);
        }

        /// <summary>
        /// Updates the progress based on downloaded bytes.
        /// </summary>
        private static void UpdateProgress(long downloadedBytes, long totalBytes)
        {
            if (totalBytes > 0)
            {
                int progress = (int)Math.Round((double)downloadedBytes / totalBytes * 100);
                ProgressChanged?.Invoke(progress);
            }
        }
    }
}
