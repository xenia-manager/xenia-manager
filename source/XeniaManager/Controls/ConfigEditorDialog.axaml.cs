using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Config;
using XeniaManager.Core.Utilities;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that allows users to view and edit configuration file settings.
/// Supports custom UI definitions to control which options are shown and how they appear.
/// </summary>
public partial class ConfigEditorDialog : UserControl
{
    /// <summary>
    /// The ViewModel containing the dialog's data and logic.
    /// </summary>
    private readonly ConfigEditorViewModel _viewModel;

    /// <summary>
    /// Gets a value indicating whether the dialog was saved.
    /// </summary>
    public bool WasSaved { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigEditorDialog"/> class.
    /// This constructor is required for the XAML designer.
    /// </summary>
    public ConfigEditorDialog()
    {
        InitializeComponent();
        _viewModel = null!;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigEditorDialog"/> class.
    /// </summary>
    /// <param name="configFile">The configuration file to edit.</param>
    /// <param name="configFilePath">The path to the configuration file (optional).</param>
    /// <param name="uiDefinition">Optional UI definition to customize the display.</param>
    public ConfigEditorDialog(ConfigFile configFile, string? configFilePath = null, ConfigUiDefinition? uiDefinition = null)
    {
        InitializeComponent();
        _viewModel = new ConfigEditorViewModel(configFile, configFilePath, uiDefinition);
        DataContext = _viewModel;
        WasSaved = false;
    }

    /// <summary>
    /// Shows a dialog to allow the user to edit configuration file settings.
    /// </summary>
    /// <param name="configFile">The configuration file to edit.</param>
    /// <param name="configFilePath">The path to the configuration file (optional).</param>
    /// <param name="uiDefinition">Optional UI definition to customize the display.</param>
    /// <param name="title">Optional title for the dialog. If not provided, uses the title from uiDefinition or "Config Editor".</param>
    /// <returns>True if the user saved changes, false if canceled.</returns>
    public static async Task<bool> ShowAsync(ConfigFile configFile, string? configFilePath = null, ConfigUiDefinition? uiDefinition = null, string? title = null)
    {
        ConfigEditorDialog dialog = new ConfigEditorDialog(configFile, configFilePath, uiDefinition);
        ConfigEditorViewModel viewModel = dialog._viewModel;

        // Override the title if one was provided
        if (!string.IsNullOrEmpty(title))
        {
            viewModel.Title = title;
        }

        ContentDialog contentDialog = new ContentDialog
        {
            Title = LocalizationHelper.GetText("ConfigEditorDialog.ContentDialog.Title"),
            Content = dialog,
            PrimaryButtonText = LocalizationHelper.GetText("ConfigEditorDialog.ContentDialog.SaveButton.Text"),
            CloseButtonText = LocalizationHelper.GetText("ConfigEditorDialog.ContentDialog.CancelButton.Text"),
            FullSizeDesired = true,
            DefaultButton = ContentDialogButton.Primary
        };

        // Controlling ContentDialog
        contentDialog.Resources.Add("ContentDialogMinWidth", 600.0);
        contentDialog.Resources.Add("ContentDialogMaxWidth", 1000.0);

        // Handle primary button (Save)
        contentDialog.PrimaryButtonClick += async (_, e) =>
        {
            try
            {
                bool success = await viewModel.SaveAsync();

                if (!success)
                {
                    // Cancel the dialog close if save failed
                    e.Cancel = true;
                }
                else
                {
                    dialog.WasSaved = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error<ConfigEditorDialog>("Save operation failed");
                Logger.LogExceptionDetails<ConfigEditorDialog>(ex);
                e.Cancel = true;
                await viewModel.MessageBoxService.ShowErrorAsync(
                    LocalizationHelper.GetText("ConfigEditorDialog.Save.Failed.Title"),
                    ex.Message);
            }
        };

        try
        {
            await contentDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Logger.Error<ConfigEditorDialog>("Error showing config editor dialog");
            Logger.LogExceptionDetails<ConfigEditorDialog>(ex);
        }

        return dialog.WasSaved;
    }
}