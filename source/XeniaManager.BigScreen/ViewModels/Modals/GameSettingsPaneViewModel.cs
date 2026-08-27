using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Factories;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Files;
using XeniaManager.Logging;
using XeniaManager.Core.Models.Files.Config;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// The game modal's game settings pane: a curated list of primary config
/// options (toggles, sliders and combo boxes) rendered as main-settings-style
/// cards. Values commit straight to the game's config file - there is no
/// save prompt, so no unsaved-changes state exists.
/// </summary>
public partial class GameSettingsPaneViewModel : ViewModelBase, IGameModalPane
{
    /// <summary>
    /// The curated options shown in the pane, in display order (section name,
    /// option name). Only primary options - simple switches, dropdowns and
    /// sliders - make the cut.
    /// </summary>
    private static readonly (string Section, string Option)[] CuratedOptions =
    [
        ("Display", "fullscreen"),
        ("Display", "present_letterbox"),
        ("APU", "apu"),
        ("APU", "mute"),
        ("APU", "enable_xmp"),
        ("APU", "xma_decoder"),
        ("GPU", "gpu"),
        ("GPU", "async_shader_compilation"),
        ("GPU", "vsync"),
        ("GPU", "draw_resolution_scale_x"),
        ("GPU", "draw_resolution_scale_y"),
        ("General", "apply_patches"),
        ("General", "controller_hotkeys"),
        ("General", "discord"),
        ("HID", "vibration"),
        ("HID", "left_stick_deadzone_percentage"),
        ("HID", "right_stick_deadzone_percentage"),
        ("UI", "show_achievement_notification")
    ];

    private readonly Game _game;
    private readonly string _configFilePath;
    private readonly IModalService _modalService;
    private ConfigFile _configFile;

    /// <summary>
    /// The row whose combo editor is open, or null when the rows navigate
    /// normally (sliders step directly, no editor).
    /// </summary>
    private ConfigRowViewModel? _editorRow;

    /// <summary>
    /// The curated config rows shown in the pane.
    /// </summary>
    public ObservableCollection<ConfigRowViewModel> Rows { get; } = [];

    /// <summary>
    /// Whether any row has unsaved changes (drives the exit confirmation).
    /// </summary>
    [ObservableProperty]
    public partial bool HasUnsavedChanges { get; set; }

    /// <summary>
    /// Raised after unsaved changes were saved or discarded, so the modal
    /// returns to the options list.
    /// </summary>
    public event Action? ExitRequested;

    /// <summary>
    /// Raised when a row's editor opens, so the view can open/focus the
    /// matching native control.
    /// </summary>
    public event Action<ConfigRowViewModel>? EditorOpened;

    /// <summary>
    /// Raised when a row's editor closes (commit or cancel), so the view can
    /// close the native control.
    /// </summary>
    public event Action? EditorClosed;

    /// <summary>
    /// Raised after the selection moved, so the view can scroll the row into view.
    /// </summary>
    public event Action<ConfigRowViewModel>? RowSelectionChanged;

    /// <summary>
    /// Closes the editor and notifies the view to close the native control.
    /// </summary>
    private void CloseEditor()
    {
        _editorRow = null;
        EditorClosed?.Invoke();
    }

    /// <summary>
    /// Recomputes the unsaved-changes flag after a row's value changed.
    /// </summary>
    private void OnRowValueChanged()
    {
        HasUnsavedChanges = Rows.Any(row => row.IsDirty);
    }

    /// <summary>
    /// Writes the current values to the config file and marks every row saved.
    /// </summary>
    private void SaveChanges()
    {
        try
        {
            _configFile.Save(_configFilePath);
            foreach (ConfigRowViewModel row in Rows)
            {
                row.MarkAsSaved();
            }

            HasUnsavedChanges = false;
            Logger.Info<GameSettingsPaneViewModel>($"Saved config '{_configFilePath}'");
        }
        catch (Exception ex)
        {
            Logger.Error<GameSettingsPaneViewModel>($"Failed to save config '{_configFilePath}'");
            Logger.LogExceptionDetails<GameSettingsPaneViewModel>(ex);
        }
    }

    /// <summary>
    /// Steps the open slider editor by one increment, clamped to its range.
    /// </summary>
    private static void StepSlider(ConfigRowViewModel row, int delta)
    {
        double step = row.Step ?? 1;
        double min = row.Minimum ?? double.MinValue;
        double max = row.Maximum ?? double.MaxValue;
        row.FloatValue = Math.Clamp(row.FloatValue + delta * step, min, max);
    }

