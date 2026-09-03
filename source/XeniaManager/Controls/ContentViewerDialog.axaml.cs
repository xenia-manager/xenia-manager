using System;
using System.Collections.Generic;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using XeniaManager.Logging;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Models.Items;
using XeniaManager.Core.Utilities;
using XeniaManager.Files.Models.Stfs;
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
    /// <param name="initialType">Content type to display. When null, the content type selector stays visible.</param>
    public static async void Show(List<AccountContent> accountContents, Game game, ContentType? initialType = null)
    {
        ContentViewerDialog dialog = new ContentViewerDialog();

        // Initialize the ViewModel with the account contents
        if (initialType.HasValue)
        {
            dialog._viewModel.Initialize(accountContents, game, initialType.Value);
        }
        else
        {
            dialog._viewModel.Initialize(accountContents, game);
        }

        FAContentDialog contentDialog = new FAContentDialog
        {
            Title = !string.IsNullOrEmpty(game.Title)
                ? game.Title
                : LocalizationHelper.GetText("ContentViewerDialog.ContentDialog.Title"),
            Content = dialog,
            CloseButtonText = LocalizationHelper.GetText("ContentViewerDialog.ContentDialog.CloseButton.Text"),
            FullSizeDesired = true,
            DefaultButton = FAContentDialogButton.Close
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

    /// <summary>
    /// Shows the saved games dialog, locked to the <see cref="ContentType.SavedGame"/> content type.
    /// </summary>
    public static void ShowSavedGames(List<AccountContent> accountContents, Game game) =>
        Show(accountContents, game, ContentType.SavedGame);

    /// <summary>
    /// Shows the achievements dialog, locked to the <see cref="ContentType.Achievements"/> content type.
    /// </summary>
    public static void ShowAchievements(List<AccountContent> accountContents, Game game) =>
        Show(accountContents, game, ContentType.Achievements);

    /// <summary>
    /// Shows the title updates dialog, locked to the <see cref="ContentType.TitleUpdates"/> content type.
    /// </summary>
    public static void ShowTitleUpdates(List<AccountContent> accountContents, Game game) =>
        Show(accountContents, game, ContentType.TitleUpdates);

    /// <summary>
    /// Shows the marketplace content dialog, locked to the <see cref="ContentType.MarketplaceContent"/> content type.
    /// </summary>
    public static void ShowMarketplaceContent(List<AccountContent> accountContents, Game game) =>
        Show(accountContents, game, ContentType.MarketplaceContent);
}