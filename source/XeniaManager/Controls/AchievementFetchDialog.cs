using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAvalonia.UI.Controls;
using Symbol = FluentIcons.Common.Symbol;
using SymbolIconSource = FluentIcons.Avalonia.Fluent.SymbolIconSource;
using XeniaManager.Logging;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Controls;

/// <summary>
/// What to fetch from the game files.
/// </summary>
public enum AchievementFetchOption
{
    MissingImages,
    UnlockedImages,
    OverwriteImages,
    Strings
}

/// <summary>
/// Provides functionality for displaying a dialog that allows users to pick
/// what achievement data to fetch from the game files.
/// </summary>
public abstract class AchievementFetchDialog
{
    /// <summary>
    /// Shows a dialog letting the user pick what to fetch.
    /// </summary>
    /// <returns>The picked option, or null if cancelled</returns>
    public static async Task<AchievementFetchOption?> ShowAsync()
    {
        Logger.Info<AchievementFetchDialog>("Showing achievement fetch dialog");

        FATaskDialog taskDialog = new FATaskDialog
        {
            Title = LocalizationHelper.GetText("InstalledContentDialog.Achievements.Fetch.Dialog.Title"),
            SubHeader = LocalizationHelper.GetText("InstalledContentDialog.Achievements.Fetch.Dialog.Message"),
            IconSource = new SymbolIconSource
            {
                Symbol = Symbol.ArrowDownload
            },
            XamlRoot = App.MainWindow
        };

        List<(AchievementFetchOption Option, string LabelKey, Symbol Icon)> options =
        [
            (AchievementFetchOption.MissingImages, "InstalledContentDialog.Achievements.Fetch.Dialog.ImagesMissing", Symbol.Image),
            (AchievementFetchOption.UnlockedImages, "InstalledContentDialog.Achievements.Fetch.Dialog.ImagesUnlocked", Symbol.Image),
            (AchievementFetchOption.OverwriteImages, "InstalledContentDialog.Achievements.Fetch.Dialog.ImagesOverwrite", Symbol.Image),
            (AchievementFetchOption.Strings, "InstalledContentDialog.Achievements.Fetch.Dialog.Strings", Symbol.TextDescription)
        ];

        List<FATaskDialogCommand> commands = [];
        foreach ((AchievementFetchOption option, string labelKey, Symbol icon) in options)
        {
            FATaskDialogCommand command = new FATaskDialogCommand
            {
                Text = LocalizationHelper.GetText(labelKey),
                IconSource = new SymbolIconSource
                {
                    Symbol = icon
                },
                ClosesOnInvoked = false
            };

            AchievementFetchOption capturedOption = option; // avoid closure-over-loop-variable pitfall
            command.Click += (_, _) =>
            {
                Logger.Info<AchievementFetchDialog>($"User selected {capturedOption}");
                taskDialog.Hide((int)capturedOption);
            };

            commands.Add(command);
        }

        taskDialog.Commands = commands;
        taskDialog.Buttons = new List<FATaskDialogButton>
        {
            FATaskDialogButton.CloseButton
        };

        object result = await taskDialog.ShowAsync(true);

        if (result is int optionResult
            && optionResult >= (int)AchievementFetchOption.MissingImages
            && optionResult <= (int)AchievementFetchOption.Strings)
        {
            Logger.Info<AchievementFetchDialog>($"User confirmed {(AchievementFetchOption)optionResult}");
            return (AchievementFetchOption)optionResult;
        }

        Logger.Info<AchievementFetchDialog>("User cancelled achievement fetch");
        return null;
    }
}