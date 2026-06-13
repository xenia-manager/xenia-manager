using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that allows users to edit Xbox 360 XConfig settings for a Xenia emulator.
/// This control provides combobox-based editing for resolution, language, country, and default profile.
/// </summary>
public partial class EditXConfigDialog : UserControl
{
    /// <summary>
    /// The ViewModel containing the dialog's data and logic.
    /// </summary>
    private readonly EditXConfigDialogViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditXConfigDialog"/> class.
    /// This constructor is required for the XAML loader.
    /// </summary>
    public EditXConfigDialog()
    {
        Logger.Trace<EditXConfigDialog>("Creating EditXConfigDialog");
        InitializeComponent();
        _viewModel = new EditXConfigDialogViewModel();
        DataContext = _viewModel;
    }

    /// <summary>
    /// Shows a ContentDialog to allow the user to edit XConfig settings.
    /// </summary>
    /// <param name="xconfig">The XConfigFile instance to edit.</param>
    /// <param name="profiles">The list of available profiles for the default profile selection.</param>
    /// <param name="xeniaVersion">The Xenia version to use for saving the XConfig.</param>
    /// <returns>True if the user saved changes, false if the user canceled the dialog.</returns>
    public static async Task<bool> ShowAsync(XConfigFile xconfig, List<AccountInfo> profiles, XeniaVersion xeniaVersion)
    {
        Logger.Info<EditXConfigDialog>($"Showing Edit XConfig dialog for {xeniaVersion}");
        Logger.Debug<EditXConfigDialog>($"Loaded XConfig file, {profiles.Count} profiles available");

        EditXConfigDialog dialog = new EditXConfigDialog
        {
            _viewModel =
            {
                Xconfig = xconfig,
                XeniaVersion = xeniaVersion
            }
        };

        // Load XConfig settings and profiles into the dialog
        dialog._viewModel.LoadXConfig(profiles);

        ContentDialog contentDialog = new ContentDialog
        {
            Title = LocalizationHelper.GetText("EditXConfigDialog.ContentDialog.Title"),
            Content = dialog,
            PrimaryButtonText = LocalizationHelper.GetText("EditXConfigDialog.ContentDialog.SaveButton.Text"),
            CloseButtonText = LocalizationHelper.GetText("EditXConfigDialog.ContentDialog.CancelButton.Text"),
            FullSizeDesired = true,
            DefaultButton = ContentDialogButton.Primary
        };

        // Set dialog size constraints
        contentDialog.Resources.Add("ContentDialogMinWidth", 200.0);
        contentDialog.Resources.Add("ContentDialogMaxWidth", 800.0);
        contentDialog.Resources.Add("ContentDialogMinHeight", 400.0);
        contentDialog.Resources.Add("ContentDialogMaxHeight", 670.0);

        bool result = false;

        // Handle primary button (Save) using deferral to properly handle async operation
        contentDialog.PrimaryButtonClick += async (_, e) =>
        {
            Deferral? deferral = e.GetDeferral();
            try
            {
                Logger.Info<EditXConfigDialog>("Save button clicked, saving XConfig settings");
                dialog._viewModel.SaveCommand.Execute(null);
                result = true;
                Logger.Info<EditXConfigDialog>("XConfig settings saved successfully");
            }
            catch (Exception ex)
            {
                Logger.Error<EditXConfigDialog>("Save operation failed");
                Logger.LogExceptionDetails<EditXConfigDialog>(ex);
                result = false;
                e.Cancel = true;

                IMessageBoxService messageBox = App.Services.GetRequiredService<IMessageBoxService>();
                await messageBox.ShowErrorAsync(
                    LocalizationHelper.GetText("EditXConfigDialog.Save.Failed.Title"),
                    ex.Message);
            }
            finally
            {
                deferral.Complete();
            }
        };

        try
        {
            Logger.Trace<EditXConfigDialog>("Showing Edit XConfig content dialog");
            await contentDialog.ShowAsync();
        }
        catch (Exception ex)
        {
            Logger.Error<EditXConfigDialog>("Error showing edit XConfig dialog");
            Logger.LogExceptionDetails<EditXConfigDialog>(ex);
        }

        return result;
    }
}