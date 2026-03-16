using System;
using System.Collections.Generic;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Bindings;
using XeniaManager.Core.Utilities;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.Controls;

/// <summary>
/// Represents a dialog that allows users to edit Xenia mousehook controls (bindings) for a game.
/// </summary>
public partial class MousehookControlsEditorDialog : UserControl
{
    /// <summary>
    /// The ViewModel containing the dialog's data and logic.
    /// </summary>
    private readonly MousehookControlsEditorDialogViewModel _viewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="MousehookControlsEditorDialog"/> class.
    /// </summary>
    public MousehookControlsEditorDialog()
    {
        InitializeComponent();
        _viewModel = new MousehookControlsEditorDialogViewModel(null!, new List<BindingsSection>());
        DataContext = _viewModel;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MousehookControlsEditorDialog"/> class with the specified bindings.
    /// </summary>
    /// <param name="bindingsFile">The bindings file to edit.</param>
    /// <param name="gameBindingsSections">List of bindings sections for the game.</param>
    public MousehookControlsEditorDialog(BindingsFile bindingsFile, List<BindingsSection> gameBindingsSections)
    {
        InitializeComponent();
        _viewModel = new MousehookControlsEditorDialogViewModel(bindingsFile, gameBindingsSections);
        DataContext = _viewModel;
    }

    /// <summary>
    /// Shows the mousehook controls editor dialog.
    /// </summary>
    /// <param name="bindingsFile">The bindings file to edit.</param>
    /// <param name="gameBindingsSections">List of bindings sections for the game.</param>
    public static async void Show(BindingsFile bindingsFile, List<BindingsSection> gameBindingsSections)
    {
        MousehookControlsEditorDialog dialog = new MousehookControlsEditorDialog(bindingsFile, gameBindingsSections);

        ContentDialog contentDialog = new ContentDialog
        {
            Title = LocalizationHelper.GetText("MousehookControlsEditorDialog.ContentDialog.Title"),
            Content = dialog,
            CloseButtonText = LocalizationHelper.GetText("MousehookControlsEditorDialog.ContentDialog.CloseButton.Text"),
            PrimaryButtonText = LocalizationHelper.GetText("MousehookControlsEditorDialog.ContentDialog.SaveButton.Text"),
            FullSizeDesired = true,
            DefaultButton = ContentDialogButton.Primary
        };

        // Controlling ContentDialog
        contentDialog.Resources.Add("ContentDialogMinWidth", 700.0);
        contentDialog.Resources.Add("ContentDialogMaxWidth", 1000.0);
        contentDialog.Resources.Add("ContentDialogMinHeight", 600.0);
        contentDialog.Resources.Add("ContentDialogMaxHeight", 800.0);

        try
        {
            ContentDialogResult result = await contentDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Save changes
                dialog._viewModel.SaveCommand.Execute(null);
            }
        }
        catch (Exception ex)
        {
            Logger.Error<MousehookControlsEditorDialog>("Error showing mousehook controls editor dialog");
            Logger.LogExceptionDetails<MousehookControlsEditorDialog>(ex);
        }
    }
}