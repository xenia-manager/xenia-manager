using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Controls;

/// <summary>
/// Dialog for selecting one or more game groups (checklist).
/// </summary>
public abstract class GroupSelectionDialog
{
    /// <summary>
    /// Shows a checklist dialog of available groups.
    /// </summary>
    /// <param name="groups">Groups the user can choose from.</param>
    /// <param name="title">Optional dialog title override.</param>
    /// <param name="message">Optional message override.</param>
    /// <returns>Selected groups, or null if cancelled / none selected.</returns>
    public static async Task<List<GameGroup>?> ShowAsync(
        List<GameGroup> groups,
        string? title = null,
        string? message = null,
        string? confirmButtonText = null)
    {
        Logger.Info<GroupSelectionDialog>($"Showing group selection dialog with {groups.Count} groups");

        if (groups.Count == 0)
        {
            Logger.Warning<GroupSelectionDialog>("No groups available for selection");
            return null;
        }

        ObservableCollection<GroupSelectableItem> items = new(
            groups.Select(g => new GroupSelectableItem(g)));

        ListBox listBox = new ListBox
        {
            ItemsSource = items,
            MinWidth = 320,
            MaxHeight = 360,
            SelectionMode = SelectionMode.Multiple
        };

        listBox.ItemTemplate = new FuncDataTemplate<GroupSelectableItem>((item, _) =>
        {
            CheckBox checkBox = new CheckBox
            {
                Content = item.Group.Name,
                IsChecked = item.IsSelected,
                Margin = new Avalonia.Thickness(4, 2)
            };
            checkBox.IsCheckedChanged += (_, _) =>
            {
                item.IsSelected = checkBox.IsChecked == true;
            };
            return checkBox;
        }, true);

        FAContentDialog dialog = new FAContentDialog
        {
            Title = title ?? LocalizationHelper.GetText("GroupSelectionDialog.Title"),
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock
                    {
                        Text = message ?? LocalizationHelper.GetText("GroupSelectionDialog.Message"),
                        TextWrapping = TextWrapping.Wrap
                    },
                    listBox
                }
            },
            PrimaryButtonText = confirmButtonText ?? LocalizationHelper.GetText("GroupSelectionDialog.Confirm"),
            CloseButtonText = LocalizationHelper.GetText("MessageBox.Cancel"),
            DefaultButton = FAContentDialogButton.Primary
        };

        FAContentDialogResult result = await dialog.ShowAsync();
        if (result != FAContentDialogResult.Primary)
        {
            Logger.Info<GroupSelectionDialog>("Group selection cancelled");
            return null;
        }

        List<GameGroup> selected = items.Where(i => i.IsSelected).Select(i => i.Group).ToList();
        if (selected.Count == 0)
        {
            Logger.Info<GroupSelectionDialog>("No groups selected");
            return null;
        }

        Logger.Info<GroupSelectionDialog>($"User selected {selected.Count} group(s)");
        return selected;
    }

    private sealed class GroupSelectableItem(GameGroup group)
    {
        public GameGroup Group { get; } = group;
        public bool IsSelected { get; set; }
    }
}
