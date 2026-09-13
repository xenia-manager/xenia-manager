using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Utilities;
using XeniaManager.Logging;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that displays the progress of extracting game files from a container.
/// This control provides real-time progress tracking without user interaction.
/// </summary>
public partial class ExtractProgressDialog : UserControl
{
    /// <summary>
    /// The ViewModel containing the dialog's data and logic.
    /// </summary>
    private readonly ExtractProgressDialogViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractProgressDialog"/> class.
    /// </summary>
    public ExtractProgressDialog()
    {
        InitializeComponent();
        _viewModel = new ExtractProgressDialogViewModel();
        DataContext = _viewModel;
    }

    /// <summary>
    /// Gets the ViewModel for this dialog.
    /// </summary>
    public ExtractProgressDialogViewModel ViewModel
    {
        get
        {
            return _viewModel;
        }
    }

    /// <summary>
    /// Shows a dialog to display the progress of extracting game files.
    /// </summary>
    /// <param name="extractAction">
    /// The extract action to execute. This function receives the progress reporter
    /// and returns the results (filesExtracted, firstError).
    /// </param>
    /// <returns>
    /// A tuple containing the results: (filesExtracted, firstError).
    /// </returns>
    public static async Task<(int FilesExtracted, string? FirstError)> ShowAsync(
        Func<Action<ExtractionProgress>, Task<(int, string?)>> extractAction)
    {
        ExtractProgressDialog dialog = new ExtractProgressDialog();

        FAContentDialog contentDialog = new FAContentDialog
        {
            Title = null, // Title is in the UserControl
            Content = dialog,
            PrimaryButtonText = LocalizationHelper.GetText("ExtractProgressDialog.Button.Close"),
            FullSizeDesired = false,
            DefaultButton = FAContentDialogButton.Primary
        };

        // Controlling ContentDialog
        contentDialog.Resources.Add("ContentDialogMinWidth", 500.0);
        contentDialog.Resources.Add("ContentDialogMaxWidth", 600.0);

        // Disable the close button while processing
        contentDialog.IsPrimaryButtonEnabled = false;

        // Create a progress reporter that updates the ViewModel
        Action<ExtractionProgress> progressReporter = progress =>
        {
            dialog._viewModel.UpdateProgress(progress);
        };

        // Start the extract action
        Task<(int, string?)> extractTask = extractAction(progressReporter);

        try
        {
            // Show the dialog and wait for completion
            Task<FAContentDialogResult>? showTask = contentDialog.ShowAsync();

            // Wait for the operation to complete
            (int, string?) result = await extractTask;

            // Mark the operation as complete
            dialog._viewModel.CompleteOperation();

            // Enable the close button and auto-close after a short delay
            contentDialog.IsPrimaryButtonEnabled = true;

            // Close the dialog programmatically
            if (contentDialog is { IsVisible: true })
            {
                contentDialog.Hide();
            }

            return result;
        }
        catch (Exception ex)
        {
            Logger.Error<ExtractProgressDialog>("Extract operation failed");
            Logger.LogExceptionDetails<ExtractProgressDialog>(ex);
            dialog._viewModel.CompleteOperation();
            dialog._viewModel.StatusMessage = string.Format(
                LocalizationHelper.GetText("ExtractProgressDialog.Status.Error"),
                ex.Message);

            // Enable close button
            contentDialog.IsPrimaryButtonEnabled = true;

            // Keep the dialog open to show the error
            await contentDialog.ShowAsync();

            return (0, ex.Message);
        }
    }
}