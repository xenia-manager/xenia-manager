using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using Symbol = FluentIcons.Common.Symbol;
using SymbolIconSource = FluentIcons.Avalonia.Fluent.SymbolIconSource;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Services;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Controls;

/// <summary>
/// Provides functionality for displaying a dialog that allows users to pick which
/// disc of a multi-disc game to launch.
/// </summary>
public abstract class DiscSelectionDialog
{
    /// <summary>
    /// Shows a dialog letting the user pick which disc to launch.
    /// The disc that was played last (<see cref="Game.LastPlayedDisc"/>) is highlighted as the default.
    /// Supports controller navigation (D-Pad/left stick Up/Down between discs, A to confirm, B to cancel)
    /// while this dialog is open, taking over from whatever page opened it (see <see cref="XInputService"/>).
    /// </summary>
    /// <param name="game">The multi-disc game to show disc options for</param>
    /// <returns>The 1-based disc number the user picked, or null if cancelled</returns>
    public static async Task<int?> ShowAsync(Game game)
    {
        Logger.Info<DiscSelectionDialog>($"Showing disc selection dialog for '{game.Title}' ({game.FileLocations.DiscCount} discs)");

        FATaskDialog taskDialog = new FATaskDialog
        {
            Title = game.Title,
            Header = LocalizationHelper.GetText("DiscSelectionDialog.Header"),
            SubHeader = LocalizationHelper.GetText("DiscSelectionDialog.SubHeader"),
            IconSource = new SymbolIconSource { Symbol = Symbol.Games },
            XamlRoot = App.MainWindow
        };

        List<FATaskDialogCommand> commands = [];

        // Tracks which command index the controller navigation is currently pointing at,
        // independently of the dialog's own keyboard/mouse focus. Kept simple (an index into
        // the enabled subset) rather than relying on any FluentAvalonia-internal focus state,
        // since that's not something we can confirm the exact API surface of.
        List<FATaskDialogCommand> enabledCommands = [];
        int controllerCursorIndex = -1;

        void UpdateCursorVisual(int newIndex)
        {
            if (controllerCursorIndex >= 0 && controllerCursorIndex < enabledCommands.Count)
            {
                enabledCommands[controllerCursorIndex].Text = StripCursorPrefix(enabledCommands[controllerCursorIndex].Text);
            }

            controllerCursorIndex = newIndex;

            if (controllerCursorIndex >= 0 && controllerCursorIndex < enabledCommands.Count)
            {
                FATaskDialogCommand cmd = enabledCommands[controllerCursorIndex];
                cmd.Text = "\u25B8 " + StripCursorPrefix(cmd.Text); // "▸ " prefix marks the controller-selected option
            }
        }

        for (int discNumber = 1; discNumber <= game.FileLocations.DiscCount; discNumber++)
        {
            string? path = game.FileLocations.GetDiscPath(discNumber);
            bool isValid = !string.IsNullOrEmpty(path) && System.IO.File.Exists(path);
            bool isLastPlayed = discNumber == game.LastPlayedDisc;

            string label = discNumber == 1
                ? LocalizationHelper.GetText("DiscSelectionDialog.Disc1")
                : GetDiscLabel(game, discNumber);

            if (isLastPlayed)
            {
                label = $"{label} {LocalizationHelper.GetText("DiscSelectionDialog.LastPlayedSuffix")}";
            }

            if (!isValid)
            {
                label = $"{label} {LocalizationHelper.GetText("DiscSelectionDialog.MissingSuffix")}";
            }

            FATaskDialogCommand discCommand = new FATaskDialogCommand
            {
                Text = label,
                IconSource = new SymbolIconSource { Symbol = Symbol.Games },
                ClosesOnInvoked = false,
                IsEnabled = isValid
            };

            int capturedDiscNumber = discNumber; // avoid closure-over-loop-variable pitfall
            discCommand.Click += (_, _) =>
            {
                Logger.Info<DiscSelectionDialog>($"User selected Disc {capturedDiscNumber}");
                taskDialog.Hide(capturedDiscNumber);
            };

            commands.Add(discCommand);
            if (isValid)
            {
                enabledCommands.Add(discCommand);
            }
        }

        taskDialog.Commands = commands;
        taskDialog.Buttons = new List<FATaskDialogButton>
        {
            FATaskDialogButton.CloseButton
        };

        // Experimental: take over controller input from whatever page opened this dialog
        // (e.g. the Library grid) for the duration of the dialog. Up/Down moves a simple
        // text-prefix cursor between the enabled disc options (avoids depending on
        // FluentAvalonia's internal focus API, which isn't confirmed to be publicly
        // accessible the way we'd need); Confirm invokes the currently-marked option's
        // same Click handler used for mouse/keyboard; Back cancels the dialog.
        XInputService xInputService = App.Services.GetRequiredService<XInputService>();
        object navigationOwner = new object();

        EventHandler<ControllerNavigationAction>? controllerHandler = null;
        controllerHandler = (_, action) =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (!xInputService.IsActiveNavigationContext(navigationOwner) || enabledCommands.Count == 0)
                {
                    return;
                }

                switch (action)
                {
                    case ControllerNavigationAction.Up:
                    case ControllerNavigationAction.Left:
                        int prevIndex = controllerCursorIndex <= 0 ? enabledCommands.Count - 1 : controllerCursorIndex - 1;
                        UpdateCursorVisual(prevIndex);
                        break;
                    case ControllerNavigationAction.Down:
                    case ControllerNavigationAction.Right:
                        int nextIndex = controllerCursorIndex < 0 ? 0 : (controllerCursorIndex + 1) % enabledCommands.Count;
                        UpdateCursorVisual(nextIndex);
                        break;
                    case ControllerNavigationAction.Confirm:
                        if (controllerCursorIndex >= 0 && controllerCursorIndex < enabledCommands.Count)
                        {
                            InvokeDiscCommand(enabledCommands[controllerCursorIndex], taskDialog, game, controllerCursorIndex);
                        }
                        break;
                    case ControllerNavigationAction.Back:
                        Logger.Info<DiscSelectionDialog>("Controller Back pressed, cancelling disc selection");
                        taskDialog.Hide(null);
                        break;
                }
            });
        };

        xInputService.PushNavigationContext(navigationOwner);
        xInputService.NavigationActionTriggered += controllerHandler;

        // Start the controller cursor on the first enabled option so there's a visible
        // starting point as soon as the dialog opens
        if (enabledCommands.Count > 0)
        {
            Dispatcher.UIThread.Post(() => UpdateCursorVisual(0));
        }

        object result;
        try
        {
            result = await taskDialog.ShowAsync(true);
        }
        finally
        {
            xInputService.NavigationActionTriggered -= controllerHandler;
            xInputService.PopNavigationContext(navigationOwner);
        }

        if (result is int discNumberResult)
        {
            Logger.Info<DiscSelectionDialog>($"User confirmed Disc {discNumberResult}");
            return discNumberResult;
        }

        Logger.Info<DiscSelectionDialog>("User cancelled disc selection");
        return null;
    }

    /// <summary>
    /// Removes the "▸ " controller cursor prefix from a command's text, if present, so it
    /// can be safely re-applied to whichever command the cursor moves to next.
    /// </summary>
    private static string StripCursorPrefix(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }
        return text.StartsWith("\u25B8 ") ? text[2..] : text;
    }

    /// <summary>
    /// Finds which disc number the given (already-known-enabled) command corresponds to and
    /// hides the dialog with that result, mirroring exactly what the command's own Click
    /// handler does for mouse/keyboard interaction. Kept as a separate lookup (by matching
    /// on the command's label having already been captured) instead of re-deriving the disc
    /// number from scratch, since <see cref="ShowAsync"/> already knows this mapping.
    /// </summary>
    private static void InvokeDiscCommand(FATaskDialogCommand command, FATaskDialog taskDialog, Game game, int enabledIndex)
    {
        // The disc number for a given position among *enabled* commands isn't guaranteed to
        // equal (index + 1) if some discs are disabled (missing files) - so we recompute it
        // the same way ShowAsync built the list, by walking discs and skipping invalid ones.
        int seen = -1;
        for (int discNumber = 1; discNumber <= game.FileLocations.DiscCount; discNumber++)
        {
            string? path = game.FileLocations.GetDiscPath(discNumber);
            bool isValid = !string.IsNullOrEmpty(path) && System.IO.File.Exists(path);
            if (!isValid)
            {
                continue;
            }

            seen++;
            if (seen == enabledIndex)
            {
                Logger.Info<DiscSelectionDialog>($"User selected Disc {discNumber} via controller");
                taskDialog.Hide(discNumber);
                return;
            }
        }
    }

    /// <summary>
    /// Builds the display label for a disc, preferring its custom label when set.
    /// </summary>
    private static string GetDiscLabel(Game game, int discNumber)
    {
        int index = discNumber - 2; // AdditionalDiscs is 0-indexed starting at Disc 2
        if (index >= 0 && index < game.FileLocations.AdditionalDiscs.Count)
        {
            GameDisc disc = game.FileLocations.AdditionalDiscs[index];
            if (!string.IsNullOrWhiteSpace(disc.Label))
            {
                return disc.Label!;
            }
        }

        return string.Format(LocalizationHelper.GetText("DiscSelectionDialog.DiscN"), discNumber);
    }
}
