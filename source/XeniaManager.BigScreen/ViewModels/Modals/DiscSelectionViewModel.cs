using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Logging;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// Disc selection modal shown when launching a multi-disc game: the game's
/// identity up top, its box art on the left and one card per disc on the right.
/// Only the disc cards take input - A launches the selected disc, B cancels.
/// </summary>
public class DiscSelectionViewModel : ModalViewModelBase<int?>
{
    /// <summary>
    /// Whether disc art is available to show.
    /// </summary>
    public bool HasIcon
    {
        get
        {
            return Icon != null;
        }
    }

    /// <summary>
    /// The Core game model this selection is for.
    /// </summary>
    public Game Game { get; }

    /// <summary>
    /// The game's display title.
    /// </summary>
    public string Title
    {
        get
        {
            return Game.Title;
        }
    }

    /// <summary>
    /// The game's disc art (icon), or null when missing/unreadable.
    /// </summary>
    public Bitmap? Icon
    {
        get
        {
            return Game.Artwork.CachedIcon;
        }
    }

    /// <summary>
    /// The disc cards, one per disc (Disc 1 + additional discs).
    /// </summary>
    public List<DiscOptionItemViewModel> Discs { get; }

    /// <summary>
    /// Builds one card per disc: Disc 1 from the main game file, then every
    /// additional disc with its custom label (or "Disc N").
    /// </summary>
    private static List<DiscOptionItemViewModel> BuildDiscs(Game game)
    {
        List<DiscOptionItemViewModel> discs =
        [
            new DiscOptionItemViewModel(
                1,
                LocalizationHelper.GetText("DiscSelection.Disc1"),
                game.LastPlayedDisc == 1,
                game.FileLocations.IsGamePathValid)
        ];

        foreach (GameDisc disc in game.FileLocations.AdditionalDiscs)
        {
            string label = string.IsNullOrWhiteSpace(disc.Label)
                ? string.Format(LocalizationHelper.GetText("DiscSelection.DiscN"), disc.DiscNumber)
                : disc.Label!;
            discs.Add(new DiscOptionItemViewModel(
                disc.DiscNumber,
                label,
                game.LastPlayedDisc == disc.DiscNumber,
                disc.IsPathValid));
        }

        return discs;
    }

    /// <summary>
    /// Selects the last played disc when its file exists, otherwise the first
    /// valid disc; no selection when every disc's file is missing.
    /// </summary>
    private void SelectDefaultDisc()
    {
        DiscOptionItemViewModel? lastPlayed = Discs.FirstOrDefault(d => d is { IsLastPlayed: true, IsPathValid: true });
        DiscOptionItemViewModel? target = lastPlayed ?? Discs.FirstOrDefault(d => d.IsPathValid);
        if (target != null)
        {
            SelectionHelper.SelectOnly(Discs, target);
        }
    }

    /// <summary>
    /// Index of the first valid disc, scanning from the left for a positive
    /// step and from the right otherwise. -1 when every disc is missing.
    /// </summary>
    private int FindFirstSelectable(int delta)
    {
        int index = delta > 0 ? 0 : Discs.Count - 1;
        int step = delta > 0 ? 1 : -1;
        for (int i = index; i >= 0 && i < Discs.Count; i += step)
        {
            if (Discs[i].IsPathValid)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Index of the next valid disc in the given direction, or the current
    /// index when the row ends (clamped - no wrap-back).
    /// </summary>
    private int FindNextSelectable(int index, int delta)
    {
        for (int i = index + delta; i >= 0 && i < Discs.Count; i += delta)
        {
            if (Discs[i].IsPathValid)
            {
                return i;
            }
        }

        return index;
    }

    /// <summary>
    /// Moves the selection by the given step, skipping cards whose file is
    /// missing. Stays put when no valid disc lies in that direction.
    /// </summary>
    private void MoveSelection(int delta)
    {
        int index = SelectionHelper.IndexOfSelected(Discs);
        int target = index < 0 ? FindFirstSelectable(delta) : FindNextSelectable(index, delta);
        if (target >= 0)
        {
            SelectionHelper.SelectOnlyAt(Discs, target);
            Logger.Trace<DiscSelectionViewModel>($"Moved disc selection by {delta}");
        }
    }

    /// <summary>
    /// Launches the selected disc (A), blocked when its file is missing.
    /// </summary>
    private void ActivateSelected()
    {
        DiscOptionItemViewModel? selected = Discs.FirstOrDefault(d => d.IsSelected);
        if (selected is { IsPathValid: true })
        {
            Logger.Info<DiscSelectionViewModel>($"User selected Disc {selected.DiscNumber}");
            Close(selected.DiscNumber);
        }
    }

    /// <summary>
    /// Launches the clicked disc (mouse path; a click selects and confirms in
    /// one step, mirroring the desktop dialog). Ignored for missing files.
    /// </summary>
    public void SelectDisc(DiscOptionItemViewModel disc)
    {
        if (!disc.IsPathValid)
        {
            return;
        }

        SelectionHelper.SelectOnly(Discs, disc);
        Logger.Info<DiscSelectionViewModel>($"User selected Disc {disc.DiscNumber}");
        Close(disc.DiscNumber);
    }

    /// <inheritdoc />
    public override bool HandleInput(NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveLeft:
                MoveSelection(-1);
                return true;
            case NavigationCommand.MoveRight:
                MoveSelection(1);
                return true;
            case NavigationCommand.Activate:
                ActivateSelected();
                return true;
            case NavigationCommand.Back:
                Close(null);
                return true;
            default:
                return false;
        }
    }

    public DiscSelectionViewModel(Game game)
    {
        Game = game;
        Discs = BuildDiscs(game);
        SelectDefaultDisc();
    }
}