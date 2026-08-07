using System.Threading.Tasks;
using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Controls;

/// <summary>
/// Dialog for entering a new game group name.
/// </summary>
public abstract class GroupNameDialog
{
    /// <summary>
    /// Shows a dialog prompting the user for a group name.
    /// </summary>
    /// <param name="initialName">Optional prefilled name.</param>
    /// <returns>The trimmed group name, or null if cancelled / empty.</returns>
    public static async Task<string?> ShowAsync(string? initialName = null)
    {
        Logger.Info<GroupNameDialog>("Showing group name dialog");

        TextBox nameBox = new TextBox
        {
            PlaceholderText = LocalizationHelper.GetText("GroupNameDialog.Watermark"),
            Text = initialName ?? string.Empty,
            MinWidth = 320
        };

        FAContentDialog dialog = new FAContentDialog
        {
            Title = LocalizationHelper.GetText("GroupNameDialog.Title"),
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = LocalizationHelper.GetText("GroupNameDialog.Message"),
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap
                    },
                    nameBox
                }
            },
            PrimaryButtonText = LocalizationHelper.GetText("GroupNameDialog.Save"),
            CloseButtonText = LocalizationHelper.GetText("MessageBox.Cancel"),
            DefaultButton = FAContentDialogButton.Primary
        };

        FAContentDialogResult result = await dialog.ShowAsync();
        if (result != FAContentDialogResult.Primary)
        {
            Logger.Info<GroupNameDialog>("Group name dialog cancelled");
            return null;
        }

        string name = nameBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            Logger.Info<GroupNameDialog>("Group name dialog returned empty name");
            return null;
        }

        Logger.Info<GroupNameDialog>($"User entered group name: '{name}'");
        return name;
    }
}
