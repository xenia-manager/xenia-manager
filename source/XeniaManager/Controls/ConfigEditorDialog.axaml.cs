using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAvalonia.Core;
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
    /// Shows a TaskDialog to allow the user to edit configuration file settings.
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

        TaskDialog taskDialog = new TaskDialog
        {
            Title = LocalizationHelper.GetText("ConfigEditorDialog.ContentDialog.Title"),
            Content = dialog,
            ShowProgressBar = false,
            XamlRoot = App.MainWindow
        };

        // Add Save and Cancel buttons
        TaskDialogButton saveButton = new TaskDialogButton
        {
            Text = LocalizationHelper.GetText("ConfigEditorDialog.ContentDialog.SaveButton.Text"),
            DialogResult = "SaveConfig"
        };

        TaskDialogButton cancelButton = new TaskDialogButton
        {
            Text = LocalizationHelper.GetText("ConfigEditorDialog.ContentDialog.CancelButton.Text"),
            DialogResult = TaskDialogStandardResult.Cancel
        };

        taskDialog.Buttons.Add(saveButton);
        taskDialog.Buttons.Add(cancelButton);

        bool result = false;

        // Use the closing event to handle saving with deferral
        taskDialog.Closing += async (s, e) =>
        {
            // Only use deferral if the Save button was clicked
            if (ReferenceEquals(e.Result, "SaveConfig"))
            {
                // Cancel the default close behavior
                e.Cancel = true;

                // Get a deferral to keep the dialog open during saving
                Deferral? deferral = e.GetDeferral();

                try
                {
                    // Save the configuration
                    result = await viewModel.SaveAsync();
                }
                catch (Exception ex)
                {
                    Logger.Error<ConfigEditorDialog>("Save operation failed");
                    Logger.LogExceptionDetails<ConfigEditorDialog>(ex);
                    await viewModel.MessageBoxService.ShowErrorAsync(LocalizationHelper.GetText("ConfigEditorDialog.Save.Failed.Title"),
                        ex.Message);
                    result = false;
                }
                finally
                {
                    // Complete the deferral to allow the dialog to close
                    deferral.Complete();
                    if (result)
                    {
                        // Cancel the default close behavior
                        e.Cancel = false;
                        taskDialog.Hide(TaskDialogStandardResult.OK);
                    }
                }
            }
        };

        // Handle Cancel button click
        cancelButton.Click += (s, e) =>
        {
            taskDialog.Hide(TaskDialogStandardResult.Cancel);
        };

        try
        {
            await taskDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Logger.Error<ConfigEditorDialog>("Error showing config editor dialog");
            Logger.LogExceptionDetails<ConfigEditorDialog>(ex);
        }

        return result;
    }
}