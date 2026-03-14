using System;
using System.Collections.Generic;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Models.Items;
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

        TaskDialog taskDialog = new TaskDialog
        {
            Title = "Installed Content",
            Content = dialog,
            ShowProgressBar = false,
            XamlRoot = App.MainWindow
        };

        try
        {
            await taskDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Logger.Error<ContentViewerDialog>("Error showing installed content dialog");
            Logger.LogExceptionDetails<ContentViewerDialog>(ex);
        }
    }
}