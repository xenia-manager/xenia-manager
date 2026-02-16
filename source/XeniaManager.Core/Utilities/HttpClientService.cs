using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// A customized HttpClient service that provides enhanced HTTP request capabilities with connection pooling,
/// proper timeout handling, and standardized User-Agent header for the Xenia Manager application.
/// Implements IDisposable to ensure proper resource cleanup.
/// </summary>
public sealed class HttpClientService : IDisposable
{
    private readonly HttpClient _client;
    private bool _disposed;

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
    /// </summary>
    /// <param name="url">The URL to send the GET request to</param>
    /// <param name="cancellationToken">A cancellation token to cancel the request</param>
    /// <returns>The response body as a string</returns>
    /// <exception cref="HttpRequestException">Thrown when there's an error connecting to the server</exception>
    /// <exception cref="TimeoutException">Thrown when the request times out</exception>
    public async Task<string> GetAsync(string url, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
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