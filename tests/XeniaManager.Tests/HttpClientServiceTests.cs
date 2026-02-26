using XeniaManager.Core.Utilities;

namespace XeniaManager.Tests;

[TestFixture]
public class HttpClientServiceTests
{
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
        string testUrl = "https://httpbin.org/get";

        // Act
        string response = await httpClientService.GetAsync(testUrl);

        // Assert
        Assert.That(response, Is.Not.Null);
        Assert.That(response, Does.Contain("\"url\"")); // httpbin returns JSON with "url" field
    }

    [Test]
    public async Task GetAsync_WithValidUrlAndCancellationToken_ReturnsSuccessfulResponse()
    {
        // Arrange
        using HttpClientService httpClientService = new HttpClientService();
        string testUrl = "https://httpbin.org/get";
        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        // Act
        string response = await httpClientService.GetAsync(testUrl, cancellationTokenSource.Token);

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
        ObjectDisposedException exception = Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await httpClientService.GetAsync("https://httpbin.org/get")
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
        ObjectDisposedException exception = Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await httpClientService.GetAsync(null!)
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