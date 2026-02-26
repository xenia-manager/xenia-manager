using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that allows users to configure patches for a game.
/// Users can enable/disable patches, edit patch metadata, and manage patch commands.
/// </summary>
public partial class PatchConfigurationDialog : UserControl
{
    private readonly PatchConfigurationViewModel _viewModel = null!;
    private readonly IMessageBoxService _messageBoxService = null!;

    /// <summary>
    /// Gets a value indicating whether the dialog was saved.
    /// </summary>
    public bool WasSaved { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PatchConfigurationDialog"/> class.
    /// This constructor is required for disabling warning caused by the builder.
    /// </summary>
    public PatchConfigurationDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PatchConfigurationDialog"/> class.
    /// </summary>
    /// <param name="patchFile">The patch file to configure.</param>
    /// <param name="patchFilePath">The path to the patch file.</param>
    public PatchConfigurationDialog(PatchFile patchFile, string patchFilePath)
    {
        InitializeComponent();
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
        _viewModel = new PatchConfigurationViewModel(patchFile, patchFilePath, _messageBoxService);
        DataContext = _viewModel;
        WasSaved = false;
    }

    /// <summary>
    /// Handles the click event of the Delete Patch button.
    /// </summary>
    private async void DeletePatchButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is not Button { DataContext: PatchEntryViewModel patch })
            {
                return;
            }

            await _viewModel.RemovePatchAsync(patch);
        }
        catch (Exception ex)
        {
            Logger.Error<PatchConfigurationDialog>("There was an error while deleting a patch");
            Logger.LogExceptionDetails<PatchConfigurationDialog>(ex);
        }
    }

    /// <summary>
    /// Handles the click event of the Delete Command button.
    /// </summary>
    private void DeleteCommandButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: PatchCommandViewModel command } button)
        {
            // Find the parent PatchEntryViewModel
            if (button.FindAncestorOfType<Expander>()?.DataContext is PatchEntryViewModel patchEntry)
            {
                patchEntry.RemoveCommand(command);
            }
        }
    }

    /// <summary>
    /// Shows a dialog to allow the user to configure patches for a game.
    /// </summary>
    /// <param name="gameTitle">The title of the game.</param>
    /// <param name="patchFile">The patch file to configure.</param>
    /// <param name="patchFilePath">The path to the patch file.</param>
    /// <returns>True if the user saved changes, false if canceled.</returns>
    public static async Task<bool> ShowAsync(string gameTitle, PatchFile patchFile, string patchFilePath)
    {
        PatchConfigurationDialog dialog = new PatchConfigurationDialog(patchFile, patchFilePath);

        string dialogTitle = string.Format(LocalizationHelper.GetText("PatchConfigurationDialog.Title"), gameTitle);

        ContentDialog contentDialog = new ContentDialog
        {
            Title = dialogTitle,
            Content = dialog,
            PrimaryButtonText = LocalizationHelper.GetText("PatchConfigurationDialog.SaveButton"),
            CloseButtonText = LocalizationHelper.GetText("PatchConfigurationDialog.CancelButton"),
            FullSizeDesired = true,
            DefaultButton = ContentDialogButton.Primary
        };

        // Handle primary button (Save)
        contentDialog.PrimaryButtonClick += async (_, e) =>
        {
            bool success = await dialog._viewModel.SaveAsync();

            if (!success)
            {
                // Cancel the dialog close if save failed
                e.Cancel = true;
            }
            else
            {
                dialog.WasSaved = true;
            }
        };

        // TODO: Handle unsaved changes
        await contentDialog.ShowAsync();

        return dialog.WasSaved;
    }
}