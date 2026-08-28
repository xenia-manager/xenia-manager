using System;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Modals;
using XeniaManager.Files.Models.Stfs;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.BigScreen.Factories;

/// <summary>
/// Creates the game modal's panes. Each pane is created fresh per option;
/// caching and lifetime tracking live in the modal's view model.
/// </summary>
public static class GameModalPaneFactory
{
    /// <summary>
    /// Creates the pane for the given game modal option.
    /// </summary>
    public static ViewModelBase Create(GameModalPane pane, Game game)
    {
        return pane switch
        {
            GameModalPane.Achievements => new AchievementsPaneViewModel(game),
            GameModalPane.Screenshots => new GameScreenshotsPaneViewModel(game),
            GameModalPane.TitleUpdates => new InstalledContentPaneViewModel(game, ContentType.Installer),
            GameModalPane.MarketplaceContent => new InstalledContentPaneViewModel(game, ContentType.MarketplaceContent),
            GameModalPane.Patches => new PatchesPaneViewModel(game),
            GameModalPane.Settings => new GameSettingsPaneViewModel(game),
            _ => throw new ArgumentOutOfRangeException(nameof(pane), pane, null)
        };
    }
}