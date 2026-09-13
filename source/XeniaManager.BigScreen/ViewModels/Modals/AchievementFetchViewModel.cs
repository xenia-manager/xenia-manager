using System.Collections.Generic;
using System.Linq;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// Unified fetch picker: what achievement data to fetch from the game files
/// (missing / unlocked-only / overwrite images, or achievement strings).
/// Up/Down moves, A confirms, B cancels (null).
/// </summary>
public class AchievementFetchViewModel : ModalViewModelBase<AchievementFetchOption?>
{
    /// <summary>
    /// The prompt header.
    /// </summary>
    public string Title
    {
        get
        {
            return LocalizationHelper.GetText("GameModal.Achievements.Fetch.Dialog.Title");
        }
    }

    /// <summary>
    /// The prompt message body.
    /// </summary>
    public string Message
    {
        get
        {
            return LocalizationHelper.GetText("GameModal.Achievements.Fetch.Dialog.Message");
        }
    }

    /// <summary>
    /// The fetch options, one row per choice.
    /// </summary>
    public List<AchievementFetchOptionItemViewModel> Options { get; }

    /// <summary>
    /// Confirms the clicked option (mouse path).
    /// </summary>
    public void SelectOption(AchievementFetchOptionItemViewModel option)
    {
        SelectionHelper.SelectOnly(Options, option);
        Close(option.Option);
    }

    /// <inheritdoc />
    public override bool HandleInput(NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveUp:
                SelectionHelper.MoveSelection(Options, -1);
                return true;
            case NavigationCommand.MoveDown:
                SelectionHelper.MoveSelection(Options, 1);
                return true;
            case NavigationCommand.Activate:
                AchievementFetchOptionItemViewModel? selected = Options.FirstOrDefault(o => o.IsSelected);
                if (selected != null)
                {
                    Close(selected.Option);
                }

                return true;
            case NavigationCommand.Back:
                Close(null);
                return true;
            default:
                return false;
        }
    }

    public AchievementFetchViewModel()
    {
        Options =
        [
            new AchievementFetchOptionItemViewModel(
                AchievementFetchOption.MissingImages,
                LocalizationHelper.GetText("GameModal.Achievements.Fetch.Dialog.ImagesMissing")),
            new AchievementFetchOptionItemViewModel(
                AchievementFetchOption.UnlockedImages,
                LocalizationHelper.GetText("GameModal.Achievements.Fetch.Dialog.ImagesUnlocked")),
            new AchievementFetchOptionItemViewModel(
                AchievementFetchOption.OverwriteImages,
                LocalizationHelper.GetText("GameModal.Achievements.Fetch.Dialog.ImagesOverwrite")),
            new AchievementFetchOptionItemViewModel(
                AchievementFetchOption.Strings,
                LocalizationHelper.GetText("GameModal.Achievements.Fetch.Dialog.Strings"))
        ];
        SelectionHelper.SelectOnlyAt(Options, 0);
    }
}