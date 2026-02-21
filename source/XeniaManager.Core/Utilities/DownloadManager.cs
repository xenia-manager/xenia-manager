using SkiaSharp;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Manages file downloads with progress reporting and error handling.
/// </summary>
public sealed class DownloadManager : IDisposable
{
    public readonly string DownloadPath = AppPaths.DownloadsDirectory;
    private readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    private bool _disposed = false;

    /// <summary>
    /// Occurs when the download progress changes.
    /// </summary>
    public event Action<int>? ProgressChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="DownloadManager"/> class.
    /// Sets up the HTTP client with a custom User-Agent and ensures the download directory exists.
    /// </summary>
    public DownloadManager(string? downloadPath = null)
    {
        Logger.Debug<DownloadManager>("Initializing DownloadManager");

        // Set the User-Agent header once for all requests.
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager)");
        }

        if (!string.IsNullOrWhiteSpace(downloadPath))
        {
            DownloadPath = downloadPath;
            Logger.Debug<DownloadManager>($"Setting custom download path: {DownloadPath}");
        }
        else
        {
            Logger.Debug<DownloadManager>($"Using default download path: {DownloadPath}");
        }

        // Ensure the default directory exists
        Directory.CreateDirectory(DownloadPath);
        Logger.Debug<DownloadManager>($"Ensured download directory exists: {DownloadPath}");
    }

    /// <summary>
    /// Downloads a file from the specified URL to the given save path asynchronously.
    /// </summary>
    /// <param name="url">The URL of the file to download.</param>
    /// <param name="fileName">The local path where the file should be saved.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the download operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown when an HTTP error occurs during download.</exception>
    /// <exception cref="TaskCanceledException">Thrown when the download is canceled.</exception>
    /// <exception cref="ArgumentException">Thrown when url or fileName is null or empty.</exception>
    public async Task DownloadFileAsync(string url, string fileName, CancellationToken cancellationToken = default)
    {
        // Validate inputs before combining paths
        if (string.IsNullOrWhiteSpace(url))
        {
            Logger.Error<DownloadManager>($"Invalid URL provided for download: {url}");
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            Logger.Error<DownloadManager>($"Invalid save path provided for download: {fileName}");
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        string fullPath = Path.Combine(DownloadPath, fileName);

        Logger.Info<DownloadManager>($"Starting download from '{url}' to '{fileName}'");

        // Create the directory for the save path if it doesn't exist
        string? directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Logger.Debug<DownloadManager>($"Created directory: {directory}");
        }

        try
        {
            Logger.Debug<DownloadManager>($"Initiating HTTP GET request to {url}");

            using HttpResponseMessage response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            Logger.Debug<DownloadManager>($"Received HTTP response: Status Code {response.StatusCode}, Content Length: {response.Content.Headers.ContentLength ?? -1L} bytes");

            response.EnsureSuccessStatusCode();
            long totalBytes = response.Content.Headers.ContentLength ?? -1L;
            long downloadedBytes = 0;

            Logger.Debug<DownloadManager>($"Creating file stream for {fileName}");
            await using FileStream fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 8192, useAsync: true);

            Logger.Debug<DownloadManager>("Opening content stream from HTTP response");
            await using Stream contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

            byte[] buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                downloadedBytes += bytesRead;
                UpdateProgress(downloadedBytes, totalBytes);
            }

            // Ensure the stream is properly flushed
            await fileStream.FlushAsync(cancellationToken);
            Logger.Debug<DownloadManager>($"File stream flushed successfully");

            // Report 100% completion only on successful download
            ProgressChanged?.Invoke(100);

            Logger.Info<DownloadManager>($"Successfully downloaded file from {url} to {fullPath}. Total size: {downloadedBytes} bytes");
        }
        catch (TaskCanceledException)
        {
            Logger.Warning<DownloadManager>($"Download was cancelled for URL: {url}");
            throw;
        }
        catch (HttpRequestException hrex)
        {
            Logger.Error<DownloadManager>($"HTTP error during download from {url}: Status Code {hrex.StatusCode}, Message: {hrex.Message}");
            throw;
        }
        catch (IOException ioex)
        {
            Logger.Error<DownloadManager>($"IO error during download to {fullPath}: {ioex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error<DownloadManager>($"An unexpected error occurred during download from {url} to {fullPath}: {ex.Message}");
            Logger.LogExceptionDetails<DownloadManager>(ex);
            throw;
        }
    }

    /// <summary>
    /// Downloads a file from the specified array of URLs to the given save path asynchronously.
    /// Tries each URL in sequence until one succeeds or all fail, with a 15-second timeout for each attempt.
    /// </summary>
    /// <param name="urls">Array of URLs to try for downloading the file, in order of preference.</param>
    /// <param name="fileName">The local path where the file should be saved.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the download operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">Thrown when an HTTP error occurs during download.</exception>
    /// <exception cref="TaskCanceledException">Thrown when the download is canceled.</exception>
    /// <exception cref="ArgumentException">Thrown when the URLs array is null or empty.</exception>
    public async Task DownloadFileFromMultipleUrlsAsync(string[] urls, string fileName, CancellationToken cancellationToken = default)
    {
        if (urls is null || urls.Length == 0)
        {
            Logger.Error<DownloadManager>("URLs array is null or empty");
            throw new ArgumentException("URLs array cannot be null or empty.", nameof(urls));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            Logger.Error<DownloadManager>("Invalid file name provided");
            throw new ArgumentException("File name cannot be null or empty.", nameof(fileName));
        }

        Directory.CreateDirectory(DownloadPath);
        Logger.Info<DownloadManager>($"Starting download with {urls.Length} fallback URLs. Target file: '{fileName}'");

        foreach (string rawUrl in urls)
        {
            if (string.IsNullOrWhiteSpace(rawUrl))
            {
                Logger.Warning<DownloadManager>("Skipping empty URL entry");
                continue;
            }

            string url = rawUrl.Trim();
            Logger.Debug<DownloadManager>($"Attempting download from: {url}");

            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));

            try
            {
                await DownloadFileAsync(url, fileName, timeoutCts.Token);
                return; // Successful file download
            }
            catch (TaskCanceledException)
            {
                Logger.Warning<DownloadManager>($"Download cancelled or timed out for URL: {url}");
                throw;
            }
            catch (HttpRequestException ex)
            {
                Logger.Warning<DownloadManager>($"HTTP error for {url}: {ex.StatusCode} - {ex.Message}");
            }
            catch (IOException ex)
            {
                Logger.Warning<DownloadManager>($"IO error for {url}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Warning<DownloadManager>($"Unexpected error for {url}: {ex.Message}");
            }
        }

        // Failed download
        Logger.Error<DownloadManager>($"All {urls.Length} download attempts failed for file '{fileName}'");
        throw new HttpRequestException($"Failed to download file from any of the provided {urls.Length} URLs.");
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
            Logger.Error<DownloadManager>($"Error checking URL.");
            Logger.LogExceptionDetails<DownloadManager>(ex);
            return false;
        }
    }

    /// <summary>
    /// Downloads the artwork from the url
    /// </summary>
    /// <param name="url">Url of the artwork</param>
    /// <param name="savePath">Where to save the artwork</param>
    /// <param name="format">Format of the output</param>
    public async Task DownloadArtwork(string url, string savePath, SKEncodedImageFormat format = SKEncodedImageFormat.Ico)
    {
        if (format == SKEncodedImageFormat.Ico)
        {
            ArtworkManager.ConvertToIcon(await _httpClient.GetByteArrayAsync(url), savePath);
        }
        else
        {
            ArtworkManager.ConvertArtwork(await _httpClient.GetByteArrayAsync(url), savePath, format);
        }
    }

    /// <summary>
    /// Updates the download progress by calculating the percentage based on downloaded and total bytes.
    /// </summary>
    /// <param name="downloadedBytes">The number of bytes downloaded so far.</param>
    /// <param name="totalBytes">The total number of bytes to download, or -1 if unknown.</param>
    private void UpdateProgress(long downloadedBytes, long totalBytes)
    {
        if (totalBytes > 0)
        {
            int progress = (int)Math.Round((double)downloadedBytes / totalBytes * 100);
            Logger.Trace<DownloadManager>($"Download progress: {downloadedBytes}/{totalBytes} bytes");
            ProgressChanged?.Invoke(progress);
        }
    }

    /// <summary>
    /// Disposes of the resources used by the DownloadManager.
    /// </summary>
    public void Dispose()
    {
        Logger.Debug<DownloadManager>("Disposing DownloadManager");
        Dispose(true);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the DownloadManager and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    private void Dispose(bool disposing)
    {
        if (_disposed || !disposing)
        {
            return;
        }
        _httpClient.Dispose();
        _disposed = true;
        Logger.Debug<DownloadManager>("HttpClient disposed and DownloadManager marked as disposed");
    }
}