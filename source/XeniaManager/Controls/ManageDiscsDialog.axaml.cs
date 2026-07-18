using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that allows users to view, add, rename, and remove the discs
/// associated with a multi-disc game (e.g. Blue Dragon's 3 discs).
/// </summary>
public partial class ManageDiscsDialog : UserControl
{
    private readonly ManageDiscsViewModel? _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManageDiscsDialog"/> class.
    /// This constructor is required for the AXAML loader.
    /// </summary>
    public ManageDiscsDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManageDiscsDialog"/> class.
    /// </summary>
    /// <param name="game">The game whose discs are being managed.</param>
    public ManageDiscsDialog(Game game)
    {
        InitializeComponent();
        _viewModel = new ManageDiscsViewModel(game);
        DataContext = _viewModel;
    }

    /// <summary>
    /// Persists a disc's custom label when the user finishes editing it.
    /// </summary>
    private void OnDiscLabelLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: DiscRowViewModel row } textBox)
        {
            _viewModel?.UpdateDiscLabel(row.DiscNumber, textBox.Text ?? string.Empty);
        }
    }

    /// <summary>
    /// Shows the Manage Discs dialog for the given game.
    /// </summary>
    /// <param name="game">The game whose discs are being managed.</param>
    /// <returns>True when the dialog was closed (changes are applied live and always saved by the caller).</returns>
    public static async Task<bool> ShowAsync(Game game)
    {
        ManageDiscsDialog dialogContent = new ManageDiscsDialog(game);

        string dialogTitle = string.Format(LocalizationHelper.GetText("ManageDiscsDialog.Title"), game.Title);

        FAContentDialog contentDialog = new FAContentDialog
        {
            Title = dialogTitle,
            Content = dialogContent,
            CloseButtonText = LocalizationHelper.GetText("ManageDiscsDialog.CloseButton"),
            DefaultButton = FAContentDialogButton.Close
        };

        await contentDialog.ShowAsync();
        return true;
    }
}
