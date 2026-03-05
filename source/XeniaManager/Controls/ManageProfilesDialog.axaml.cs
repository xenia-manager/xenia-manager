using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Utilities;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that allows users to manage and edit Xenia profiles.
/// This control provides profile selection and editing capabilities with a modern Fluent Design UI.
/// </summary>
public partial class ManageProfilesDialog : UserControl
{
    /// <summary>
    /// The ViewModel containing the dialog's data and logic.
    /// </summary>
    private readonly ManageProfilesDialogViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManageProfilesDialog"/> class.
    /// This constructor is required for the XAML loader.
    /// </summary>
    public ManageProfilesDialog()
    {
        InitializeComponent();
        _viewModel = new ManageProfilesDialogViewModel();
        DataContext = _viewModel;
    }

    /// <summary>
    /// Shows a TaskDialog to allow the user to manage and edit profiles.
    /// </summary>
    /// <param name="profiles">The list of profiles to manage.</param>
    /// <param name="xeniaVersion">The Xenia version to use for profile management.</param>
    /// <returns>True if the user saved changes, false if the user canceled the dialog.</returns>
    public static async Task<bool> ShowAsync(List<AccountInfo> profiles, XeniaVersion xeniaVersion = XeniaVersion.Canary)
    {
        ManageProfilesDialog dialog = new ManageProfilesDialog
        {
            _viewModel =
            {
                XeniaVersion = xeniaVersion
            }
        };

        // Load profiles into the dialog
        dialog._viewModel.LoadProfiles(profiles, xeniaVersion);

        TaskDialog taskDialog = new TaskDialog
        {
            Title = LocalizationHelper.GetText("ManageProfilesDialog.ContentDialog.Title"),
            Content = dialog,
            ShowProgressBar = false,
            XamlRoot = App.MainWindow
        };

        // Add Save and Cancel buttons
        TaskDialogButton saveButton = new TaskDialogButton
        {
            Text = LocalizationHelper.GetText("ManageProfilesDialog.ContentDialog.SaveButton.Text"),
            IsEnabled = dialog._viewModel.CanSave,
            DialogResult = "SaveProfiles"
        };

        TaskDialogButton cancelButton = new TaskDialogButton
        {
            Text = LocalizationHelper.GetText("ManageProfilesDialog.ContentDialog.CancelButton.Text"),
            DialogResult = TaskDialogStandardResult.Cancel
        };

        taskDialog.Buttons.Add(saveButton);
        taskDialog.Buttons.Add(cancelButton);

        // Bind button states to ViewModel
        dialog._viewModel.PropertyChanged += (s, e) =>
        {
            saveButton.IsEnabled = dialog._viewModel.CanSave;
        };

        bool result = false;

        // Use the closing event to handle saving with deferral
        taskDialog.Closing += async (s, e) =>
        {
            // Only use deferral if the Save button was clicked
            if (ReferenceEquals(e.Result, "SaveProfiles"))
            {
                // Cancel the default close behavior
                e.Cancel = true;

                // Get a deferral to keep the dialog open during saving
                Deferral? deferral = e.GetDeferral();

                try
                {
                    // First, apply any pending edits to the selected profile
                    dialog._viewModel.SaveCommand.Execute(null);

                    // Save all profiles using ProfileManager
                    int savedCount = await Task.Run(() => ProfileManager.SaveProfiles(dialog._viewModel.Profiles, dialog._viewModel.XeniaVersion));
                    int failedCount = dialog._viewModel.Profiles.Count - savedCount;

                    result = failedCount == 0;
                }
                catch (Exception ex)
                {
                    Logger.Error<ManageProfilesDialog>("Save operation failed");
                    Logger.LogExceptionDetails<ManageProfilesDialog>(ex);
                    result = false;
                }
                finally
                {
                    // Complete the deferral to allow the dialog to close
                    deferral.Complete();
                    taskDialog.Hide(TaskDialogStandardResult.OK);
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
            Logger.Error<ManageProfilesDialog>("Error showing manage profiles dialog");
            Logger.LogExceptionDetails<ManageProfilesDialog>(ex);
        }

        return result;
    }
}