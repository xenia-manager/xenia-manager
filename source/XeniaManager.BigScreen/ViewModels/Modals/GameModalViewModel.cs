using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Stfs;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// The game modal modal: the game's icon and title above a vertical options
/// list (Achievements, Screenshots, Title Updates, Marketplace Content, Patches,
/// Settings); the selected option's pane renders on the right and updates live
/// as the selection moves. A/Right enters the open pane, B/Left returns to the
/// options list, B there closes the dialog.
/// </summary>
public partial class GameModalViewModel : ModalViewModelBase
{
    /// <summary>
    /// The Core game model this dialog describes.
    /// </summary>
    public Game Game { get; }

    /// <summary>
    /// The game's display title.
    /// </summary>
    public string Title => Game.Title;

    /// <summary>
    /// The game's disc art (icon), or null when missing/unreadable.
    /// </summary>
    public Bitmap? Icon => Game.Artwork.CachedIcon;

    /// <summary>
    /// Whether disc art is available to show.
    /// </summary>
    public bool HasIcon => Icon != null;

    /// <summary>
    /// The options list shown on the left column.
    /// </summary>
    public List<GameActionItemViewModel> Options { get; }

    /// <summary>
    /// The currently selected option row.
    /// </summary>
    [ObservableProperty] private GameActionItemViewModel? _selectedOption;

    /// <summary>
    /// The pane displayed for the selected option, updated live on navigation.
    /// </summary>
    [ObservableProperty] private ViewModelBase? _pane;

    /// <summary>
    /// Whether input flows to the open pane instead of the options list.
    /// </summary>
    [ObservableProperty] private bool _isPaneActive;

    /// <summary>
    /// The pane instances created so far, keyed by option; navigating back to an
    /// option reuses its pane (no reloads).
    /// </summary>
    private readonly Dictionary<GameModalPane, ViewModelBase> _panes = [];

    /// <summary>
    /// The disposable panes created during this modal's lifetime, disposed on close.
    /// </summary>
    private readonly List<IDisposable> _disposables = [];

    /// <summary>
    /// The session-cached settings pane for this game, or null until first shown.
    /// Kept alive across modal opens (the config load runs once per session);
    /// this modal only subscribes to its exit request and unsubscribes on close.
    /// </summary>
    private GameSettingsPaneViewModel? _settingsPane;

    /// <summary>
    /// The X hint label while a pane is shown (sort / save), or empty when the
    /// shown pane has no X action.
    /// </summary>
    public string XHintText => Pane switch
    {
        AchievementsPaneViewModel => LocalizationHelper.GetText("GameModal.Hint.Sort"),
        GameSettingsPaneViewModel => LocalizationHelper.GetText("GameModal.Hint.Save"),
        _ => string.Empty,
    };

    /// <summary>
    /// Whether the X hint is shown (the shown pane has an X action).
    /// </summary>
    public bool IsXHintVisible => XHintText.Length > 0;

    /// <summary>
    /// The A hint label: what activating does in the current column (enter the
    /// pane from the nav list, or the pane's own A action).
    /// </summary>
    public string AHintText => Pane switch
    {
        AchievementsPaneViewModel => LocalizationHelper.GetText("GameModal.Hint.Select"),
        GameScreenshotsPaneViewModel => LocalizationHelper.GetText("GameModal.Hint.View"),
        InstalledContentPaneViewModel => LocalizationHelper.GetText("GameModal.Hint.Delete"),
        PatchesPaneViewModel => LocalizationHelper.GetText("GameModal.Hint.Toggle"),
        GameSettingsPaneViewModel => LocalizationHelper.GetText("GameModal.Hint.Edit"),
        _ => LocalizationHelper.GetText("GameModal.Hint.Select"),
    };

    partial void OnPaneChanged(ViewModelBase? value)
    {
        OnPropertyChanged(nameof(XHintText));
        OnPropertyChanged(nameof(IsXHintVisible));
        OnPropertyChanged(nameof(AHintText));
    }

    /// <summary>
    /// Toggles the single highlight on column switches: entering the pane
    /// clears the nav selection and selects the pane's first item; exiting
    /// clears the pane and re-selects the nav option.
    /// </summary>
    partial void OnIsPaneActiveChanged(bool value)
    {
        if (value)
        {
            SelectionHelper.ClearSelection(Options);
            if (Pane is IGameModalPane pane)
            {
                pane.OnPaneEntered();
            }
        }
        else
        {
            if (Pane is IGameModalPane pane)
            {
                pane.OnPaneExited();
            }

            if (SelectedOption != null)
            {
                SelectedOption.IsSelected = true;
            }
        }
    }

