using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Utilities;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that displays the progress of scanning folders for game files.
/// This control provides real-time progress tracking with cancellation support.
/// </summary>
public partial class FolderScanProgressDialog : UserControl
{
    /// <summary>
    /// The ViewModel containing the dialog's data and logic.
    /// </summary>
    private readonly FolderScanProgressDialogViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="FolderScanProgressDialog"/> class.
    /// </summary>
    public FolderScanProgressDialog()
    {
        InitializeComponent();
        _viewModel = new FolderScanProgressDialogViewModel();
        DataContext = _viewModel;
    }

    /// <summary>
    /// Gets the ViewModel for this dialog.
    /// </summary>
    public FolderScanProgressDialogViewModel ViewModel => _viewModel;

    /// <summary>
    /// Shows a dialog to display the progress of scanning folders for game files.
    /// </summary>
    /// <param name="scanAction">
    /// The scan action to execute. This function receives the CancellationToken
    /// and an Action for reporting progress.
    /// </param>
    /// <returns>
    /// A tuple containing:
    /// - The result of the scan action
    /// - A boolean indicating whether the scan was cancelled
    /// </returns>
    public static async Task<(TResult Result, bool Cancelled)> ShowAsync<TResult>(Func<CancellationToken, Action<string, string, int, int, int>, Task<TResult>> scanAction)
    {
        FolderScanProgressDialog dialog = new FolderScanProgressDialog();

        ContentDialog contentDialog = new ContentDialog
        {
            Title = null, // Title is in the UserControl
            Content = dialog,
            PrimaryButtonText = LocalizationHelper.GetText("FolderScanProgressDialog.Button.Cancel"),
            SecondaryButtonText = LocalizationHelper.GetText("FolderScanProgressDialog.Button.Close"),
            FullSizeDesired = false,
            DefaultButton = ContentDialogButton.Secondary
        };

        // Controlling ContentDialog
        contentDialog.Resources.Add("ContentDialogMinWidth", 500.0);
        contentDialog.Resources.Add("ContentDialogMaxWidth", 600.0);
        contentDialog.Resources.Add("ContentDialogMinHeight", 400.0);
        contentDialog.Resources.Add("ContentDialogMaxHeight", 600.0);

        // Initially: Cancel enabled, Close (Secondary) disabled while scanning
        contentDialog.IsPrimaryButtonEnabled = true;
        contentDialog.IsSecondaryButtonEnabled = false;

        // Create a progress reporter that updates the ViewModel
        Action<string, string, int, int, int> progressReporter = (status, directory, dirsScanned, filesFound, progress) =>
        {
            dialog._viewModel.UpdateProgress(status, directory, dirsScanned, filesFound, progress);
        };

        // Start the scan action
        Task<TResult> scanTask = scanAction(dialog._viewModel.CancellationTokenSource.Token, progressReporter);

        // Handle Cancel button click - don't close dialog, just cancel scanning
        contentDialog.PrimaryButtonClick += (_, e) =>
        {
            Logger.Info<FolderScanProgressDialog>("User requested scan cancellation");
            e.Cancel = true; // Prevent the dialog from closing
            dialog._viewModel.IsCancelled = true;
            dialog._viewModel.CancellationTokenSource.Cancel();
            dialog._viewModel.CanCancel = false;
            contentDialog.IsPrimaryButtonEnabled = false; // Disable the Cancel button after click
        };

        try
        {
            // Show the dialog and wait for completion
            Task<ContentDialogResult>? showTask = contentDialog.ShowAsync();

            // Wait for the scan to complete
            TResult result = await scanTask;

            // Mark the scan as complete
            dialog._viewModel.CompleteScan();
            dialog._viewModel.StatusMessage = string.Format(LocalizationHelper.GetText("FolderScanProgressDialog.Status.Complete"),
                dialog._viewModel.GameFilesFound);

            // Close the dialog programmatically by hiding it
            if (contentDialog is { IsVisible: true })
            {
                contentDialog.Hide();
            }

            return (result, dialog._viewModel.IsCancelled);
        }
        catch (OperationCanceledException)
        {
            Logger.Info<FolderScanProgressDialog>("Scan operation was cancelled");
            dialog._viewModel.CompleteScan();
            dialog._viewModel.StatusMessage = LocalizationHelper.GetText("FolderScanProgressDialog.Status.Cancelled");

            // Swap buttons: disable Cancel, enable Close
            contentDialog.IsPrimaryButtonEnabled = false;
            contentDialog.IsSecondaryButtonEnabled = true;

            // Dialog is already showing, just return
            return (default!, true);
        }
        catch (Exception ex)
        {
            Logger.Error<FolderScanProgressDialog>("Scan operation failed");
            Logger.LogExceptionDetails<FolderScanProgressDialog>(ex);
            dialog._viewModel.CompleteScan();
            dialog._viewModel.StatusMessage = string.Format(LocalizationHelper.GetText("FolderScanProgressDialog.Status.Error"),
                ex.Message);

            // Swap buttons: disable Cancel, enable Close
            contentDialog.IsPrimaryButtonEnabled = false;
            contentDialog.IsSecondaryButtonEnabled = true;

            // Dialog is already showing with the error message, just return
            return (default!, true);
        }
        finally
        {
            dialog._viewModel.Dispose();
        }
    }
}