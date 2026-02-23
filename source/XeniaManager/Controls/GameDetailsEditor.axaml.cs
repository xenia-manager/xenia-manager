using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that allows users to edit game information including artwork, title, compatibility rating, and compatibility page URL.
/// </summary>
public partial class GameDetailsEditor : UserControl
{
    private readonly GameDetailsEditorViewModel? _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameDetailsEditor"/> class.
    /// This constructor is required for the AXAML loader.
    /// </summary>
    public GameDetailsEditor()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameDetailsEditor"/> class.
    /// </summary>
    /// <param name="game">The game to edit.</param>
    public GameDetailsEditor(Game game)
    {
        InitializeComponent();
        IMessageBoxService messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
        _viewModel = new GameDetailsEditorViewModel(game, messageBoxService);
        DataContext = _viewModel;
    }

    /// <summary>
    /// Shows a dialog to allow the user to edit game information.
    /// </summary>
    /// <param name="game">The game to edit.</param>
    /// <returns>True if the user saved changes, false if canceled.</returns>
    public static async Task<bool> ShowAsync(Game game)
    {
        GameDetailsEditor editor = new GameDetailsEditor(game);

        string dialogTitle = string.Format(LocalizationHelper.GetText("GameDetailsEditor.DialogTitle"), game.Title);

        ContentDialog contentDialog = new ContentDialog
        {
            Title = dialogTitle,
            Content = editor,
            PrimaryButtonText = LocalizationHelper.GetText("GameDetailsEditor.SaveButton"),
            CloseButtonText = LocalizationHelper.GetText("GameDetailsEditor.CancelButton"),
            FullSizeDesired = true,
            DefaultButton = ContentDialogButton.Primary
        };

        // Handle primary button (Save)
        contentDialog.PrimaryButtonClick += async (_, e) =>
        {
            bool success = await editor._viewModel?.SaveAsync()!;

            if (!success)
            {
                // Cancel the dialog close if save failed
                e.Cancel = true;
            }
        };

        ContentDialogResult result = await contentDialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}