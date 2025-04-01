namespace XeniaManager.Core;

/// <summary>
/// Customized HttpClient class 
/// </summary>
public class HttpClientService
{
    // Variables
    private HttpClient _client { get; set; }
    private HttpResponseMessage _responseMessage { get; set; }

    // Constructor
    public HttpClientService(TimeSpan? timeout = null)
    {
        _client = new HttpClient();
        _client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
        _client.Timeout = timeout ?? TimeSpan.FromSeconds(30);
    }
    
    // Functions
    /// <summary>
    /// Sends a GET request to the specified URL and returns the response body as a string.
    /// Throws an exception if a connection issue occurs or if the response indicates an error.
    /// </summary>
    public async Task<string> GetAsync(string url)
    {
        try
        {
            _responseMessage = await _client.GetAsync(url);
            _responseMessage.EnsureSuccessStatusCode();
            return await _responseMessage.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException httpEx)
        {
            // Connection problems or non-success status codes are wrapped here.
            Logger.Error($"Error connecting to the server. Check your connection or the URL.\n{httpEx}");
            throw new Exception($"Error connecting to the server. Check your connection or the URL.\n{httpEx}");
        }
        catch (TaskCanceledException taskEx)
        {
            // This exception may indicate a timeout.
            Logger.Error($"The request timed out.\n{taskEx}");
            throw new Exception($"The request timed out.\n{taskEx}");
        }
        catch (Exception ex)
        {
            // Handle any other exceptions.
            Logger.Error($"An unexpected error occurred during the GET request.\n{ex}");
            throw new Exception($"An unexpected error occurred during the GET request.\n{ex}");
        }
    }
}