    /// <summary>
    /// Cycles the open combo editor's selection by the given step, wrapping at
    /// both ends.
    /// </summary>
    private static void CycleCombo(ConfigRowViewModel row, int delta)
    {
        int count = row.ComboBoxOptions?.Count ?? 0;
        if (count == 0)
        {
            return;
        }

        int current = row.SelectedIndex < 0 ? 0 : row.SelectedIndex;
        row.SelectedIndex = (current + delta + count) % count;
    }

    /// <summary>
    /// Moves the row selection by the given step, clamped at both ends. Rows
    /// start unselected - the first move selects the first row.
    /// </summary>
    private void MoveSelection(int delta)
    {
        int index = SelectionHelper.MoveSelection(Rows, delta);
        if (index >= 0)
        {
            RowSelectionChanged?.Invoke(Rows[index]);
        }
    }

    /// <summary>
    /// Opens the combo editor for the given row, snapshotting its value so a
    /// cancel can restore it.
    /// </summary>
    private void OpenEditor(ConfigRowViewModel row)
    {
        row.StartEdit();
        _editorRow = row;
        EditorOpened?.Invoke(row);
        Logger.Debug<GameSettingsPaneViewModel>($"Opened editor for '{row.Label}'");
    }

    /// <summary>
    /// Rebuilds the curated rows from the shared UI definitions, wiring each
    /// row's value changes to the unsaved-changes tracking.
    /// </summary>
    private void RebuildRows()
    {
        foreach (ConfigRowViewModel row in Rows)
        {
            row.ValueChanged -= OnRowValueChanged;
        }

        Rows.Clear();
        string? lastSection = null;
        foreach ((string sectionName, string optionName) in CuratedOptions)
        {
            ConfigSectionDefinition? sectionDef =
                ConfigUiSettings.AllSettings.Sections.FirstOrDefault(s => s.SectionName == sectionName);
            ConfigOptionDefinition? optionDef =
                sectionDef?.Options.FirstOrDefault(o => o.OptionName == optionName);
            ConfigOption? option = _configFile.GetSection(sectionName)?.GetOption(optionName);
            if (option == null || optionDef == null)
            {
                Logger.Warning<GameSettingsPaneViewModel>(
                    $"Skipping '{sectionName}.{optionName}': option or definition missing");
                continue;
            }

            string? sectionTitle = lastSection != sectionName ? sectionDef?.DisplayName ?? sectionName : null;
            lastSection = sectionName;
            ConfigRowViewModel row = new(option, optionDef, optionDef.DisplayName ?? option.Name, sectionTitle);
            row.ValueChanged += OnRowValueChanged;
            Rows.Add(row);
        }

        HasUnsavedChanges = false;
        Logger.Info<GameSettingsPaneViewModel>($"Game settings pane: {Rows.Count} curated rows for '{_game.Title}'");
    }

    /// <summary>
    /// Re-reads the config file from disk (updating the cache) and rebuilds
    /// the rows, discarding the unsaved changes.
    /// </summary>
    private void ReloadFromDisk()
    {
        foreach (ConfigRowViewModel row in Rows)
        {
            row.ValueChanged -= OnRowValueChanged;
        }

        _configFile = GameDataCache.ReloadConfig(_game);
        RebuildRows();
    }

    /// <summary>
    /// Commits the open editor: closes it, keeping the edited value (the save
    /// happens on X or the exit confirmation).
    /// </summary>
    private void CommitEditor()
    {
        ConfigRowViewModel? row = _editorRow;
        CloseEditor();
        Logger.Debug<GameSettingsPaneViewModel>($"Committed editor for '{row?.Label}'");
    }

    /// <summary>
    /// Restores the open editor's original value and closes it.
    /// </summary>
    private void CancelEditor()
    {
        ConfigRowViewModel? row = _editorRow;
        CloseEditor();
        row?.CancelEdit();
        Logger.Debug<GameSettingsPaneViewModel>($"Cancelled editor for '{row?.Label}'");
    }

