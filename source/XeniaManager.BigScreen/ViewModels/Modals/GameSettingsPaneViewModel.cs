using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Config;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// The game modal's game settings pane: a full config editor (sections and
/// five control types) saved back to the game's config file.
/// </summary>
public partial class GameSettingsPaneViewModel : ViewModelBase, IGameModalPane, IDisposable
{
    private readonly Game _game;
    private ConfigFile _configFile;
    private readonly string _configFilePath;
    private readonly IModalService _modalService;
    private readonly List<ConfigOptionViewModel> _navigableOptions = [];

    /// <summary>
    /// The config sections shown in the pane.
    /// </summary>
    public ObservableCollection<ConfigSectionViewModel> Sections { get; } = [];

    /// <summary>
    /// The flattened display list for the virtualized view: each visible
    /// section header followed by its visible options. Only realized rows are
    /// instantiated, so opening the pane stays instant.
    /// </summary>
    public ObservableCollection<object> Items { get; } = [];

    /// <summary>
    /// Whether any option has unsaved changes.
    /// </summary>
    [ObservableProperty] private bool _hasUnsavedChanges;

    /// <summary>
    /// Raised when the controller navigates to an option, so the view can
    /// scroll it into view and focus its control.
    /// </summary>
    public event Action<ConfigOptionViewModel>? FocusOptionRequested;

    /// <summary>
    /// Raised after unsaved changes were saved or discarded, so the dialog
    /// closes the pane.
    /// </summary>
    public event Action? ExitRequested;

    /// <summary>
    /// Loads the game's config file (from the boot preload cache) and builds
    /// the sections from the shared UI definition.
    /// </summary>
    public GameSettingsPaneViewModel(Game game)
    {
        _game = game;
        _modalService = App.Services.GetRequiredService<IModalService>();
        _configFilePath = AppPathResolver.GetFullPath(game.FileLocations.Config);
        _configFile = GameDataCache.GetConfig(game);
        LoadSections();

        // Two-way bindings push control values into the option VMs while the
        // view attaches (clamping, snapping, control defaults). None of that is
        // a user edit - re-mark everything saved once the initial layout has
        // settled so a freshly opened pane never prompts about phantom changes.
        _ = Dispatcher.UIThread.InvokeAsync(MarkAllAsSaved, DispatcherPriority.Background);
    }

    /// <summary>
    /// Marks every section and option as saved (clears the phantom dirty flags
    /// caused by control binding attach).
    /// </summary>
    private void MarkAllAsSaved()
    {
        foreach (ConfigSectionViewModel section in Sections)
        {
            section.MarkAsSaved();
        }

        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Handles pane input: Up/Down moves through the visible options, A focuses
    /// the selected option's control, X saves and Back leaves (confirming when
    /// there are unsaved changes).
    /// </summary>
    public bool HandleInput(NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveUp:
                MoveSelection(-1);
                return true;
            case NavigationCommand.MoveDown:
                MoveSelection(1);
                return true;
            case NavigationCommand.Activate:
                FocusSelectedOption();
                return true;
            case NavigationCommand.CycleSort:
                SaveChanges();
                return true;
            case NavigationCommand.Back:
                return HasUnsavedChanges ? ConfirmExitAsync() : false;
            default:
                return false;
        }
    }

    /// <summary>
    /// Selects the first visible option when the pane becomes active.
    /// </summary>
    public void OnPaneEntered()
    {
        SelectionHelper.SelectOnlyAt(_navigableOptions, 0);
    }

    /// <summary>
    /// Clears the option selection when the pane loses focus.
    /// </summary>
    public void OnPaneExited()
    {
        foreach (ConfigOptionViewModel option in _navigableOptions)
        {
            option.IsSelected = false;
        }
    }

    /// <summary>
    /// Moves the option selection by the given step, clamped at both ends.
    /// </summary>
    private void MoveSelection(int delta)
    {
        if (_navigableOptions.Count == 0)
        {
            return;
        }

        int index = _navigableOptions.FindIndex(o => o.IsSelected);
        if (index < 0)
        {
            _navigableOptions[0].IsSelected = true;
            return;
        }

        int target = Math.Clamp(index + delta, 0, _navigableOptions.Count - 1);
        if (target != index)
        {
            _navigableOptions[index].IsSelected = false;
            _navigableOptions[target].IsSelected = true;
        }
    }