    /// <summary>
    /// Creates the dialog, builds its options list and shows the first pane.
    /// </summary>
    public GameModalViewModel(Game game)
    {
        Game = game;
        Options =
        [
            new GameActionItemViewModel(
                LocalizationHelper.GetText("GameModal.Action.Achievements"), "Trophy", GameModalPane.Achievements),
            new GameActionItemViewModel(
                LocalizationHelper.GetText("GameModal.Action.Screenshots"), "Camera", GameModalPane.Screenshots),
            new GameActionItemViewModel(
                LocalizationHelper.GetText("GameModal.Action.TitleUpdates"), "Document", GameModalPane.TitleUpdates),
            new GameActionItemViewModel(
                LocalizationHelper.GetText("GameModal.Action.MarketplaceContent"), "Tag",
                GameModalPane.MarketplaceContent),
            new GameActionItemViewModel(
                LocalizationHelper.GetText("GameModal.Action.Patches"), "DocumentText", GameModalPane.Patches),
            new GameActionItemViewModel(
                LocalizationHelper.GetText("GameModal.Action.Settings"), "Settings", GameModalPane.Settings),
        ];
        _selectedOption = Options[0];
        _selectedOption.IsSelected = true;
        _pane = GetOrCreatePane(_selectedOption.Pane);
    }

    /// <summary>
    /// Handles navigation input. While the pane is active it consumes input
    /// first (B/Left fall back to the options list); otherwise the options move
    /// and their panes display live, A/Right enter the pane, B closes the dialog.
    /// </summary>
    public override bool HandleInput(NavigationCommand command)
    {
        if (IsPaneActive && Pane is { } pane)
        {
            if (pane is IGameModalPane gamePane && gamePane.HandleInput(command))
            {
                return true;
            }

            if (command is NavigationCommand.Back or NavigationCommand.MoveLeft)
            {
                IsPaneActive = false;
            }

            return true;
        }

        switch (command)
        {
            case NavigationCommand.MoveUp:
                MoveSelection(-1);
                return true;
            case NavigationCommand.MoveDown:
                MoveSelection(1);
                return true;
            case NavigationCommand.Activate:
            case NavigationCommand.MoveRight:
                IsPaneActive = true;
                return true;
            case NavigationCommand.Back:
                Close();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Moves the options selection by the given step (clamped at both ends) and
    /// displays the newly selected option's pane.
    /// </summary>
    private void MoveSelection(int delta)
    {
        int target = SelectionHelper.MoveSelection(Options, delta);
        SelectedOption = Options[target];
        Pane = GetOrCreatePane(SelectedOption.Pane);
        Logger.Trace<GameModalViewModel>($"Moved game modal selection by {delta}");
    }

    /// <summary>
    /// Selects the given option and displays its pane (mouse path).
    /// </summary>
    public void OpenOption(GameActionItemViewModel option)
    {
        SelectionHelper.SelectOnly(Options, option);
        SelectedOption = option;
        Pane = GetOrCreatePane(option.Pane);
    }

    /// <summary>
    /// Returns the pane for the given option, creating and caching it on first use.
    /// </summary>
    private ViewModelBase GetOrCreatePane(GameModalPane pane)
    {
        if (_panes.TryGetValue(pane, out ViewModelBase? existing))
        {
            return existing;
        }

        ViewModelBase created = pane switch
        {
            GameModalPane.Achievements => new AchievementsPaneViewModel(Game),
            GameModalPane.Screenshots => new GameScreenshotsPaneViewModel(Game),
            GameModalPane.TitleUpdates => new InstalledContentPaneViewModel(Game, ContentType.Installer),
            GameModalPane.MarketplaceContent => new InstalledContentPaneViewModel(Game, ContentType.MarketplaceContent),
            GameModalPane.Patches => new PatchesPaneViewModel(Game),
            GameModalPane.Settings => CreateSettingsPane(),
            _ => throw new ArgumentOutOfRangeException(nameof(pane), pane, null),
        };
        _panes[pane] = created;
        if (created is IDisposable disposable && !ReferenceEquals(created, _settingsPane))
        {
            _disposables.Add(disposable);
        }

        return created;
    }

    /// <summary>
    /// Returns the session-cached settings pane for this game (created on first
    /// use), subscribing this modal to its exit request.
    /// </summary>
    private GameSettingsPaneViewModel CreateSettingsPane()
    {
        if (_settingsPane == null)
        {
            _settingsPane = GameSettingsPaneCache.GetOrCreate(Game);
            _settingsPane.ExitRequested += OnSettingsExitRequested;
        }

        return _settingsPane;
    }

    /// <summary>
    /// Returns the settings pane to the nav list after its changes were saved
    /// or discarded.
    /// </summary>
    private void OnSettingsExitRequested() => IsPaneActive = false;

    /// <summary>
    /// Disposes every pane created during this modal's lifetime. The cached
    /// settings pane stays alive for the session - only the subscription is
    /// released.
    /// </summary>
    public override void Dispose()
    {
        foreach (IDisposable disposable in _disposables)
        {
            disposable.Dispose();
        }

        if (_settingsPane != null)
        {
            _settingsPane.ExitRequested -= OnSettingsExitRequested;
        }

        base.Dispose();
    }
}
