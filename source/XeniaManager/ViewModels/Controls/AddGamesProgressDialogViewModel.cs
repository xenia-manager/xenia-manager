using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.Core.Logging;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for the add games progress dialog.
/// Tracks the progress of adding discovered games to the library.
/// </summary>
public partial class AddGamesProgressDialogViewModel : ViewModelBase
{
    /// <summary>
    /// The current status message displayed in the dialog.
    /// </summary>
    [ObservableProperty] private string _statusMessage = "Initializing...";

    /// <summary>
    /// The current game file being processed.
    /// </summary>
    [ObservableProperty] private string _currentGameFile = string.Empty;

    /// <summary>
    /// The number of games processed so far.
    /// </summary>
    [ObservableProperty] private int _gamesProcessed;

    /// <summary>
    /// The total number of games to process.
    /// </summary>
    [ObservableProperty] private int _totalGames;

    /// <summary>
    /// The number of games successfully added.
    /// </summary>
    [ObservableProperty] private int _gamesAdded;

    /// <summary>
    /// The number of games skipped (duplicates).
    /// </summary>
    [ObservableProperty] private int _gamesSkipped;

    /// <summary>
    /// The number of games failed to add.
    /// </summary>
    [ObservableProperty] private int _gamesFailed;

    /// <summary>
    /// The current progress value (0-100).
    /// </summary>
    [ObservableProperty] private int _progressValue;

    /// <summary>
    /// Whether the operation is currently in progress.
    /// </summary>
    [ObservableProperty] private bool _isProcessing;

    /// <summary>
    /// Collection of status messages for detailed logging.
    /// </summary>
    [ObservableProperty] private ObservableCollection<string> _logMessages = [];

    /// <summary>
    /// Updates the progress information.
    /// </summary>
    /// <param name="statusMessage">The current status message.</param>
    /// <param name="currentGameFile">The current game file being processed.</param>
    /// <param name="gamesProcessed">The number of games processed so far.</param>
    /// <param name="totalGames">The total number of games to process.</param>
    /// <param name="gamesAdded">The number of games successfully added.</param>
    /// <param name="gamesSkipped">The number of games skipped.</param>
    /// <param name="gamesFailed">The number of games failed.</param>
    /// <param name="progressValue">The progress percentage (0-100).</param>
    public void UpdateProgress(string statusMessage, string currentGameFile,
        int gamesProcessed, int totalGames, int gamesAdded, int gamesSkipped, int gamesFailed, int progressValue)
    {
        StatusMessage = statusMessage;
        CurrentGameFile = currentGameFile;
        GamesProcessed = gamesProcessed;
        TotalGames = totalGames;
        GamesAdded = gamesAdded;
        GamesSkipped = gamesSkipped;
        GamesFailed = gamesFailed;
        ProgressValue = progressValue;

        // Add to log (keep last 100 messages)
        if (LogMessages.Count >= 100)
        {
            LogMessages.RemoveAt(0);
        }
        LogMessages.Add($"[{DateTime.Now:HH:mm:ss}] {statusMessage}");
    }

    /// <summary>
    /// Updates the processing state.
    /// </summary>
    /// <param name="isProcessing">Whether processing is in progress.</param>
    public void UpdateProcessingState(bool isProcessing)
    {
        IsProcessing = isProcessing;
    }

    /// <summary>
    /// Completes the add games operation.
    /// </summary>
    public void CompleteOperation()
    {
        UpdateProcessingState(false);
        StatusMessage = "Operation complete!";
    }

    /// <summary>
    /// Resets the progress to the initial state.
    /// </summary>
    public void Reset()
    {
        StatusMessage = "Initializing...";
        CurrentGameFile = string.Empty;
        GamesProcessed = 0;
        TotalGames = 0;
        GamesAdded = 0;
        GamesSkipped = 0;
        GamesFailed = 0;
        ProgressValue = 0;
        IsProcessing = false;
        LogMessages.Clear();
    }
}