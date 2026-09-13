using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.Core.Utilities;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// Progress snapshot reported by the extraction task.
/// </summary>
/// <param name="Processed">The number of files processed so far.</param>
/// <param name="Total">The total number of files to extract.</param>
/// <param name="CurrentFile">The full path of the file currently being extracted.</param>
/// <param name="Failed">The number of files that failed so far.</param>
public sealed record ExtractionProgress(int Processed, int Total, string CurrentFile, int Failed);

/// <summary>
/// ViewModel for the extraction progress dialog.
/// Tracks the progress of extracting game files from a container.
/// </summary>
public partial class ExtractProgressDialogViewModel : ViewModelBase
{
    /// <summary>
    /// The current status message displayed in the dialog.
    /// </summary>
    [ObservableProperty] private string _statusMessage = string.Empty;

    /// <summary>
    /// The file currently being extracted.
    /// </summary>
    [ObservableProperty] private string _currentFile = string.Empty;

    /// <summary>
    /// The number of files extracted so far.
    /// </summary>
    [ObservableProperty] private int _filesExtracted;

    /// <summary>
    /// The total number of files to extract.
    /// </summary>
    [ObservableProperty] private int _totalFiles;

    /// <summary>
    /// The number of files that failed to extract.
    /// </summary>
    [ObservableProperty] private int _filesFailed;

    /// <summary>
    /// The current progress value (0-100).
    /// </summary>
    [ObservableProperty] private int _progressValue;

    /// <summary>
    /// Updates the progress information.
    /// </summary>
    /// <param name="progress">The latest extraction progress snapshot.</param>
    public void UpdateProgress(ExtractionProgress progress)
    {
        StatusMessage = string.Format(LocalizationHelper.GetText("ExtractProgressDialog.Status.Extracting"),
            progress.Processed, progress.Total);
        CurrentFile = progress.CurrentFile;
        FilesExtracted = progress.Processed;
        TotalFiles = progress.Total;
        FilesFailed = progress.Failed;
        ProgressValue = progress.Total == 0 ? 0 : progress.Processed * 100 / progress.Total;
    }

    /// <summary>
    /// Marks the extraction as complete.
    /// </summary>
    public void CompleteOperation() => ProgressValue = 100;
}