using XeniaManager.Core.Constants;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// A customized HttpClient service that provides enhanced HTTP request capabilities with connection pooling,
/// proper timeout handling, standardized User-Agent header, and response caching.
/// Implements IDisposable to ensure proper resource cleanup.
/// </summary>
public sealed class HttpClientService : IDisposable
{
    private readonly HttpClient _client;
    private bool _disposed;

    /// <summary>
    /// Default cache duration for HTTP responses (1 day)
    /// </summary>
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromDays(1);

    /// <summary>
    /// Initializes a new instance of the HttpClientService with optional timeout configuration
    /// </summary>
    /// <param name="timeout">Optional timeout duration for HTTP requests. Defaults to 15 seconds if not specified.</param>
    public HttpClientService(TimeSpan? timeout = null)
    {
        Logger.Info<HttpClientService>("Initializing HttpClientService with connection pooling and timeout configuration");

        // Use SocketsHttpHandler for connection pooling and to avoid socket exhaustion
        SocketsHttpHandler handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 10
        };

        _client = new HttpClient(handler);
        _client.DefaultRequestHeaders.Add("User-Agent",
            "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
        _client.Timeout = timeout ?? TimeSpan.FromSeconds(15);

        Logger.Debug<HttpClientService>($"HttpClientService initialized with timeout: {_client.Timeout}");
    }

    /// <summary>
    /// Sends a GET request to the specified URL and returns the response body as a string.
    /// Supports optional caching with configurable duration.
    /// </summary>
    /// <param name="url">The URL to send the GET request to</param>
    /// <param name="cancellationToken">A cancellation token to cancel the request</param>
    /// <param name="cacheKey">Optional cache key. If provided, a response will be cached and reused if not expired</param>
    /// <param name="cacheDuration">Optional cache duration. Defaults to 1 day if not specified</param>
    /// <returns>The response body as a string</returns>
    /// <exception cref="HttpRequestException">Thrown when there's an error connecting to the server</exception>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    public async Task<string> GetAsync(string url, CancellationToken cancellationToken = default, string? cacheKey = null, TimeSpan? cacheDuration = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // If the cache key is provided, try to use cache
        if (cacheKey != null)
        {
            TimeSpan duration = cacheDuration ?? DefaultCacheDuration;
            string cacheFile = Path.Combine(AppPaths.X360DataBaseCacheDirectory, $"{cacheKey}.json");

            if (TryReadCache(cacheFile, duration, out string? cachedContent))
            {
                Logger.Info<HttpClientService>($"Cache hit for {cacheKey}: {cacheFile}");
                return cachedContent!;
            }

            // Cache missing or expired - fetch fresh and cache it
            Logger.Info<HttpClientService>($"Cache miss or expired for {cacheKey}, fetching fresh data");
            string freshData = await GetAsyncInternal(url, cancellationToken);
            SaveCache(cacheFile, freshData);
            Logger.Info<HttpClientService>($"Cached fresh data for {cacheKey} to {cacheFile}");
            return freshData;
        }

        // No caching - direct request
        return await GetAsyncInternal(url, cancellationToken);
    }

    /// <summary>
    /// Internal GET request implementation without caching logic
    /// </summary>
    private async Task<string> GetAsyncInternal(string url, CancellationToken cancellationToken)
    {
        Logger.Info<HttpClientService>($"Sending GET request to URL: {url}");

        try
        {
            using HttpResponseMessage response = await _client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            Logger.Debug<HttpClientService>($"GET request to {url} completed successfully with status code: {response.StatusCode}");
            return responseBody;
        }
        catch (HttpRequestException ex)
        {
            Logger.Error<HttpClientService>($"Error connecting to the server. Check your connection or the URL.\n{ex}");
            throw; // Preserves original stack trace
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Only a timeout if the caller didn't cancel
            Logger.Error<HttpClientService>($"The request timed out");
            Logger.LogExceptionDetails<HttpClientService>(ex);
            throw new TimeoutException($"The request to '{url}' timed out.", ex);
        }
        catch (TaskCanceledException)
        {
            // Caller-requested cancellation — just rethrow, not an error
            Logger.Debug<HttpClientService>($"Request to {url} was cancelled by caller");
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error<HttpClientService>($"An unexpected error occurred during the GET request.\n{ex}");
            throw;
        }
    }

    /// <summary>
    /// Attempts to read a cached response if it exists and is not expired.
    /// </summary>
    /// <param name="cacheFile">Path to the cache file</param>
    /// <param name="cacheDuration">Maximum age of cached data</param>
    /// <param name="content">Output parameter for cached content if successful</param>
    /// <returns>True if the cache hit and not expired, false otherwise</returns>
    private bool TryReadCache(string cacheFile, TimeSpan cacheDuration, out string? content)
    {
        content = null;

        if (!File.Exists(cacheFile))
        {
            Logger.Debug<HttpClientService>($"Cache file does not exist: {cacheFile}");
            return false;
        }

        try
        {
            FileInfo fileInfo = new FileInfo(cacheFile);
            TimeSpan age = DateTime.UtcNow - fileInfo.LastWriteTimeUtc;

            if (age > cacheDuration)
            {
                Logger.Debug<HttpClientService>($"Cache expired for {cacheFile} (age: {age.TotalHours:F1}h, max: {cacheDuration.TotalHours:F1}h)");
                return false;
            }

            content = File.ReadAllText(cacheFile);
            Logger.Debug<HttpClientService>($"Cache hit for {cacheFile} (age: {age.TotalMinutes:F1}m)");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warning<HttpClientService>($"Failed to read cache file {cacheFile}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Saves content to the cache file.
    /// </summary>
    /// <param name="cacheFile">Path to the cache file</param>
    /// <param name="content">Content to cache</param>
    private void SaveCache(string cacheFile, string content)
    {
        try
        {
            // Ensure the cache directory exists
            Directory.CreateDirectory(AppPaths.X360DataBaseCacheDirectory);
            File.WriteAllText(cacheFile, content);
            Logger.Debug<HttpClientService>($"Successfully saved cache to {cacheFile}");
        }
        catch (Exception ex)
        {
            Logger.Warning<HttpClientService>($"Failed to save cache file {cacheFile}: {ex.Message}");
        }
    }

    /// <summary>
    /// Disposes of the HttpClient resources to prevent memory leaks
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            Logger.Debug<HttpClientService>("HttpClientService already disposed");
            return;
        }

        Logger.Info<HttpClientService>("Disposing HttpClientService and cleaning up resources");
        _client.Dispose();
        _disposed = true;
        Logger.Debug<HttpClientService>("HttpClientService disposed successfully");
    }
}