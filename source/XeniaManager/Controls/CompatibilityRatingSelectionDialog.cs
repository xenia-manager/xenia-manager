using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Controls;

/// <summary>
/// Provides functionality for displaying a dialog that allows users to select
/// which compatibility rating type to update.
/// </summary>
public abstract class CompatibilityRatingSelectionDialog
{
    /// <summary>
    /// Shows a dialog to let the user pick which compatibility rating to update.
    /// </summary>
    /// <returns>
    /// A tuple indicating which ratings to update, or null if the user cancelled.
    /// </returns>
    public static async Task<(bool Game, bool Mousehook, bool Netplay)?> ShowAsync()
    {
        Logger.Info<CompatibilityRatingSelectionDialog>("Showing compatibility rating selection dialog");

        CheckBox gameCheckBox = new CheckBox
        {
            Content = LocalizationHelper.GetText("CompatibilityRatingSelectionDialog.GameCompatibility"),
            IsChecked = true,
            Margin = new Thickness(0, 0, 0, 8)
        };

        CheckBox mousehookCheckBox = new CheckBox
        {
            Content = LocalizationHelper.GetText("CompatibilityRatingSelectionDialog.MousehookCompatibility"),
            IsChecked = true,
            Margin = new Thickness(0, 0, 0, 8)
        };

        CheckBox netplayCheckBox = new CheckBox
        {
            Content = LocalizationHelper.GetText("CompatibilityRatingSelectionDialog.NetplayCompatibility"),
            IsChecked = true
        };

        StackPanel content = new StackPanel
        {
            Children = { gameCheckBox, mousehookCheckBox, netplayCheckBox }
        };

        FAContentDialog dialog = new FAContentDialog
        {
            Title = LocalizationHelper.GetText("CompatibilityRatingSelectionDialog.Title"),
            Content = content,
            PrimaryButtonText = LocalizationHelper.GetText("CompatibilityRatingSelectionDialog.UpdateButton"),
            CloseButtonText = LocalizationHelper.GetText("MessageBox.Cancel"),
            DefaultButton = FAContentDialogButton.Primary
        };

        void UpdateButtonState(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "IsChecked")
            {
                dialog.IsPrimaryButtonEnabled = gameCheckBox.IsChecked == true
                                                || mousehookCheckBox.IsChecked == true
                                                || netplayCheckBox.IsChecked == true;
            }
        }

        gameCheckBox.PropertyChanged += UpdateButtonState;
        mousehookCheckBox.PropertyChanged += UpdateButtonState;
        netplayCheckBox.PropertyChanged += UpdateButtonState;

        FAContentDialogResult result = await dialog.ShowAsync();

        if (result != FAContentDialogResult.Primary)
        {
            Logger.Info<CompatibilityRatingSelectionDialog>("User cancelled selection or closed dialog without choosing");
            return null;
        }

        bool game = gameCheckBox.IsChecked == true;
        bool mousehook = mousehookCheckBox.IsChecked == true;
        bool netplay = netplayCheckBox.IsChecked == true;

        if (!game && !mousehook && !netplay)
        {
            Logger.Info<CompatibilityRatingSelectionDialog>("User confirmed with no selections, treating as cancel");
            return null;
        }

        Logger.Info<CompatibilityRatingSelectionDialog>($"User confirmed selection - Game: {game}, Mousehook: {mousehook}, Netplay: {netplay}");
        return (game, mousehook, netplay);
    }
}