using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using Symbol = FluentIcons.Common.Symbol;
using SymbolIconSource = FluentIcons.Avalonia.Fluent.SymbolIconSource;
using XeniaManager.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Controls;

/// <summary>
/// Provides functionality for displaying a dialog that allows users to select which Steam account to use
/// when multiple accounts are found in loginusers.vdf and no MostRecent flag is set.
/// </summary>
public abstract class SteamUserSelectionDialog
{
    /// <summary>
    /// Shows a dialog to allow the user to select a Steam account.
    /// Each account is displayed as "PersonaName (SteamID32)".
    /// </summary>
    /// <param name="users">A list of Steam users found in loginusers.vdf</param>
    /// <returns>The selected Steam user, or null if the user canceled the selection</returns>
    public static async Task<SteamUser?> ShowAsync(List<SteamUser> users)
    {
        Logger.Info<SteamUserSelectionDialog>(
            $"Showing Steam user selection dialog with {users.Count} account(s): [{string.Join(", ", users.Select(u => u.PersonaName))}]");

        FATaskDialog taskDialog = new FATaskDialog
        {
            Title = LocalizationHelper.GetText("SteamUserSelectionDialog.Title"),
            Header = LocalizationHelper.GetText("SteamUserSelectionDialog.Header"),
            SubHeader = LocalizationHelper.GetText("SteamUserSelectionDialog.SubHeader"),
            IconSource = new SymbolIconSource
            {
                Symbol = Symbol.People
            },
            XamlRoot = App.MainWindow
        };

        List<FATaskDialogCommand> commands = [];

        foreach (SteamUser user in users)
        {
            string displayText = user.SteamId32 != null
                ? $"{user.PersonaName} ({user.SteamId32})"
                : $"{user.PersonaName} ({user.SteamId64})";

            Logger.Trace<SteamUserSelectionDialog>($"Adding Steam account command: {displayText}");

            FATaskDialogCommand userCommand = new FATaskDialogCommand
            {
                Text = displayText,
                IconSource = new SymbolIconSource
                {
                    Symbol = Symbol.Person
                },
                ClosesOnInvoked = false
            };

            // Capture the user in the closure
            SteamUser capturedUser = user;
            userCommand.Click += (_, _) =>
            {
                Logger.Info<SteamUserSelectionDialog>($"User selected Steam account: {capturedUser.PersonaName} ({capturedUser.SteamId64})");
                taskDialog.Hide(capturedUser);
            };

            commands.Add(userCommand);
        }

        taskDialog.Commands = commands;

        taskDialog.Buttons = new List<FATaskDialogButton>
        {
            FATaskDialogButton.CloseButton
        };

        Logger.Debug<SteamUserSelectionDialog>("Showing Steam user selection dialog with available options");

        // ShowAsync returns the object passed to Hide()
        object result = await taskDialog.ShowAsync(true);

        if (result is SteamUser selectedUser)
        {
            Logger.Info<SteamUserSelectionDialog>($"User confirmed selection: {selectedUser.PersonaName}");
            return selectedUser;
        }

        Logger.Info<SteamUserSelectionDialog>("User cancelled selection or closed dialog without choosing");
        return null;
    }
}