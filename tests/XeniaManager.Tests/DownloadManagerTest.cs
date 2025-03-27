// Imported
using XeniaManager.Core.Downloader;

namespace XeniaManager.Tests;

[TestFixture]
public class DownloadManagerTest
{
    // URL from the latest release (adjust if needed)
    private readonly string _downloadUrl = "https://github.com/xenia-canary/xenia-canary-releases/releases/download/5f918ef/xenia_canary_windows.zip";
        
    private string _tempDownloadFile;
    private string _tempExtractDir;
    private DownloadManager _manager;

    [SetUp]
    public void Setup()
    {
        _manager = new DownloadManager();

        // Create temporary paths for the downloaded ZIP and extraction directory.
        _tempDownloadFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
        _tempExtractDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempExtractDir);
    }

    [TearDown]
    public void Cleanup()
    {
        // Clean up downloaded file and extracted directory.
        if (File.Exists(_tempDownloadFile))
            File.Delete(_tempDownloadFile);

        if (Directory.Exists(_tempExtractDir))
            Directory.Delete(_tempExtractDir, true);
    }

    [Test, Category("Integration")]
    public async Task TestCheckIfUrlWorksAsync()
    {
        bool works = await _manager.CheckIfUrlWorksAsync(_downloadUrl);
        if (works)
        {
            Assert.Pass("URL Checker passed");
        }
        else
        {
            Assert.Fail("URL Checker failed");
        }
    }
    
    [Test, Category("Integration")]
    public async Task TestDownloadAndExtractAsync()
    {
        int lastProgress = 0;
        _manager.ProgressChanged += progress => lastProgress = progress;

        using var cts = new CancellationTokenSource();

        await _manager.DownloadAndExtractAsync(_downloadUrl, _tempDownloadFile, _tempExtractDir, cts.Token);

        Assert.That(File.Exists(_tempDownloadFile), Is.False, "Downloaded file should be deleted after extraction.");
        string[] extractedFiles = Directory.GetFiles(_tempExtractDir, "*", SearchOption.AllDirectories);
        Assert.That(extractedFiles.Any(), Is.True, "Extraction directory should contain files.");
        Assert.That(lastProgress, Is.GreaterThan(0), "Download progress should have been reported.");
    }
}