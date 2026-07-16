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
/// Provides functionality for displaying a dialog that allows users to pick which
/// disc of a multi-disc game to launch.
/// </summary>
public abstract class DiscSelectionDialog
{
    /// <summary>
    /// Shows a dialog letting the user pick which disc to launch.
    /// The disc that was played last (<see cref="Game.LastPlayedDisc"/>) is highlighted as the default.
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
        }

        taskDialog.Commands = commands;
        taskDialog.Buttons = new List<FATaskDialogButton>
        {
            FATaskDialogButton.CloseButton
        };

        object result = await taskDialog.ShowAsync(true);

        if (result is int discNumberResult)
        {
            Logger.Info<DiscSelectionDialog>($"User confirmed Disc {discNumberResult}");
            return discNumberResult;
        }

        Logger.Info<DiscSelectionDialog>("User cancelled disc selection");
        return null;
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
