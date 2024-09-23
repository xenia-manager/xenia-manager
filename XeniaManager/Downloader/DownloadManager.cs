using System;

// Imported
using Serilog;

namespace XeniaManager.Downloader
{
    public static partial class DownloadManager
    {
        /// <summary>
        /// Event to report progress
        /// </summary>
        public static event Action<int> ProgressChanged;

        /// <summary>
        /// Downloads a file from a specified URL and extracts it.
        /// </summary>
        public static async Task DownloadAndExtractAsync(string downloadUrl, string downloadPath, string extractPath)
        {
            try
            {
                Log.Information($"Downloading {Path.GetFileName(downloadPath)}");
                await DownloadFileAsync(downloadUrl, downloadPath);
                Log.Information("Download completed");
                Log.Information($"Extracting the zip file into {extractPath}");
                ExtractZipFile(downloadPath, extractPath);
                Log.Information("Extraction done");
                Log.Information("Cleaning up");
                if (File.Exists(downloadPath))
                {
                    File.Delete(downloadPath);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Downloads a file from the specified URL to the specified path.
        /// </summary>
        public static async Task DownloadFileAsync(string url, string savePath)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                using (HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    long totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    long downloadedBytes = 0L;

                    using (FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        var buffer = new byte[8192];
                        int bytesRead;
                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                            downloadedBytes += bytesRead;
                            UpdateProgress(downloadedBytes, totalBytes);
                        }
                    }
                }
            }
        }
    }
}
