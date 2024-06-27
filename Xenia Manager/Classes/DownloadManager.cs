using System;
using System.IO.Compression;
using System.IO;
using System.Net.Http;
using System.Windows.Controls;

// Imported
using Serilog;

namespace Xenia_Manager.Classes
{
    public class DownloadManager
    {
        public ProgressBar? progressBar;
        public string? downloadUrl;
        public string? downloadPath;

        public DownloadManager()
        {

        }

        public DownloadManager(ProgressBar? progressBar, string downloadUrl, string downloadPath)
        {
            this.progressBar = progressBar;
            this.downloadUrl = downloadUrl;
            this.downloadPath = downloadPath;
        }

        /// <summary>
        /// Downloads a file from a specified URL and extracts it.
        /// </summary>
        public async Task DownloadAndExtractAsync()
        {
            try
            {
                await DownloadFileAsync(downloadUrl, downloadPath);
                Log.Information("Download completed");
                Log.Information("Extracting the zip file");
                ExtractZipFile(downloadPath, Path.GetDirectoryName(downloadPath) + @"\Xenia\");
                Log.Information("Extraction done");
                DeleteFile(downloadPath);
                Log.Information("Deleting the zip file");
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Downloads a file from the specified URL to the specified path, and updates the progress bar.
        /// </summary>
        private async Task DownloadFileAsync(string url, string savePath)
        {
            using (HttpClient httpClient = new HttpClient())
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

        /// <summary>
        /// Extracts a ZIP file to the specified directory.
        /// </summary>
        private void ExtractZipFile(string zipFilePath, string extractPath)
        {
            ZipFile.ExtractToDirectory(zipFilePath, extractPath, true);
        }

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        private void DeleteFile(string filePath)
        {
            File.Delete(filePath);
        }

        /// <summary>
        /// Updates the progress bar with the current download progress.
        /// </summary>
        private void UpdateProgress(long downloadedBytes, long totalBytes)
        {
            if (progressBar != null && totalBytes > 0)
            {
                int progress = (int)Math.Round((double)downloadedBytes / totalBytes * 100);
                UpdateProgressBar(progress);
            }
        }

        /// <summary>
        /// Updates the progress bar safely, checking for UI thread access.
        /// </summary>
        private void UpdateProgressBar(int progress)
        {
            if (progressBar.Dispatcher.CheckAccess())
            {
                progressBar.Value = progress;
            }
            else
            {
                progressBar.Dispatcher.Invoke(() => progressBar.Value = progress);
            }
        }
    }
}
