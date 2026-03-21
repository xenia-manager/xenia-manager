using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Models.Items;
using XeniaManager.Core.Utilities;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that displays installed content for a game.
/// Allows users to view saved games, marketplace content, installers, and achievements.
/// </summary>
public partial class ContentViewerDialog : UserControl
{
    /// <summary>
    /// The ViewModel containing the dialog's data and logic.
    /// </summary>
    private readonly ContentViewerDialogViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContentViewerDialog"/> class.
    /// </summary>
    public ContentViewerDialog()
    {
        InitializeComponent();
        _viewModel = new ContentViewerDialogViewModel();
        DataContext = _viewModel;
    }

    /// <summary>
    /// Shows the installed content dialog.
    /// </summary>
    /// <param name="accountContents">List of all account contents (including GameContent).</param>
    /// <param name="game">Game whose content we're showing</param>
    public static async void Show(List<AccountContent> accountContents, Game game)
    {
        ContentViewerDialog dialog = new ContentViewerDialog();

        // Initialize the ViewModel with the account contents
        dialog._viewModel.Initialize(accountContents, game);

        ContentDialog contentDialog = new ContentDialog
        {
            Title = LocalizationHelper.GetText("ContentViewerDialog.ContentDialog.Title"),
            Content = dialog,
            CloseButtonText = LocalizationHelper.GetText("ContentViewerDialog.ContentDialog.CloseButton.Text"),
            FullSizeDesired = true,
            DefaultButton = ContentDialogButton.Close
        };

        // Controlling ContentDialog
        contentDialog.Resources.Add("ContentDialogMinWidth", 600.0);
        contentDialog.Resources.Add("ContentDialogMaxWidth", 1000.0);
        contentDialog.Resources.Add("ContentDialogMinHeight", 700.0);
        contentDialog.Resources.Add("ContentDialogMaxHeight", 900.0);

        try
        {
            await contentDialog.ShowAsync();

            // Clean up secret code listener when dialog closes
            dialog._viewModel.DisposeSecretCodeListener();
        }
        catch (Exception ex)
        {
            Logger.Error<ContentViewerDialog>("Error showing installed content dialog");
            Logger.LogExceptionDetails<ContentViewerDialog>(ex);

            // Clean up secret code listener on error
            dialog._viewModel.DisposeSecretCodeListener();
        }
    }
}