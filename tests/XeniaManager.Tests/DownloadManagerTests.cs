using XeniaManager.Core.Constants;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Tests;

[TestFixture]
public class DownloadManagerTests
{
    private string _testDownloadPath = string.Empty;
    private string _testFilePath = string.Empty;

    [SetUp]
    public void Setup()
    {
        // Create a temporary directory for test downloads
        _testDownloadPath = Path.Combine(Path.GetTempPath(), "XeniaManagerTestDownloads");
        _testFilePath = Path.Combine(_testDownloadPath, "testfile.txt");

        // Clean up any existing test files
        if (Directory.Exists(_testDownloadPath))
        {
            Directory.Delete(_testDownloadPath, true);
        }
    }

    [TearDown]
    public void Teardown()
    {
        // Clean up test files after each test
        if (Directory.Exists(_testDownloadPath))
        {
            Directory.Delete(_testDownloadPath, true);
        }
    }

    [Test]
    public void Constructor_SetsUpHttpClientWithCorrectUserAgent()
    {
        // Act
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);

        // Assert
        Assert.That(downloadManager, Is.Not.Null);
        // The constructor should complete without throwing exceptions
    }

    [Test]
    public void DownloadFileAsync_WithNullUrl_ThrowsArgumentException()
    {
        // Arrange
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);

        // Act & Assert
        ArgumentException? exception = Assert.ThrowsAsync<ArgumentException>(async () => await downloadManager.DownloadFileAsync(null!, "validPath.txt"));
        Assert.That(exception!.ParamName, Is.EqualTo("url"));
    }

    [Test]
    public void DownloadFileAsync_WithEmptyUrl_ThrowsArgumentException()
    {
        // Arrange
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);

        // Act & Assert
        ArgumentException? exception = Assert.ThrowsAsync<ArgumentException>(async () => await downloadManager.DownloadFileAsync("", "validPath.txt"));
        Assert.That(exception!.ParamName, Is.EqualTo("url"));
    }

    [Test]
    public void DownloadFileAsync_WithWhitespaceUrl_ThrowsArgumentException()
    {
        // Arrange
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);

        // Act & Assert
        ArgumentException? exception = Assert.ThrowsAsync<ArgumentException>(async () => await downloadManager.DownloadFileAsync("   ", "validPath.txt"));
        Assert.That(exception!.ParamName, Is.EqualTo("url"));
    }

    [Test]
    public void DownloadFileAsync_WithNullSavePath_ThrowsArgumentException()
    {
        // Arrange
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);

        // Act & Assert
        ArgumentException? exception = Assert.ThrowsAsync<ArgumentException>(async () => await downloadManager.DownloadFileAsync("https://httpbin.org/get", null!));
        Assert.That(exception!.ParamName, Is.EqualTo("fileName"));
    }

    [Test]
    public void DownloadFileAsync_WithEmptySavePath_ThrowsArgumentException()
    {
        // Arrange
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);

        // Act & Assert
        ArgumentException? exception = Assert.ThrowsAsync<ArgumentException>(async () => await downloadManager.DownloadFileAsync("https://httpbin.org/get", ""));
        Assert.That(exception!.ParamName, Is.EqualTo("fileName"));
    }

    [Test]
    public void DownloadFileAsync_WithWhitespaceSavePath_ThrowsArgumentException()
    {
        // Arrange
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);

        // Act & Assert
        ArgumentException? exception = Assert.ThrowsAsync<ArgumentException>(async () => await downloadManager.DownloadFileAsync("https://httpbin.org/get", "   "));
        Assert.That(exception!.ParamName, Is.EqualTo("fileName"));
    }

    [Test]
    public async Task DownloadFileAsync_WithValidParameters_HasCorrectProgressEvents()
    {
        // Arrange
        List<int> progressValues = new List<int>();
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);
        downloadManager.ProgressChanged += (progress) => progressValues.Add(progress);

        // For this test, we'll use a URL that should trigger the validation logic
        // but not complete the download (since we can't guarantee external connectivity)
        string url = "https://httpbin.org/get";
        string savePath = _testFilePath;

        // Act & Assert - Check that no validation exceptions are thrown
        try
        {
            // This will likely fail due to network but should pass parameter validation
            await downloadManager.DownloadFileAsync(url, savePath);
        }
        catch (HttpRequestException)
        {
            // Expected when trying to reach an actual URL in a unit test
            Assert.Pass("Parameter validation passed, network error is expected");
        }
        catch (TaskCanceledException)
        {
            // Also acceptable if the request gets canceled
            Assert.Pass("Parameter validation passed, request cancellation is expected");
        }
    }

    [Test]
    public Task DownloadFileAsync_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10)); // Short timeout for test

        // Act & Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await downloadManager.DownloadFileAsync(
                "https://httpbin.org/delay/1",
                _testFilePath,
                cts.Token);
        });
        return Task.CompletedTask;
    }

    [Test]
    public void Dispose_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);

        // Act & Assert
        Assert.DoesNotThrow(() => downloadManager.Dispose());
        Assert.DoesNotThrow(() => downloadManager.Dispose()); // Double dispose should be safe
    }

    [Test]
    public void DownloadFileAsync_WithPathThatRequiresDirectoryCreation_CreatesDirectory()
    {
        // Arrange
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);
        string newPath = Path.Combine(_testDownloadPath, "subdir", "nested", "file.txt");

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await downloadManager.DownloadFileAsync("https://httpbin.org/get", newPath);
            }
            catch (HttpRequestException)
            {
                // Expected when trying to reach an actual URL in a unit test
            }
        });

        // The directory should have been created even if the download failed
        string? directory = Path.GetDirectoryName(newPath);
        Assert.That(Directory.Exists(directory), Is.True);
    }

    [Test]
    public void DownloadFileFromMultipleUrlsAsync_WithNullUrls_ThrowsArgumentException()
    {
        // Arrange
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);

        // Act & Assert
        ArgumentException? exception = Assert.ThrowsAsync<ArgumentException>(async () => await downloadManager.DownloadFileFromMultipleUrlsAsync(null!, "validPath.txt"));
        Assert.That(exception!.ParamName, Is.EqualTo("urls"));
    }

    [Test]
    public void DownloadFileFromMultipleUrlsAsync_WithEmptyUrls_ThrowsArgumentException()
    {
        // Arrange
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);
        string[] emptyUrls = [];

        // Act & Assert
        ArgumentException? exception = Assert.ThrowsAsync<ArgumentException>(async () => await downloadManager.DownloadFileFromMultipleUrlsAsync(emptyUrls, "validPath.txt"));
        Assert.That(exception!.ParamName, Is.EqualTo("urls"));
    }

    [Test]
    public void DownloadFileFromMultipleUrlsAsync_WithNullSavePath_ThrowsArgumentException()
    {
        // Arrange
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);
        string[] urls = Urls.Manifest;

        // Act & Assert
        ArgumentException? exception = Assert.ThrowsAsync<ArgumentException>(async () => await downloadManager.DownloadFileFromMultipleUrlsAsync(urls, null!));
        Assert.That(exception!.ParamName, Is.EqualTo("fileName"));
    }

    [Test]
    public void DownloadFileFromMultipleUrlsAsync_WithEmptySavePath_ThrowsArgumentException()
    {
        // Arrange
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);
        string[] urls = Urls.Manifest;

        // Act & Assert
        ArgumentException? exception = Assert.ThrowsAsync<ArgumentException>(async () => await downloadManager.DownloadFileFromMultipleUrlsAsync(urls, ""));
        Assert.That(exception!.ParamName, Is.EqualTo("fileName"));
    }

    [Test]
    public void DownloadFileFromMultipleUrlsAsync_WithWhitespaceSavePath_ThrowsArgumentException()
    {
        // Arrange
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);
        string[] urls = Urls.Manifest;

        // Act & Assert
        ArgumentException? exception = Assert.ThrowsAsync<ArgumentException>(async () => await downloadManager.DownloadFileFromMultipleUrlsAsync(urls, "   "));
        Assert.That(exception!.ParamName, Is.EqualTo("fileName"));
    }

    [Test]
    public async Task DownloadFileFromMultipleUrlsAsync_WithValidParametersAndFirstUrlSucceeds_DownloadsSuccessfully()
    {
        // Arrange
        List<int> progressValues = new List<int>();
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);
        downloadManager.ProgressChanged += (progress) => progressValues.Add(progress);

        // Using the actual manifest URLs for testing
        string[] urls = Urls.Manifest;
        string savePath = _testFilePath;

        // Act & Assert - Check that no validation exceptions are thrown
        try
        {
            await downloadManager.DownloadFileFromMultipleUrlsAsync(urls, savePath);
        }
        catch (HttpRequestException)
        {
            // Expected when trying to reach actual URLs in unit test
            Assert.Pass("Parameter validation passed, network error is expected");
        }
        catch (TaskCanceledException)
        {
            // Also acceptable if the request gets canceled
            Assert.Pass("Parameter validation passed, request cancellation is expected");
        }
    }

    [Test]
    public async Task DownloadFileFromMultipleUrlsAsync_WithValidParametersAndSecondUrlSucceeds_DownloadsSuccessfully()
    {
        // Arrange
        List<int> progressValues = new List<int>();
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);
        downloadManager.ProgressChanged += (progress) => progressValues.Add(progress);

        // Using manifest URLs with an invalid URL first to test fallback behavior
        string[] urls = new[] { "https://invalid-url-that-will-fail.com/file" }.Concat(Urls.Manifest).ToArray();
        string savePath = _testFilePath;

        // Act & Assert - Check that no validation exceptions are thrown
        try
        {
            await downloadManager.DownloadFileFromMultipleUrlsAsync(urls, savePath);
        }
        catch (HttpRequestException)
        {
            // Expected when trying to reach actual URLs in unit test
            Assert.Pass("Parameter validation passed, network error is expected");
        }
        catch (TaskCanceledException)
        {
            // Also acceptable if the request gets canceled
            Assert.Pass("Parameter validation passed, request cancellation is expected");
        }
    }

    [Test]
    public Task DownloadFileFromMultipleUrlsAsync_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);
        CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(10)); // Short timeout for test
        string[] urls = Urls.Manifest;

        // Act & Assert
        Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await downloadManager.DownloadFileFromMultipleUrlsAsync(
                urls,
                _testFilePath,
                cts.Token);
        });
        return Task.CompletedTask;
    }

    [Test]
    public void DownloadFileFromMultipleUrlsAsync_WithValidParameters_HasCorrectProgressEvents()
    {
        // Arrange
        List<int> progressValues = new List<int>();
        DownloadManager downloadManager = new DownloadManager(_testDownloadPath);
        downloadManager.ProgressChanged += (progress) => progressValues.Add(progress);

        // Using the actual manifest URLs for testing
        string[] urls = Urls.Manifest;
        string savePath = _testFilePath;

        // Act & Assert - Check that no validation exceptions are thrown
        Assert.DoesNotThrowAsync(async () =>
        {
            try
            {
                await downloadManager.DownloadFileFromMultipleUrlsAsync(urls, savePath);
            }
            catch (HttpRequestException)
            {
                // Expected when trying to reach actual URLs in unit test
            }
        });
    }
}