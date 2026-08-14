using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
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
/// The mode the patches pane is currently in.
/// </summary>
public enum PatchesPaneMode
{
    /// <summary>
    /// The installed patch entries plus the download/remove actions.
    /// </summary>
    List,

    /// <summary>
    /// The selected entry's commands with the command editor on the right.
    /// </summary>
    Commands,
}

/// <summary>
/// The game modal's patches pane: download new patches (via the download
/// modal), enable/disable and edit the installed patch's entries and
/// commands, or remove the patch.
/// </summary>
public partial class PatchesPaneViewModel : ViewModelBase, IGameModalPane
{
    private readonly Game _game;
    private readonly IModalService _modalService;
    private PatchFile? _patchFile;
    private string? _patchFilePath;

    /// <summary>
    /// The rows shown in list mode: patch entries with the download and remove
    /// actions pinned at the ends.
    /// </summary>
    public ObservableCollection<PatchListRowViewModel> Rows { get; } = [];

    /// <summary>
    /// The currently selected patch entry (commands mode).
    /// </summary>
    [ObservableProperty] private PatchEntryItemViewModel? _selectedEntry;

    /// <summary>
    /// The command being edited in the commands mode editor, or null.
    /// </summary>
    [ObservableProperty] private PatchCommandItemViewModel? _editingCommand;

    /// <summary>
    /// The pane's current mode.
    /// </summary>
    [ObservableProperty] private PatchesPaneMode _mode = PatchesPaneMode.List;

    /// <summary>
    /// The pane header: the patch's title name, or a "no patch" label.
    /// </summary>
    public string HeaderText => _patchFile?.TitleName
        ?? LocalizationHelper.GetText("GameModal.Patches.NoPatch");

    /// <summary>
    /// Whether a patch file is installed.
    /// </summary>
    public bool HasPatch => _patchFile != null;

    /// <summary>
    /// The available patch types for the command editor combo box.
    /// </summary>
    public IReadOnlyList<PatchType> PatchTypes { get; } = Enum.GetValues<PatchType>();

    /// <summary>
    /// The commands of the selected entry (commands mode).
    /// </summary>
    public ObservableCollection<PatchCommandItemViewModel> Commands => SelectedEntry?.Commands ?? [];

    /// <summary>
    /// Whether the validation error is shown in the command editor.
    /// </summary>
    public bool ShowValidationError => EditingCommand is { IsValid: false };

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

    partial void OnSelectedEntryChanged(PatchEntryItemViewModel? value)
    {
        OnPropertyChanged(nameof(Commands));
        if (value != null)
        {
            EditingCommand = value.Commands.FirstOrDefault();
        }
    }

    partial void OnEditingCommandChanged(PatchCommandItemViewModel? value)
    {
        OnPropertyChanged(nameof(ShowValidationError));
    }

    /// <summary>
    /// Whether the pane is in list mode.
    /// </summary>
    public bool IsListMode => Mode == PatchesPaneMode.List;

    /// <summary>
    /// Whether the pane is in commands mode.
    /// </summary>
    public bool IsCommandsMode => Mode == PatchesPaneMode.Commands;

    partial void OnModeChanged(PatchesPaneMode value)
    {
        OnPropertyChanged(nameof(IsListMode));
        OnPropertyChanged(nameof(IsCommandsMode));
    }

    /// <summary>
    /// Selects the first row of the current mode (patch list or commands) when
    /// the pane becomes active.
    /// </summary>
    public void OnPaneEntered()
    {
        if (Mode == PatchesPaneMode.Commands)
        {
            SelectionHelper.SelectOnlyAt(Commands, 0);
        }
        else
        {
            SelectionHelper.SelectOnlyAt(Rows, 0);
        }
    }

    /// <summary>
    /// Clears the patch and command selections when the pane loses focus.
    /// </summary>
    public void OnPaneExited()
    {
        SelectionHelper.ClearSelection(Rows);
        SelectionHelper.ClearSelection(Commands);
    }

    /// <summary>
    /// Handles pane input per mode: list navigation/actions and command editing.
    /// </summary>
    public bool HandleInput(NavigationCommand command)
    {
        switch (Mode)
        {
            case PatchesPaneMode.List:
                return HandleListInput(command);
            case PatchesPaneMode.Commands:
                return HandleCommandsInput(command);
            default:
                return false;
        }
    }

