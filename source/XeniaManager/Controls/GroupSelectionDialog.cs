using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using Symbol = FluentIcons.Common.Symbol;
using SymbolIconSource = FluentIcons.Avalonia.Fluent.SymbolIconSource;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Controls;

/// <summary>
/// Dialog for selecting a game group to add a game into.
/// </summary>
public abstract class GroupSelectionDialog
{
    /// <summary>
    /// Shows a dialog listing available groups.
    /// </summary>
    /// <param name="groups">Groups the user can choose from.</param>
    /// <returns>The selected group, or null if cancelled.</returns>
    public static async Task<GameGroup?> ShowAsync(List<GameGroup> groups)
    {
        Logger.Info<GroupSelectionDialog>($"Showing group selection dialog with {groups.Count} groups");

        if (groups.Count == 0)
        {
            Logger.Warning<GroupSelectionDialog>("No groups available for selection");
            return null;
        }

        FATaskDialog taskDialog = new FATaskDialog
        {
            Title = LocalizationHelper.GetText("GroupSelectionDialog.Title"),
            Header = LocalizationHelper.GetText("GroupSelectionDialog.Header"),
            SubHeader = LocalizationHelper.GetText("GroupSelectionDialog.SubHeader"),
            IconSource = new SymbolIconSource { Symbol = Symbol.Folder },
            XamlRoot = App.MainWindow
        };

        List<FATaskDialogCommand> commands = [];
        foreach (GameGroup group in groups)
        {
            FATaskDialogCommand command = new FATaskDialogCommand
            {
                Text = group.Name,
                IconSource = new SymbolIconSource { Symbol = Symbol.Folder },
                ClosesOnInvoked = false
            };

            GameGroup capturedGroup = group;
            command.Click += (_, _) =>
            {
                Logger.Info<GroupSelectionDialog>($"User selected group '{capturedGroup.Name}'");
                taskDialog.Hide(capturedGroup);
            };
            commands.Add(command);
        }

        taskDialog.Commands = commands;
        taskDialog.Buttons =
        [
            FATaskDialogButton.CloseButton
        ];

        object result = await taskDialog.ShowAsync(true);
        if (result is GameGroup selected)
        {
            return selected;
        }

        Logger.Info<GroupSelectionDialog>("User cancelled group selection");
        return null;
    }
}
