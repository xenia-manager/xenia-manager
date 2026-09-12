using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Utilities;
using XeniaManager.Files.Browsing;
using XeniaManager.Logging;
using XeniaManager.Services;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that browses a game's container (ISO, ZAR, STFS, SVOD, or loose directory)
/// read-only, with XEX/SPA details and an option to open the game location in the file explorer.
/// </summary>
public partial class GameFilesDialog : UserControl
{
    private readonly GameFilesDialogViewModel? _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="GameFilesDialog"/> class.
    /// This constructor is required for the AXAML loader.
    /// </summary>
    public GameFilesDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GameFilesDialog"/> class.
    /// </summary>
    /// <param name="gamePath">The resolved game file or directory to browse.</param>
    /// <param name="gameTitle">The game title shown in the dialog.</param>
    public GameFilesDialog(string gamePath, string gameTitle)
    {
        InitializeComponent();
        _viewModel = new GameFilesDialogViewModel(gamePath, gameTitle);
        _viewModel.OwnerProvider = () => TopLevel.GetTopLevel(this);
        DataContext = _viewModel;
    }

    /// <summary>
    /// Reveals an activated search result in the tree when its row is double-tapped.
    /// </summary>
    private void OnSearchResultDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border { DataContext: GameFileNode node })
        {
            _viewModel?.RevealSearchResult(node);
        }
    }

    /// <summary>
    /// Shows the game files dialog for the given game path.
    /// </summary>
    /// <param name="gamePath">The resolved game file or directory to browse.</param>
    /// <param name="gameTitle">The game title shown in the dialog.</param>
    public static async Task ShowAsync(string gamePath, string gameTitle)
    {
        GameFilesDialog dialogContent = new GameFilesDialog(gamePath, gameTitle);
        try
        {
            if (dialogContent._viewModel == null || !await dialogContent._viewModel.LoadAsync())
            {
                IMessageBoxService messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
                await messageBoxService.ShowInfoAsync(
                    LocalizationHelper.GetText("GameFilesDialog.LoadError.Title"),
                    LocalizationHelper.GetText("GameFilesDialog.LoadError.Message"));
                return;
            }

            FAContentDialog contentDialog = new FAContentDialog
            {
                Title = !string.IsNullOrEmpty(gameTitle)
                    ? gameTitle
                    : LocalizationHelper.GetText("GameFilesDialog.ContentDialog.Title"),
                Content = dialogContent,
                CloseButtonText = LocalizationHelper.GetText("GameFilesDialog.CloseButton"),
                FullSizeDesired = true,
                DefaultButton = FAContentDialogButton.Close
            };

            // Controlling ContentDialog
            contentDialog.Resources.Add("ContentDialogMinWidth", 600.0);
            contentDialog.Resources.Add("ContentDialogMaxWidth", 1000.0);
            contentDialog.Resources.Add("ContentDialogMinHeight", 700.0);
            contentDialog.Resources.Add("ContentDialogMaxHeight", 900.0);

            await contentDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Logger.Error<GameFilesDialog>("Error showing game files dialog");
            Logger.LogExceptionDetails<GameFilesDialog>(ex);
        }
        finally
        {
            dialogContent._viewModel?.Dispose();
        }
    }
}