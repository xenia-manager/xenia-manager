using XeniaManager.Core.Constants;
using XeniaManager.Logging;

namespace XeniaManager.Core.Services;

/// <summary>
/// Monitors the Games directory for new files using a FileSystemWatcher and
/// fires an event after a debounce period when new game files are detected.
/// </summary>
public class GameDirectoryWatcherService : IDisposable
{
    // File system watcher instance
    private FileSystemWatcher? _watcher;

    // Debounce mechanism to avoid firing multiple events during bulk copies
    private CancellationTokenSource? _debounceCts;

    // Thread safety for watcher lifecycle
    private readonly Lock _lock = new Lock();

    /// <summary>
    /// Fires when new game files are detected after the debounce period.
    /// </summary>
    public event EventHandler? NewGameFilesDetected;

    /// <summary>
    /// Gets whether the watcher is currently running.
    /// </summary>
    public bool IsRunning => _watcher != null;

    /// <summary>
    /// Starts monitoring the Games directory for new files.
    /// Creates the directory if it doesn't exist.
    /// </summary>
    public void Start()
    {
        lock (_lock)
        {
            if (_watcher != null)
            {
                Logger.Trace<GameDirectoryWatcherService>("Watcher is already running, skipping duplicate start");
                return;
            }

            // Ensure the Games directory exists
            if (!Directory.Exists(AppPaths.GamesDirectory))
            {
                Logger.Debug<GameDirectoryWatcherService>($"Games directory does not exist, creating: {AppPaths.GamesDirectory}");
                Directory.CreateDirectory(AppPaths.GamesDirectory);
            }

            Logger.Debug<GameDirectoryWatcherService>($"Initializing FileSystemWatcher for: {AppPaths.GamesDirectory}");

            _watcher = new FileSystemWatcher(AppPaths.GamesDirectory)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
                Filter = "*.*"
            };

            _watcher.Created += OnFileSystemEvent;
            _watcher.Changed += OnFileSystemEvent;
            // Renamed events are not meaningful for new game detection
            _watcher.EnableRaisingEvents = true;

            Logger.Info<GameDirectoryWatcherService>($"Started watching directory: {AppPaths.GamesDirectory}");
        }
    }

    /// <summary>
    /// Stops monitoring the Games directory and cleans up resources.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            // Cancel any pending debounce
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = null;

            if (_watcher != null)
            {
                Logger.Debug<GameDirectoryWatcherService>("Disabling FileSystemWatcher");

                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnFileSystemEvent;
                _watcher.Changed -= OnFileSystemEvent;
                _watcher.Dispose();
                _watcher = null;

                Logger.Info<GameDirectoryWatcherService>("Stopped watching directory");
            }
            else
            {
                Logger.Trace<GameDirectoryWatcherService>("Watcher is not running, nothing to stop");
            }
        }
    }

    /// <summary>
    /// Handles file system events by filtering directories and unsupported extensions,
    /// then resetting the debounce timer.
    /// </summary>
    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        // Skip events that are for directories (folder creation, metadata updates)
        try
        {
            FileAttributes attr = File.GetAttributes(e.FullPath);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                Logger.Trace<GameDirectoryWatcherService>($"Ignoring directory event: {e.ChangeType} - {e.Name}");
                return;
            }
        }
        catch
        {
            return;
        }

        // Skip files with extensions that are not supported game file types
        string ext = Path.GetExtension(e.Name ?? "").ToLowerInvariant();
        if (!IsSupportedExtension(ext))
        {
            Logger.Trace<GameDirectoryWatcherService>($"Ignoring file change for non-game file: {e.Name} (extension: {ext})");
            return;
        }

        Logger.Trace<GameDirectoryWatcherService>($"File system event detected: {e.ChangeType} - {e.Name}");

        // Reset the debounce timer on each event
        lock (_lock)
        {
            _debounceCts?.Cancel();
            _debounceCts?.Dispose();
            _debounceCts = new CancellationTokenSource();
        }

        // Wait 3 seconds of inactivity before firing the event
        // This handles bulk copies and ongoing file writes
        CancellationToken token = _debounceCts.Token;
        Task.Delay(3000, token).ContinueWith(_ =>
        {
            Logger.Debug<GameDirectoryWatcherService>("Debounce period elapsed, firing NewGameFilesDetected event");
            NewGameFilesDetected?.Invoke(this, EventArgs.Empty);
        }, token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
    }

    /// <summary>
    /// Determines whether a file extension is a supported game file type.
    /// </summary>
    private static bool IsSupportedExtension(string ext) => ext switch
    {
        ".iso" or ".xiso" or ".zar" or ".xex" or "" => true,
        _ => false
    };

    /// <summary>
    /// Disposes the watcher by stopping all monitoring.
    /// </summary>
    public void Dispose()
    {
        Logger.Trace<GameDirectoryWatcherService>("Disposing GameDirectoryWatcherService");
        Stop();
    }
}