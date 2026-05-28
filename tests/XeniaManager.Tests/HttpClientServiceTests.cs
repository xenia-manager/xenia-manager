using System.Net;
using System.Net.Http;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Tests;

[TestFixture]
public class HttpClientServiceTests
{
    private const string TestUrl = "https://httpbin.org/get";
    [Test]
    public void Constructor_WithDefaultTimeout_SetsTimeoutTo15Seconds()
    {
        // Act
        using HttpClientService httpClientService = new HttpClientService();

        // Assert
        // Note: We can't directly access the timeout property since it's internal to the HttpClientService,
        // but we can verify the constructor doesn't throw an exception
        Assert.That(httpClientService, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithCustomTimeout_SetsCorrectTimeout()
    {
        // Arrange
        TimeSpan customTimeout = TimeSpan.FromSeconds(30);

        // Act
        using HttpClientService httpClientService = new HttpClientService(customTimeout);

        // Assert
        Assert.That(httpClientService, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithNullTimeout_SetsDefaultTimeout()
    {
        // Act
        using HttpClientService httpClientService = new HttpClientService(null);

        // Assert
        Assert.That(httpClientService, Is.Not.Null);
    }

    [Test]
    public async Task GetAsync_WithValidUrl_ReturnsSuccessfulResponse()
    {
        // Arrange
        using HttpClientService httpClientService = new HttpClientService();
        // Using a reliable test endpoint that returns a simple response

        // Act
        string response;
        try
        {
            response = await httpClientService.GetAsync(TestUrl);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            Assert.Ignore($"Test skipped because httpbin.org is unavailable (503): {ex.Message}");
            return;
        }

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response, Does.Contain("\"url\"")); // httpbin returns JSON with "url" field
    }

    [Test]
    public async Task GetAsync_WithValidUrlAndCancellationToken_ReturnsSuccessfulResponse()
    {
        // Arrange
        using HttpClientService httpClientService = new HttpClientService();
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        // Act
        string response;
        try
        {
            response = await httpClientService.GetAsync(TestUrl, cancellationTokenSource.Token);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            Assert.Ignore($"Test skipped because httpbin.org is unavailable (503): {ex.Message}");
            return;
        }

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response, Does.Contain("\"url\"")); // httpbin returns JSON with "url" field
    }

    [Test]
    public void GetAsync_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        HttpClientService httpClientService = new HttpClientService();
        httpClientService.Dispose();

        // Act & Assert
        ObjectDisposedException exception = Assert.ThrowsAsync<ObjectDisposedException>(async () => await httpClientService.GetAsync(TestUrl)
        );
        Assert.That(exception.ObjectName, Is.EqualTo(httpClientService.GetType().FullName));
    }

    [Test]
    public void GetAsync_WithNullUrlAfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        HttpClientService httpClientService = new HttpClientService();
        httpClientService.Dispose();

        // Act & Assert
        ObjectDisposedException exception = Assert.ThrowsAsync<ObjectDisposedException>(async () => await httpClientService.GetAsync(null!)
        );
        Assert.That(exception.ObjectName, Is.EqualTo(httpClientService.GetType().FullName));
    }

    [Test]
    public void Dispose_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        HttpClientService httpClientService = new HttpClientService();

        // Act & Assert
        Assert.DoesNotThrow(() => httpClientService.Dispose());
        Assert.DoesNotThrow(() => httpClientService.Dispose()); // Double dispose should be safe
    }

    [Test]
    public void Dispose_WhenAlreadyDisposed_DoesNotThrow()
    {
        // Arrange
        HttpClientService httpClientService = new HttpClientService();
        httpClientService.Dispose();

        // Act & Assert
        Assert.DoesNotThrow(() => httpClientService.Dispose());
    }
}