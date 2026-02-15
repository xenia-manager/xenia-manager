using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using Symbol = FluentIcons.Common.Symbol;
using SymbolIconSource = FluentIcons.Avalonia.Fluent.SymbolIconSource;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Controls;

/// <summary>
/// Provides functionality for displaying a dialog that allows users to select which version of Xenia to use
/// </summary>
public abstract class XeniaSelectionDialog
{
    /// <summary>
    /// Shows a dialog to allow the user to select which installed version of Xenia to use
    /// </summary>
    /// <param name="installedVersions">A list of Xenia versions that are currently installed</param>
    /// <returns>The selected Xenia version, or null if the user canceled the selection</returns>
    public static async Task<XeniaVersion?> ShowAsync(List<XeniaVersion> installedVersions)
    {
        Logger.Info<XeniaSelectionDialog>($"Showing Xenia selection dialog with {installedVersions.Count} installed versions: [{string.Join(", ", installedVersions)}]");

        bool canaryInstalled = installedVersions.Contains(XeniaVersion.Canary);
        bool mousehookInstalled = installedVersions.Contains(XeniaVersion.Mousehook);
        bool netplayInstalled = installedVersions.Contains(XeniaVersion.Netplay);

        Logger.Debug<XeniaSelectionDialog>($"Available versions - Canary: {canaryInstalled}, Mousehook: {mousehookInstalled}, Netplay: {netplayInstalled}");

        TaskDialog taskDialog = new TaskDialog
        {
            Title = LocalizationHelper.GetText("XeniaSelectionDialog.Title"),
            Header = LocalizationHelper.GetText("XeniaSelectionDialog.Header"),
            SubHeader = LocalizationHelper.GetText("XeniaSelectionDialog.SubHeader"),
            IconSource = new SymbolIconSource { Symbol = Symbol.Settings },
            XamlRoot = App.MainWindow
        };

        List<TaskDialogCommand> commands = [];

        if (canaryInstalled)
        {
            Logger.Trace<XeniaSelectionDialog>("Adding Canary command to dialog");
            TaskDialogCommand canaryCommand = new TaskDialogCommand
            {
                Text = LocalizationHelper.GetText("XeniaSelectionDialog.Canary.Title"),
                IconSource = new SymbolIconSource { Symbol = Symbol.Rocket },
                ClosesOnInvoked = false
            };
            canaryCommand.Click += (_, _) =>
            {
                Logger.Info<XeniaSelectionDialog>("User selected Canary version");
                taskDialog.Hide(XeniaVersion.Canary);
            };
            commands.Add(canaryCommand);
        }

        if (mousehookInstalled)
        {
            Logger.Trace<XeniaSelectionDialog>("Adding Mousehook command to dialog");
            TaskDialogCommand mousehookCommand = new TaskDialogCommand
            {
                Text = LocalizationHelper.GetText("XeniaSelectionDialog.Mousehook.Title"),
                IconSource = new SymbolIconSource { Symbol = Symbol.DesktopKeyboard },
                ClosesOnInvoked = false
            };
            mousehookCommand.Click += (_, _) =>
            {
                Logger.Info<XeniaSelectionDialog>("User selected Mousehook version");
                taskDialog.Hide(XeniaVersion.Mousehook);
            };
            commands.Add(mousehookCommand);
        }

        if (netplayInstalled)
        {
            Logger.Trace<XeniaSelectionDialog>("Adding Netplay command to dialog");
            TaskDialogCommand netplayCommand = new TaskDialogCommand
            {
                Text = LocalizationHelper.GetText("XeniaSelectionDialog.Netplay.Title"),
                IconSource = new SymbolIconSource { Symbol = Symbol.People },
                ClosesOnInvoked = false
            };
            netplayCommand.Click += (_, _) =>
            {
                Logger.Info<XeniaSelectionDialog>("User selected Netplay version");
                taskDialog.Hide(XeniaVersion.Netplay);
            };
            commands.Add(netplayCommand);
        }

        taskDialog.Commands = commands;

        taskDialog.Buttons = new List<TaskDialogButton>
        {
            TaskDialogButton.CloseButton
        };

        Logger.Debug<XeniaSelectionDialog>("Showing Xenia selection dialog with available options");

        // ShowAsync returns the object passed to Hide()
        object result = await taskDialog.ShowAsync(true);

        if (result is XeniaVersion version)
        {
            Logger.Info<XeniaSelectionDialog>($"User confirmed selection: {version}");
            return version;
        }

        Logger.Info<XeniaSelectionDialog>("User cancelled selection or closed dialog without choosing");
        return null;
    }
}