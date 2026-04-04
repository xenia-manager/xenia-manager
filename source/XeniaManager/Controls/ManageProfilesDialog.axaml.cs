using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;
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
    /// Shows a ContentDialog to allow the user to manage and edit profiles.
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

        ContentDialog contentDialog = new ContentDialog
        {
            Title = LocalizationHelper.GetText("ManageProfilesDialog.ContentDialog.Title"),
            Content = dialog,
            PrimaryButtonText = LocalizationHelper.GetText("ManageProfilesDialog.ContentDialog.SaveButton.Text"),
            CloseButtonText = LocalizationHelper.GetText("ManageProfilesDialog.ContentDialog.CancelButton.Text"),
            FullSizeDesired = true,
            DefaultButton = ContentDialogButton.Primary
        };

        // Controlling ContentDialog
        contentDialog.Resources.Add("ContentDialogMinWidth", 200.0);
        contentDialog.Resources.Add("ContentDialogMaxWidth", 800.0);
        contentDialog.Resources.Add("ContentDialogMinHeight", 400.0);
        contentDialog.Resources.Add("ContentDialogMaxHeight", 670.0);

        // Set the initial button state
        contentDialog.IsPrimaryButtonEnabled = dialog._viewModel.CanSave;

        // Bind button states to ViewModel
        dialog._viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ManageProfilesDialogViewModel.CanSave))
            {
                contentDialog.IsPrimaryButtonEnabled = dialog._viewModel.CanSave;
            }
        };

        bool result = false;

        // Handle primary button (Save) using deferral to properly handle async operation
        contentDialog.PrimaryButtonClick += async (_, e) =>
        {
            Deferral? deferral = e.GetDeferral();
            try
            {
                // First, apply any pending edits to the selected profile
                dialog._viewModel.SaveCommand.Execute(null);

                // Save all profiles using ProfileManager
                int savedCount = await Task.Run(() => ProfileManager.SaveProfiles(dialog._viewModel.Profiles, dialog._viewModel.XeniaVersion));
                int failedCount = dialog._viewModel.Profiles.Count - savedCount;

                result = failedCount == 0;

                // Show a success message
                string successMessage = string.Format(
                    LocalizationHelper.GetText("ManageProfilesDialog.Save.Success.Message"),
                    savedCount,
                    dialog._viewModel.Profiles.Count);

                IMessageBoxService messageBox = App.Services.GetRequiredService<IMessageBoxService>();
                await messageBox.ShowInfoAsync(
                    LocalizationHelper.GetText("ManageProfilesDialog.Save.Success.Title"),
                    successMessage);
            }
            catch (Exception ex)
            {
                Logger.Error<ManageProfilesDialog>("Save operation failed");
                Logger.LogExceptionDetails<ManageProfilesDialog>(ex);
                result = false;
                e.Cancel = true;

                IMessageBoxService messageBox = App.Services.GetRequiredService<IMessageBoxService>();
                await messageBox.ShowErrorAsync(
                    LocalizationHelper.GetText("ManageProfilesDialog.Save.Failed.Title"),
                    ex.Message);
            }
            finally
            {
                deferral.Complete();
            }
        };

        try
        {
            await contentDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Logger.Error<ManageProfilesDialog>("Error showing manage profiles dialog");
            Logger.LogExceptionDetails<ManageProfilesDialog>(ex);
        }

        return result;
    }
}