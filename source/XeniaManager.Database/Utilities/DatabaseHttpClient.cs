using XeniaManager.Logging;

namespace XeniaManager.Database.Utilities;

/// <summary>
/// Lightweight HttpClient with file-cache support for database fetching.
/// Mirrors XeniaManager.Core.Utilities.HttpClientService but lives in Database to avoid Core dependency.
/// </summary>
public sealed class DatabaseHttpClient : IDisposable
{
    private readonly HttpClient _client;
    private bool _disposed;

    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromDays(1);

    public DatabaseHttpClient(TimeSpan? timeout = null)
    {
        SocketsHttpHandler handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 10
        };
        _client = new HttpClient(handler);
        _client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
        _client.Timeout = timeout ?? TimeSpan.FromSeconds(15);
    }

    public async Task<string> GetAsync(string url, CancellationToken cancellationToken = default, string? cacheKey = null, TimeSpan? cacheDuration = null,
        string? cacheDirectory = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (cacheKey != null)
        {
            TimeSpan duration = cacheDuration ?? DefaultCacheDuration;
            string directory = cacheDirectory ?? Path.Combine(AppContext.BaseDirectory, "Cache", "Database");
            string cacheFile = Path.Combine(directory, $"{cacheKey}.json");

            if (TryReadCache(cacheFile, duration, out string? cachedContent))
            {
                Logger.Info<DatabaseHttpClient>($"Cache hit for {cacheKey}: {cacheFile}");
                return cachedContent!;
            }

            Logger.Info<DatabaseHttpClient>($"Cache miss for {cacheKey}, fetching fresh");
            string freshData = await GetAsyncInternal(url, cancellationToken);
            SaveCache(cacheFile, freshData, directory);
            return freshData;
        }

        return await GetAsyncInternal(url, cancellationToken);
    }

    private async Task<string> GetAsyncInternal(string url, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await _client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            string body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return body;
        }
        catch (HttpRequestException ex)
        {
            Logger.Error<DatabaseHttpClient>($"Error connecting to {url}: {ex.Message}");
            throw;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            Logger.Error<DatabaseHttpClient>("Request timed out");
            Logger.LogExceptionDetails<DatabaseHttpClient>(ex);
            throw new TimeoutException($"Request to '{url}' timed out.", ex);
        }
        catch (TaskCanceledException)
        {
            Logger.Debug<DatabaseHttpClient>($"Request to {url} cancelled by caller");
            throw;
        }
    }

    private bool TryReadCache(string cacheFile, TimeSpan cacheDuration, out string? content)
    {
        content = null;
        if (!File.Exists(cacheFile))
        {
            return false;
        }

        try
        {
            FileInfo fi = new FileInfo(cacheFile);
            TimeSpan age = DateTime.UtcNow - fi.LastWriteTimeUtc;
            if (age > cacheDuration)
            {
                return false;
            }

            content = File.ReadAllText(cacheFile);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warning<DatabaseHttpClient>($"Failed to read cache {cacheFile}: {ex.Message}");
            return false;
        }
    }

    private void SaveCache(string cacheFile, string content, string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(cacheFile, content);
        }
        catch (Exception ex)
        {
            Logger.Warning<DatabaseHttpClient>($"Failed to save cache {cacheFile}: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _client.Dispose();
        _disposed = true;
    }
}