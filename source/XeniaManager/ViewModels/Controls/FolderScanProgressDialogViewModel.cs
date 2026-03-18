using System;
using System.Collections.ObjectModel;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XeniaManager.Core.Logging;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for the folder scan progress dialog.
/// Tracks the progress of scanning directories for game files.
/// </summary>
public partial class FolderScanProgressDialogViewModel : ViewModelBase
{
    /// <summary>
    /// The current status message displayed in the dialog.
    /// </summary>
    [ObservableProperty] private string _statusMessage = "Initializing...";

    /// <summary>
    /// The current directory being scanned.
    /// </summary>
    [ObservableProperty] private string _currentDirectory = string.Empty;

    /// <summary>
    /// The number of directories scanned so far.
    /// </summary>
    [ObservableProperty] private int _directoriesScanned;

    /// <summary>
    /// The total number of game files found.
    /// </summary>
    [ObservableProperty] private int _gameFilesFound;

    /// <summary>
    /// The current progress value (0-100).
    /// </summary>
    [ObservableProperty] private int _progressValue;

    /// <summary>
    /// Whether the scan is currently in progress.
    /// </summary>
    [ObservableProperty] private bool _isScanning;

    /// <summary>
    /// Whether the scan has been cancelled by the user.
    /// </summary>
    [ObservableProperty] private bool _isCancelled;

    /// <summary>
    /// Whether the cancel button should be enabled.
    /// </summary>
    [ObservableProperty] private bool _canCancel = true;

    /// <summary>
    /// Collection of status messages for detailed logging.
    /// </summary>
    [ObservableProperty] private ObservableCollection<string> _logMessages = [];

    /// <summary>
    /// The cancellation token source for cancelling the scan operation.
    /// </summary>
    public CancellationTokenSource CancellationTokenSource { get; } = new();

    /// <summary>
    /// Gets whether the scan can be cancelled.
    /// </summary>
    public bool CanCancelScan => CanCancel && IsScanning;

    /// <summary>
    /// Updates the progress information.
    /// </summary>
    /// <param name="statusMessage">The current status message.</param>
    /// <param name="currentDirectory">The current directory being scanned.</param>
    /// <param name="directoriesScanned">The number of directories scanned.</param>
    /// <param name="gameFilesFound">The number of game files found.</param>
    /// <param name="progressValue">The progress percentage (0-100).</param>
    public void UpdateProgress(string statusMessage, string currentDirectory,
        int directoriesScanned, int gameFilesFound, int progressValue)
    {
        StatusMessage = statusMessage;
        CurrentDirectory = currentDirectory;
        DirectoriesScanned = directoriesScanned;
        GameFilesFound = gameFilesFound;
        ProgressValue = progressValue;

        // Add to log (keep last 100 messages)
        if (LogMessages.Count >= 100)
        {
            LogMessages.RemoveAt(0);
        }
        LogMessages.Add($"[{DateTime.Now:HH:mm:ss}] {statusMessage}");
    }

    /// <summary>
    /// Updates the scanning state.
    /// </summary>
    /// <param name="isScanning">Whether scanning is in progress.</param>
    public void UpdateScanningState(bool isScanning)
    {
        IsScanning = isScanning;
        OnPropertyChanged(nameof(CanCancelScan));
    }

    /// <summary>
    /// Cancels the current scan operation.
    /// </summary>
    [RelayCommand]
    private void CancelScan()
    {
        if (IsScanning && CanCancel)
        {
            Logger.Info<FolderScanProgressDialogViewModel>("User requested scan cancellation");
            IsCancelled = true;
            CancellationTokenSource.Cancel();
            CanCancel = false;
            OnPropertyChanged(nameof(CanCancelScan));
        }
    }

    /// <summary>
    /// Completes the scan operation.
    /// </summary>
    public void CompleteScan()
    {
        UpdateScanningState(false);
        CanCancel = false;
        StatusMessage = "Scan complete!";
        OnPropertyChanged(nameof(CanCancelScan));
    }

    /// <summary>
    /// Resets the progress to the initial state.
    /// </summary>
    public void Reset()
    {
        StatusMessage = "Initializing...";
        CurrentDirectory = string.Empty;
        DirectoriesScanned = 0;
        GameFilesFound = 0;
        ProgressValue = 0;
        IsScanning = false;
        IsCancelled = false;
        CanCancel = true;
        LogMessages.Clear();
        CancellationTokenSource.Dispose();
        OnPropertyChanged(nameof(CanCancelScan));
    }

    /// <summary>
    /// Disposes of resources used by the ViewModel.
    /// </summary>
    public void Dispose()
    {
        CancellationTokenSource?.Dispose();
    }
}