    /// <summary>
    /// Activates the selected row: flips a toggle, or opens the combo editor.
    /// Sliders don't need an editor - Left/Right steps them directly.
    /// </summary>
    private void ActivateSelectedRow()
    {
        ConfigRowViewModel? row = Rows.FirstOrDefault(r => r.IsSelected);
        if (row == null)
        {
            return;
        }

        if (row.IsToggle)
        {
            row.BoolValue = !row.BoolValue;
            return;
        }

        if (row.IsComboBox)
        {
            OpenEditor(row);
        }
    }

    /// <summary>
    /// Steps the selected row's slider by one increment when it is a slider
    /// row; returns false so other rows fall through (B/Left exits the pane).
    /// </summary>
    private bool StepSelectedSlider(int delta)
    {
        ConfigRowViewModel? row = Rows.FirstOrDefault(r => r.IsSelected);
        if (row is { IsSlider: true })
        {
            StepSlider(row, delta);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Handles input while a combo editor is open: Up/Down cycles the options,
    /// A commits and B cancels (restoring the original value).
    /// </summary>
    private bool HandleEditorInput(NavigationCommand command)
    {
        ConfigRowViewModel? row = _editorRow;
        if (row == null)
        {
            return false;
        }

        switch (command)
        {
            case NavigationCommand.MoveUp:
                if (row.IsComboBox)
                {
                    CycleCombo(row, -1);
                }

                return true;
            case NavigationCommand.MoveDown:
                if (row.IsComboBox)
                {
                    CycleCombo(row, 1);
                }

                return true;
            case NavigationCommand.Activate:
                CommitEditor();
                return true;
            case NavigationCommand.Back:
                CancelEditor();
                return true;
            default:
                return true;
        }
    }

    /// <summary>
    /// Prompts to save or discard the unsaved changes; after the choice the
    /// pane raises <see cref="ExitRequested"/> so the modal returns to the
    /// options list. A cancelled prompt (B) keeps the pane open.
    /// </summary>
    private void ConfirmExitAsync()
    {
        TaskUtilities.RunSafely<GameSettingsPaneViewModel>(async () =>
        {
            bool? choice = await ModalFactory.ConfirmAsync(_modalService,
                LocalizationHelper.GetText("GameModal.Settings.Unsaved.Title"),
                string.Format(LocalizationHelper.GetText("GameModal.Settings.Unsaved.Message"), _game.Title),
                LocalizationHelper.GetText("GameModal.Settings.Unsaved.Save"),
                LocalizationHelper.GetText("GameModal.Settings.Unsaved.Discard"));
            if (choice == null)
            {
                return;
            }

            if (choice == true)
            {
                SaveChanges();
            }
            else
            {
                ReloadFromDisk();
            }

            ExitRequested?.Invoke();
        }, "Confirming unsaved config changes");
    }

    /// <summary>
    /// Handles pane input: Up/Down moves the rows, Left/Right steps the
    /// selected slider directly (no editor needed), A flips toggles or opens
    /// the combo editor. While a combo editor is open it takes the input
    /// instead: Up/Down cycles the options, A commits and B cancels.
    /// </summary>
    public bool HandleInput(NavigationCommand command)
    {
        if (_editorRow != null)
        {
            return HandleEditorInput(command);
        }

        switch (command)
        {
            case NavigationCommand.MoveUp:
                MoveSelection(-1);
                return true;
            case NavigationCommand.MoveDown:
                MoveSelection(1);
                return true;
            case NavigationCommand.MoveLeft:
                return StepSelectedSlider(-1);
            case NavigationCommand.MoveRight:
                return StepSelectedSlider(1);
            case NavigationCommand.Activate:
                ActivateSelectedRow();
                return true;
            case NavigationCommand.CycleSort:
                SaveChanges();
                return true;
            case NavigationCommand.Back:
                if (HasUnsavedChanges)
                {
                    ConfirmExitAsync();
                    return true;
                }

                return false;
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
    /// Clears the row selection when the pane loses focus.
    /// </summary>
    public void OnPaneExited()
    {
        CloseEditor();
        SelectionHelper.ClearSelection(Rows);
    }

    /// <summary>
    /// Loads the game's config file (from the boot preload cache) and builds
    /// the curated rows.
    /// </summary>
    public GameSettingsPaneViewModel(Game game)
    {
        _game = game;
        _modalService = App.Services.GetRequiredService<IModalService>();
        _configFilePath = AppPathResolver.GetFullPath(game.FileLocations.Config);
        _configFile = GameDataCache.GetConfig(game);
        RebuildRows();
    }
}