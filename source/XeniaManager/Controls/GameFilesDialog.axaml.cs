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
    /// Shows the game files dialog for the given game path.
    /// </summary>
    /// <param name="gamePath">The resolved game file or directory to browse.</param>
    /// <param name="gameTitle">The game title shown in the dialog.</param>
    public static async Task ShowAsync(string gamePath, string gameTitle)
    {
        GameFilesDialog dialogContent = new GameFilesDialog(gamePath, gameTitle);
        FATaskDialog? taskDialog = null;
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

            taskDialog = new FATaskDialog
            {
                Title = !string.IsNullOrEmpty(gameTitle)
                    ? gameTitle
                    : LocalizationHelper.GetText("GameFilesDialog.ContentDialog.Title"),
                Content = dialogContent,
                XamlRoot = App.MainWindow
            };

            // Widen beyond the default 648px TaskDialog max so the tree + details fit
            taskDialog.Resources.Add("TaskDialogMaxWidth", 1000.0);

            FATaskDialogButton closeButton = new FATaskDialogButton
            {
                Text = LocalizationHelper.GetText("GameFilesDialog.CloseButton"),
                DialogResult = "Close"
            };
            taskDialog.Buttons.Add(closeButton);

            await taskDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Logger.Error<GameFilesDialog>("Error showing game files dialog");
            Logger.LogExceptionDetails<GameFilesDialog>(ex);
        }
        finally
        {
            dialogContent._viewModel?.Dispose();
            dialogContent.DataContext = null;
            if (taskDialog != null)
            {
                taskDialog.Content = null;
                taskDialog.Buttons.Clear();
            }
        }
    }
}