using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Utilities;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that displays the progress of adding games to the library.
/// This control provides real-time progress tracking without user interaction.
/// </summary>
public partial class AddGamesProgressDialog : UserControl
{
    /// <summary>
    /// The ViewModel containing the dialog's data and logic.
    /// </summary>
    private readonly AddGamesProgressDialogViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddGamesProgressDialog"/> class.
    /// </summary>
    public AddGamesProgressDialog()
    {
        InitializeComponent();
        _viewModel = new AddGamesProgressDialogViewModel();
        DataContext = _viewModel;
    }

    /// <summary>
    /// Gets the ViewModel for this dialog.
    /// </summary>
    public AddGamesProgressDialogViewModel ViewModel => _viewModel;

    /// <summary>
    /// Shows a dialog to display the progress of adding games to the library.
    /// </summary>
    /// <param name="addGamesAction">
    /// The add games action to execute. This function receives the progress reporter
    /// and returns the results (gamesAdded, gamesSkipped, gamesFailed).
    /// </param>
    /// <returns>
    /// A tuple containing the results: (gamesAdded, gamesSkipped, gamesFailed)
    /// </returns>
    public static async Task<(int GamesAdded, int GamesSkipped, int GamesFailed)> ShowAsync(Func<Action<string, string, int, int, int, int, int, int>, Task<(int, int, int)>> addGamesAction)
    {
        AddGamesProgressDialog dialog = new AddGamesProgressDialog();

        ContentDialog contentDialog = new ContentDialog
        {
            Title = null, // Title is in the UserControl
            Content = dialog,
            PrimaryButtonText = LocalizationHelper.GetText("AddGamesProgressDialog.Button.Close"),
            FullSizeDesired = false,
            DefaultButton = ContentDialogButton.Primary
        };

        // Controlling ContentDialog
        contentDialog.Resources.Add("ContentDialogMinWidth", 500.0);
        contentDialog.Resources.Add("ContentDialogMaxWidth", 600.0);
        contentDialog.Resources.Add("ContentDialogMinHeight", 450.0);
        contentDialog.Resources.Add("ContentDialogMaxHeight", 550.0);

        // Disable the close button while processing
        contentDialog.IsPrimaryButtonEnabled = false;

        // Create a progress reporter that updates the ViewModel
        Action<string, string, int, int, int, int, int, int> progressReporter = (status, file, processed, total, added, skipped, failed, progress) =>
        {
            dialog._viewModel.UpdateProgress(status, file, processed, total, added, skipped, failed, progress);
        };

        // Start the add games action
        Task<(int, int, int)> addTask = addGamesAction(progressReporter);

        try
        {
            // Show the dialog and wait for completion
            Task<ContentDialogResult>? showTask = contentDialog.ShowAsync();

            // Wait for the operation to complete
            (int, int, int) result = await addTask;

            // Mark the operation as complete
            dialog._viewModel.CompleteOperation();
            dialog._viewModel.StatusMessage = string.Format(
                LocalizationHelper.GetText("AddGamesProgressDialog.Status.Complete"),
                result.Item1);

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
            Logger.Error<AddGamesProgressDialog>("Add games operation failed");
            Logger.LogExceptionDetails<AddGamesProgressDialog>(ex);
            dialog._viewModel.CompleteOperation();
            dialog._viewModel.StatusMessage = string.Format(
                LocalizationHelper.GetText("AddGamesProgressDialog.Status.Error"),
                ex.Message);

            // Enable close button
            contentDialog.IsPrimaryButtonEnabled = true;

            // Keep the dialog open to show the error
            await contentDialog.ShowAsync();

            return (0, 0, 0);
        }
    }
}