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
        public static void ExtractZipFile(string zipFilePath, string extractPath)
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

        /// <summary>
        /// Used to check if the URL is working
        /// </summary>
        /// <param name="url"></param>
        /// <returns>True if the URL works, otherwise false</returns>
        public static async Task<bool> CheckIfURLWorks(string url, string mediaType)
        {
            using (HttpClient client = new HttpClient())
            {
                //client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.Content.Headers.ContentType.MediaType.StartsWith(mediaType))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (HttpRequestException)
                {
                    return false; // URL is not reachable
                }
            }
        }
    }
}