    /// <summary>
    /// Handles list-mode input: Up/Down moves, A toggles/activates a row,
    /// Right opens the entry's commands.
    /// </summary>
    private bool HandleListInput(NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveUp:
                SelectionHelper.MoveSelection(Rows, -1);
                return true;
            case NavigationCommand.MoveDown:
                SelectionHelper.MoveSelection(Rows, 1);
                return true;
            case NavigationCommand.Activate:
                ActivateSelectedRow();
                return true;
            case NavigationCommand.MoveRight:
                OpenSelectedEntryCommands();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Handles commands-mode input: Up/Down moves the commands, A selects a
    /// command for editing, X saves, B returns to the list.
    /// </summary>
    private bool HandleCommandsInput(NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveUp:
                SelectionHelper.MoveSelection(Commands, -1);
                return true;
            case NavigationCommand.MoveDown:
                SelectionHelper.MoveSelection(Commands, 1);
                return true;
            case NavigationCommand.Activate:
                SelectCommandForEditing();
                return true;
            case NavigationCommand.CycleSort:
                SaveCommand();
                return true;
            case NavigationCommand.Back:
            case NavigationCommand.MoveLeft:
                EditingCommand = null;
                Mode = PatchesPaneMode.List;
                return true;
            default:
                return true;
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
    /// Selects the given command and opens it in the editor (mouse path).
    /// </summary>
    public void SelectCommand(PatchCommandItemViewModel command)
    {
        SelectionHelper.SelectOnly(Commands, command);
        SelectCommandForEditing();
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
    /// Opens the selected entry's commands (commands mode).
    /// </summary>
    private void OpenSelectedEntryCommands()
    {
        PatchListRowViewModel? row = Rows.FirstOrDefault(r => r.IsSelected);
        if (row?.Entry is { } entry)
        {
            SelectedEntry = entry;
            Mode = PatchesPaneMode.Commands;
            Logger.Debug<PatchesPaneViewModel>($"Editing commands of '{entry.Name}'");
        }
    }

    /// <summary>
    /// Selects the currently selected command for editing (focus moves to the
    /// editor panel).
    /// </summary>
    private void SelectCommandForEditing()
    {
        PatchCommandItemViewModel? command = Commands.FirstOrDefault(c => c.IsSelected);
        if (command != null)
        {
            EditingCommand = command;
        }
    }

    /// <summary>
    /// Saves the edited command (validated), then returns to the commands list.
    /// </summary>
    public void SaveCommand()
    {
        if (EditingCommand == null || !EditingCommand.Validate())
        {
            return;
        }

        SavePatchFile();
        EditingCommand = null;
        Logger.Debug<PatchesPaneViewModel>("Saved command changes");
    }

    /// <summary>
    /// Deletes the edited command and saves the patch file.
    /// </summary>
    public void DeleteEditingCommand()
    {
        if (EditingCommand == null || SelectedEntry == null)
        {
            return;
        }

        SelectedEntry.RemoveCommand(EditingCommand);
        SavePatchFile();
        EditingCommand = null;
    }

    /// <summary>
    /// Adds a new command to the selected entry and opens it in the editor.
    /// </summary>
    public void AddCommand()
    {
        if (SelectedEntry == null)
        {
            return;
        }

        PatchCommandItemViewModel command = SelectedEntry.AddCommand();
        SelectionHelper.SelectOnly(Commands, command);
        EditingCommand = command;
        Logger.Debug<PatchesPaneViewModel>($"Added command to '{SelectedEntry.Name}'");
    }

    /// <summary>
    /// Confirms and removes the installed patch, then reloads the list.
    /// </summary>
    private async Task RemovePatchAsync()
    {
        bool confirmed = await _modalService.ShowAsync<bool?>(new ConfirmationModalViewModel(
            LocalizationHelper.GetText("GameModal.Patches.Remove.Confirmation.Title"),
            string.Format(LocalizationHelper.GetText("GameModal.Patches.Remove.Confirmation.Message"), _game.Title),
            LocalizationHelper.GetText("GameModal.Patches.Remove.Confirm"),
            LocalizationHelper.GetText("Modal.Cancel"))) == true;
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
}
