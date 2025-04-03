using System.IO.Compression;

// Imported
using ImageMagick;
using XeniaManager.Core.Game;

namespace XeniaManager.Core.Downloader;

/// <summary>
/// Provides functionality for downloading files, reporting progress,
/// extracting ZIP files, and checking URL availability.
/// </summary>
public class DownloadManager
{
    // Variables
    /// <summary>
    /// A shared HttpClient instance.
    /// </summary>
    private static readonly HttpClient _httpClient = new HttpClient{ Timeout = TimeSpan.FromSeconds(15) };

    /// <summary>
    /// Raised when download progress changes. The integer parameter indicates percentage (0-100).
    /// </summary>
    public event Action<int>? ProgressChanged;
    
    // Constructor
    /// <summary>
    /// Initializes the DownloadManager and sets up the HTTP client's default headers.
    /// </summary>
    public DownloadManager()
    {
        // Set the User-Agent header once for all requests.
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
        }
        
        // Ensure the default directory exists
        Directory.CreateDirectory(Constants.DownloadDir);
    }

    // Functions
    /// <summary>
    /// Updates the progress event based on the number of downloaded bytes.
    /// </summary>
    /// <param name="downloadedBytes">The number of bytes downloaded so far.</param>
    /// <param name="totalBytes">The total number of bytes to download.</param>
    private void UpdateProgress(long downloadedBytes, long totalBytes)
    {
        if (totalBytes > 0)
        {
            int progress = (int)Math.Round((double)downloadedBytes / totalBytes * 100);
            ProgressChanged?.Invoke(progress);
        }
    }

    /// <summary>
    /// Extracts a ZIP file to the specified directory.
    /// </summary>
    /// <param name="zipFilePath">The path of the ZIP file to extract.</param>
    /// <param name="extractPath">The directory where files will be extracted.</param>
    public void ExtractZipFile(string zipFilePath, string extractPath)
    {
        try
        {
            // Ensure the extraction directory exists.
            Directory.CreateDirectory(extractPath);
            ZipFile.ExtractToDirectory(zipFilePath, extractPath, overwriteFiles: true);
        }
        catch (Exception ex)
        {
            Logger.Error($"An error occurred during extraction: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Checks if the URL works and optionally verifies that the returned media type starts with the specified value.
    /// </summary>
    /// <param name="url">The URL to check.</param>
    /// <param name="mediaType">The expected media type prefix (default is "application").</param>
    /// <returns>True if the URL is reachable and matches the media type; otherwise, false.</returns>
    public async Task<bool> CheckIfUrlWorksAsync(string url, string mediaType = "application")
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string? contentType = response.Content.Headers.ContentType?.MediaType;
            return !string.IsNullOrEmpty(contentType) && contentType.StartsWith(mediaType, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error checking URL: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Downloads a file asynchronously from the specified URL to the given local path.
    /// Reports progress via the ProgressChanged event.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
    /// <param name="savePath">The full path where the file will be saved.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the download.</param>
    public async Task DownloadFileAsync(string url, string savePath, CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            long totalBytes = response.Content.Headers.ContentLength ?? -1L;
            long downloadedBytes = 0;

            await using FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                downloadedBytes += bytesRead;
                UpdateProgress(downloadedBytes, totalBytes);
            }
        }
        catch (TaskCanceledException)
        {
            Logger.Warning("Download was cancelled.");
            throw;
        }
        catch (HttpRequestException hrex)
        {
            Logger.Error($"HTTP error during download: {hrex}");
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error($"An error occurred during download: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Downloads a file from the given URL, extracts it if it is a ZIP archive,
    /// and then cleans up the downloaded file.
    /// </summary>
    /// <param name="downloadUrl">The URL to download from.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="extractPath">The directory to extract the file if it is a ZIP.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    public async Task DownloadAndExtractAsync(string downloadUrl, string fileName, string extractPath, CancellationToken cancellationToken = default)
    {
        string downloadPath = Path.Combine(Constants.DownloadDir, fileName);

        try
        {
            Logger.Info($"Starting download of {Path.GetFileName(downloadPath)} from {downloadUrl}");
            await DownloadFileAsync(downloadUrl, downloadPath, cancellationToken);
            Logger.Info("Download completed");

            // If the downloaded file is a ZIP file, perform extraction.
            if (Path.GetExtension(downloadPath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Info($"Extracting the ZIP file to {extractPath}");
                ExtractZipFile(downloadPath, extractPath);
                Logger.Info("Extraction completed");
            }
            else
            {
                Logger.Info("Downloaded file is not a ZIP archive. Extraction skipped.");
            }

            Logger.Info("Cleaning up downloaded file");
            if (File.Exists(downloadPath))
            {
                File.Delete(downloadPath);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"An error occurred in DownloadAndExtractAsync: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Downloads the artwork from the url
    /// </summary>
    /// <param name="url">Url of the artwork</param>
    /// <param name="savePath">Where to save the artwork</param>
    /// <param name="format">Format of the output</param>
    /// <param name="width">Width of the artwork</param>
    /// <param name="height">Height of the artwork</param>
    public async Task DownloadArtwork(string url, string savePath, MagickFormat format = MagickFormat.Ico, uint width = 150, uint height = 207)
    {
        ArtworkManager.ConvertArtwork(await _httpClient.GetByteArrayAsync(url), savePath, format, width, height);
    }
}