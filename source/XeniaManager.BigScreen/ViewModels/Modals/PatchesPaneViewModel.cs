using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Factories;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models.Files.Patches;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// The game modal's patches pane: download new patches (via the download
/// modal), enable/disable the installed patch's entries, or remove the patch.
/// Entries are toggled in place; full editing stays in the main app.
/// </summary>
public partial class PatchesPaneViewModel : ViewModelBase, IGameModalPane
{
    private readonly Game _game;
    private readonly IModalService _modalService;
    private PatchFile? _patchFile;
    private string? _patchFilePath;

    /// <summary>
    /// Whether a patch file is installed.
    /// </summary>
    public bool HasPatch => _patchFile != null;

    /// <summary>
    /// The rows shown in list mode: patch entries with the download and remove
    /// actions pinned at the ends.
    /// </summary>
    public ObservableCollection<PatchListRowViewModel> Rows { get; } = [];

    /// <summary>
    /// Raised when the selection moves so the view can scroll the selected
    /// row into view.
    /// </summary>
    public event Action? ScrollRequested;

    /// <summary>
    /// The pane header: the patch's title name, or a "no patch" label.
    /// </summary>
    public string HeaderText => _patchFile?.TitleName
                                ?? LocalizationHelper.GetText("GameModal.Patches.NoPatch");

    /// <summary>
    /// Loads the game's patch file from the boot preload cache (when installed)
    /// and builds the list rows.
    /// </summary>
    private void ReloadPatch()
    {
        (_patchFile, _patchFilePath) = GameDataCache.GetPatch(_game);

        OnPropertyChanged(nameof(HeaderText));
        Rows.Clear();
        Rows.Add(new PatchListRowViewModel(
            LocalizationHelper.GetText("GameModal.Patches.Actions.Download"), PatchActionType.Download));
        if (_patchFile != null)
        {
            foreach (PatchEntry entry in _patchFile.Patches)
            {
                Rows.Add(new PatchListRowViewModel(new PatchEntryItemViewModel(entry)));
            }

            Rows.Add(new PatchListRowViewModel(
                LocalizationHelper.GetText("GameModal.Patches.Actions.Remove"), PatchActionType.Remove));
        }
    }

    /// <summary>
    /// Writes the current entry state back to the patch file and saves it.
    /// </summary>
    private void SavePatchFile()
    {
        if (_patchFile == null || _patchFilePath == null)
        {
            return;
        }

        try
        {
            _patchFile.Document.Patches = Rows
                .Where(row => row.Entry != null)
                .Select(row => row.Entry!.ToPatchEntry())
                .ToList();
            _patchFile.Save(_patchFilePath);
            Logger.Debug<PatchesPaneViewModel>($"Saved patch '{_patchFilePath}'");
        }
        catch (Exception ex)
        {
            Logger.Error<PatchesPaneViewModel>($"Failed to save patch '{_patchFilePath}'");
            Logger.LogExceptionDetails<PatchesPaneViewModel>(ex);
        }
    }

    /// <summary>
    /// Opens the patch download modal; the patch cache and list reload when it
    /// closes (a freshly downloaded patch shows up as an entry).
    /// </summary>
    private void OpenDownloadModal()
    {
        Logger.Info<PatchesPaneViewModel>($"Opening patch download for '{_game.Title}'");
        TaskUtilities.RunSafely<PatchesPaneViewModel>(async () =>
        {
            await _modalService.ShowAsync(new PatchDownloadViewModel(_game));
            GameDataCache.RefreshPatch(_game);
            ReloadPatch();
        }, "Opening patch download");
    }

    /// <summary>
    /// Confirms and removes the installed patch, then reloads the list.
    /// </summary>
    private async Task RemovePatchAsync()
    {
        bool confirmed = await ModalFactory.ConfirmAsync(_modalService,
            LocalizationHelper.GetText("GameModal.Patches.Remove.Confirmation.Title"),
            string.Format(LocalizationHelper.GetText("GameModal.Patches.Remove.Confirmation.Message"), _game.Title),
            LocalizationHelper.GetText("GameModal.Patches.Remove.Confirm"),
            LocalizationHelper.GetText("Modal.Cancel")) == true;
        if (!confirmed)
        {
            return;
        }

        try
        {
            await PatchManager.RemovePatchAsync(_game);
            Logger.Info<PatchesPaneViewModel>($"Removed patch for '{_game.Title}'");
            GameDataCache.RefreshPatch(_game);
            ReloadPatch();
        }
        catch (Exception ex)
        {
            Logger.Error<PatchesPaneViewModel>($"Failed to remove patch for '{_game.Title}'");
            Logger.LogExceptionDetails<PatchesPaneViewModel>(ex);
        }
    }

    /// <summary>
    /// Activates the selected list row: opens the download modal, toggles a
    /// patch entry, or removes the patch after confirmation.
    /// </summary>
    private void ActivateSelectedRow()
    {
        PatchListRowViewModel? row = Rows.FirstOrDefault(r => r.IsSelected);
        if (row == null)
        {
            return;
        }

        if (row.IsDownloadAction)
        {
            OpenDownloadModal();
        }
        else if (row.IsRemoveAction)
        {
            TaskUtilities.RunSafely<PatchesPaneViewModel>(
                RemovePatchAsync, "Removing patch");
        }
        else if (row.Entry is { } entry)
        {
            entry.IsEnabled = !entry.IsEnabled;
            SavePatchFile();
        }
    }

    /// <summary>
    /// Selects the given list row and activates it (mouse path).
    /// </summary>
    public void SelectRow(PatchListRowViewModel row)
    {
        SelectionHelper.SelectOnly(Rows, row);
        ActivateSelectedRow();
    }

    /// <summary>
    /// Handles pane input: Up/Down moves the rows, A toggles/activates a row.
    /// </summary>
    public bool HandleInput(NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveUp:
                SelectionHelper.MoveSelection(Rows, -1);
                ScrollRequested?.Invoke();
                return true;
            case NavigationCommand.MoveDown:
                SelectionHelper.MoveSelection(Rows, 1);
                ScrollRequested?.Invoke();
                return true;
            case NavigationCommand.Activate:
                ActivateSelectedRow();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Selects the first row when the pane becomes active.
    /// </summary>
    public void OnPaneEntered()
    {
        SelectionHelper.SelectOnlyAt(Rows, 0);
    }

    /// <summary>
    /// Clears the patch selection when the pane loses focus.
    /// </summary>
    public void OnPaneExited()
    {
        SelectionHelper.ClearSelection(Rows);
    }

    /// <summary>
    /// Loads the game's patch file (when installed) and builds the list rows.
    /// </summary>
    public PatchesPaneViewModel(Game game)
    {
        _game = game;
        _modalService = App.Services.GetRequiredService<IModalService>();
        Rows.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasPatch));
        ReloadPatch();
    }
}