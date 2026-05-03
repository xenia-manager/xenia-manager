using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Utilities;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a welcome dialog shown on the first application startup.
/// This control provides theme selection with a modern Fluent Design UI.
/// </summary>
public partial class WelcomeDialog : UserControl
{
    /// <summary>
    /// The ViewModel containing the dialog's data and logic.
    /// </summary>
    private readonly WelcomeDialogViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="WelcomeDialog"/> class.
    /// This constructor is required for the XAML loader.
    /// </summary>
    public WelcomeDialog()
    {
        InitializeComponent();
        _viewModel = new WelcomeDialogViewModel();
        DataContext = _viewModel;
    }

    /// <summary>
    /// Shows a ContentDialog to allow the user to complete initial application setup.
    /// </summary>
    /// <returns>The selected theme, or null if the user canceled the dialog.</returns>
    public static async Task<Theme?> ShowAsync()
    {
        Logger.Info<WelcomeDialog>("Showing welcome dialog");

        WelcomeDialog dialog = new WelcomeDialog();

        ContentDialog contentDialog = new ContentDialog
        {
            Title = LocalizationHelper.GetText("WelcomeDialog.Title"),
            Content = dialog,
            PrimaryButtonText = LocalizationHelper.GetText("WelcomeDialog.ContinueButton"),
            DefaultButton = ContentDialogButton.Primary
        };

        Theme? selectedTheme = null;

        contentDialog.PrimaryButtonClick += (_, _) =>
        {
            selectedTheme = dialog._viewModel.SelectedTheme;
            Logger.Info<WelcomeDialog>($"User selected theme: {selectedTheme.Value}");
        };

        try
        {
            await contentDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Logger.Error<WelcomeDialog>("Error showing welcome dialog");
            Logger.LogExceptionDetails<WelcomeDialog>(ex);
        }

        return selectedTheme;
    }
}