    /// <summary>
    /// Asks the view to scroll the selected option into view and focus it.
    /// </summary>
    private void FocusSelectedOption()
    {
        ConfigOptionViewModel? option = _navigableOptions.FirstOrDefault(o => o.IsSelected);
        if (option != null)
        {
            FocusOptionRequested?.Invoke(option);
        }
    }

    /// <summary>
    /// Loads the visible sections from the UI definition, skipping sections
    /// missing from the config file.
    /// </summary>
    private void LoadSections()
    {
        HasUnsavedChanges = false;
        Sections.Clear();

        foreach (ConfigSectionDefinition sectionDef in ConfigUiSettings.AllSettings.Sections)
        {
            if (!sectionDef.IsVisible)
            {
                continue;
            }

            ConfigSection? section = _configFile.GetSection(sectionDef.SectionName);
            if (section == null)
            {
                continue;
            }

            ConfigSectionViewModel sectionVm = new ConfigSectionViewModel(section, sectionDef);
            sectionVm.PropertyChanged += SectionViewModel_PropertyChanged;
            Sections.Add(sectionVm);
        }

        RebuildNavigableOptions();
        Logger.Debug<GameSettingsPaneViewModel>($"Loaded {Sections.Count} config sections");
    }

    /// <summary>
    /// Rebuilds the flat list of visible options used by the controller navigation.
    /// </summary>
    private void RebuildNavigableOptions()
    {
        foreach (ConfigOptionViewModel option in _navigableOptions)
        {
            option.IsSelected = false;
        }

        _navigableOptions.Clear();
        foreach (ConfigSectionViewModel section in Sections.Where(s => s.IsVisible))
        {
            _navigableOptions.AddRange(section.Options.Where(o => o.IsVisible));
        }

        RebuildItems();
    }

    /// <summary>
    /// Rebuilds the flattened display list (section headers + options) for the
    /// virtualized view.
    /// </summary>
    private void RebuildItems()
    {
        Items.Clear();
        foreach (ConfigSectionViewModel section in Sections.Where(s => s.IsVisible))
        {
            Items.Add(section);
            foreach (ConfigOptionViewModel option in section.Options.Where(o => o.IsVisible))
            {
                Items.Add(option);
            }
        }
    }

    private void SectionViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConfigSectionViewModel.HasUnsavedChanges))
        {
            HasUnsavedChanges = Sections.Any(s => s.HasUnsavedChanges);
        }
        else if (e.PropertyName == nameof(ConfigSectionViewModel.IsVisible))
        {
            RebuildNavigableOptions();
        }
    }

    /// <summary>
    /// Applies every section's changes and saves the config file.
    /// </summary>
    public void SaveChanges()
    {
        try
        {
            foreach (ConfigSectionViewModel sectionVm in Sections)
            {
                sectionVm.ApplyChanges();
            }

            _configFile.Save(_configFilePath);
            foreach (ConfigSectionViewModel sectionVm in Sections)
            {
                sectionVm.MarkAsSaved();
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
    /// Confirms saving or discarding the unsaved changes; after the choice the
    /// pane raises <see cref="ExitRequested"/> so the dialog closes it.
    /// </summary>
    private bool ConfirmExitAsync()
    {
        TaskUtilities.RunSafely<GameSettingsPaneViewModel>(async () =>
        {
            bool? choice = await _modalService.ShowAsync<bool?>(new ConfirmationModalViewModel(
                LocalizationHelper.GetText("GameModal.Settings.Unsaved.Title"),
                string.Format(LocalizationHelper.GetText("GameModal.Settings.Unsaved.Message"), _configFilePath),
                LocalizationHelper.GetText("GameModal.Settings.Unsaved.Save"),
                LocalizationHelper.GetText("GameModal.Settings.Unsaved.Discard")));
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

        return true;
    }

    /// <summary>
    /// Re-reads the config file from disk (updating the cache) and rebuilds the
    /// sections, discarding the unsaved changes.
    /// </summary>
    private void ReloadFromDisk()
    {
        Dispose();
        _configFile = GameDataCache.ReloadConfig(_game);
        LoadSections();
    }

    /// <summary>
    /// Disposes the section view models.
    /// </summary>
    public void Dispose()
    {
        foreach (ConfigSectionViewModel sectionVm in Sections)
        {
            sectionVm.PropertyChanged -= SectionViewModel_PropertyChanged;
            sectionVm.Dispose();
        }

        Sections.Clear();
    }
}
