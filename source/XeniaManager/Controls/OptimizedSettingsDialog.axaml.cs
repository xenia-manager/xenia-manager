using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Utilities;
using XeniaManager.ViewModels.Controls;
using XeniaManager.ViewModels.Items;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that allows users to review and apply optimized settings.
/// Shows settings that will be applied and allows removal of individual settings.
/// </summary>
public partial class OptimizedSettingsDialog : UserControl
{
    /// <summary>
    /// The ViewModel containing the dialog's data and logic.
    /// </summary>
    private readonly OptimizedSettingsDialogViewModel _viewModel;

    /// <summary>
    /// Gets a value indicating whether the user confirmed applying settings.
    /// </summary>
    public bool WasConfirmed { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OptimizedSettingsDialog"/> class.
    /// This constructor is required for the XAML designer.
    /// </summary>
    public OptimizedSettingsDialog()
    {
        InitializeComponent();
        _viewModel = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="OptimizedSettingsDialog"/> class.
    /// </summary>
    /// <param name="currentConfigFile">The current game configuration file.</param>
    /// <param name="optimizedConfigFile">The optimized configuration file to apply.</param>
    /// <param name="gameName">The name of the game.</param>
    public OptimizedSettingsDialog(ConfigFile currentConfigFile, ConfigFile optimizedConfigFile, string gameName)
    {
        InitializeComponent();
        _viewModel = new OptimizedSettingsDialogViewModel(currentConfigFile, optimizedConfigFile, gameName);
        DataContext = _viewModel;
        WasConfirmed = false;
    }

    /// <summary>
    /// Shows a dialog to allow the user to review and apply optimized settings.
    /// </summary>
    /// <param name="currentConfigFile">The current game configuration file.</param>
    /// <param name="optimizedConfigFile">The optimized configuration file to apply.</param>
    /// <param name="gameName">The name of the game.</param>
    /// <returns>True if the user confirmed applying settings, false if canceled.</returns>
    public static async Task<bool> ShowAsync(ConfigFile currentConfigFile, ConfigFile optimizedConfigFile, string gameName)
    {
        OptimizedSettingsDialog dialog = new OptimizedSettingsDialog(currentConfigFile, optimizedConfigFile, gameName);
        OptimizedSettingsDialogViewModel viewModel = dialog._viewModel;

        ContentDialog contentDialog = new ContentDialog
        {
            Title = LocalizationHelper.GetText("OptimizedSettingsDialog.ContentDialog.Title"),
            Content = dialog,
            PrimaryButtonText = LocalizationHelper.GetText("OptimizedSettingsDialog.ContentDialog.ApplyButton.Text"),
            CloseButtonText = LocalizationHelper.GetText("OptimizedSettingsDialog.ContentDialog.CancelButton.Text"),
            FullSizeDesired = true,
            DefaultButton = ContentDialogButton.Primary
        };

        // Controlling ContentDialog
        contentDialog.Resources.Add("ContentDialogMinWidth", 400.0);
        contentDialog.Resources.Add("ContentDialogMaxWidth", 700.0);

        // Handle primary button (Apply)
        contentDialog.PrimaryButtonClick += async (_, e) =>
        {
            try
            {
                bool success = await viewModel.ApplySettingsAsync();

                if (!success)
                {
                    // Cancel the dialog close if apply failed
                    e.Cancel = true;
                }
                else
                {
                    dialog.WasConfirmed = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error<OptimizedSettingsDialog>("Apply operation failed");
                Logger.LogExceptionDetails<OptimizedSettingsDialog>(ex);
                e.Cancel = true;
                await viewModel.MessageBoxService.ShowErrorAsync(LocalizationHelper.GetText("OptimizedSettingsDialog.Apply.Failed.Title"),
                    ex.Message);
            }
        };

        try
        {
            await contentDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Logger.Error<OptimizedSettingsDialog>("Error showing optimized settings dialog");
            Logger.LogExceptionDetails<OptimizedSettingsDialog>(ex);
        }

        return dialog.WasConfirmed;
    }

    /// <summary>
    /// Handles the remove button click event.
    /// </summary>
    private void RemoveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: OptimizedSettingOptionViewModel setting })
        {
            _viewModel.SelectedSetting = setting;
            _viewModel.RemoveSettingCommand.Execute(null);
        }
    }
}