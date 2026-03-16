using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Bindings;
using XeniaManager.Services;
using XeniaManager.ViewModels.Items;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for the mousehook controls the editor dialog.
/// Manages editing of Xenia mousehook bindings for a game.
/// </summary>
public partial class MousehookControlsEditorDialogViewModel : ViewModelBase
{
    private IMessageBoxService _messageBoxService;
    private readonly BindingsFile _bindingsFile;
    private List<BindingsSection> _gameBindingsSections;
    private BindingsSection? _currentSection;
    private int _currentSectionIndex;

    /// <summary>
    /// Gets or sets the list of available bindings sections for the game.
    /// </summary>
    public ObservableCollection<BindingsSection> BindingsSections { get; } = [];

    /// <summary>
    /// Gets or sets the currently selected bindings section.
    /// </summary>
    [ObservableProperty] private BindingsSection? _selectedBindingsSection;

    partial void OnSelectedBindingsSectionChanged(BindingsSection? value)
    {
        if (value != null)
        {
            _currentSection = value;
            _currentSectionIndex = BindingsSections.IndexOf(value);
            LoadEntriesForCurrentSection();
        }
    }

    /// <summary>
    /// Gets or sets the list of bindings entries for the current section.
    /// </summary>
    [ObservableProperty] private ObservableCollection<BindingsEntryViewModel> _bindingsEntries = [];

    partial void OnBindingsEntriesChanged(ObservableCollection<BindingsEntryViewModel> value)
    {
        UpdateEntryIndices();
        OnPropertyChanged(nameof(EntriesCountText));
    }

    /// <summary>
    /// Gets the text showing the count of entries.
    /// </summary>
    public string EntriesCountText => $"{BindingsEntries.Count} entries";

    /// <summary>
    /// Gets the list of available virtual key codes for the key combobox.
    /// </summary>
    public ObservableCollection<string> AvailableKeys { get; } = [];

    /// <summary>
    /// Gets the list of available XInput bindings for the value combobox.
    /// </summary>
    public ObservableCollection<string> AvailableValues { get; } = [];

    public MousehookControlsEditorDialogViewModel(BindingsFile bindingsFile, List<BindingsSection> gameBindingsSections)
    {
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
        _bindingsFile = bindingsFile;
        _gameBindingsSections = gameBindingsSections;

        // Populate available keys with all binding names (including alternatives)
        // Using HashSet to prevent duplicates
        HashSet<string> keysSet = [];
        foreach (VirtualKeyCode keyCode in Enum.GetValues<VirtualKeyCode>())
        {
            // Get all binding names for this key code
            FieldInfo? field = keyCode.GetType().GetField(keyCode.ToString());
            BindingNameAttribute? attribute = field?.GetCustomAttributes(typeof(BindingNameAttribute), false)
                .FirstOrDefault() as BindingNameAttribute;

            if (attribute != null)
            {
                // Add the main name
                keysSet.Add(attribute.Name);
                // Add all alternative names
                foreach (string alternative in attribute.Alternatives)
                {
                    keysSet.Add(alternative);
                }
            }
            else
            {
                keysSet.Add(keyCode.ToString());
            }
        }

        // Convert to ObservableCollection
        foreach (string key in keysSet)
        {
            AvailableKeys.Add(key);
        }

        // Populate available values with all binding names (including alternatives)
        // Using HashSet to prevent duplicates
        HashSet<string> valuesSet = [];
        foreach (XInputBinding binding in Enum.GetValues<XInputBinding>())
        {
            // Get all binding names for this binding
            FieldInfo? field = binding.GetType().GetField(binding.ToString());
            BindingNameAttribute? attribute = field?.GetCustomAttributes(typeof(BindingNameAttribute), false)
                .FirstOrDefault() as BindingNameAttribute;

            if (attribute != null)
            {
                // Add the main name
                valuesSet.Add(attribute.Name);
                // Add all alternative names
                foreach (string alternative in attribute.Alternatives)
                {
                    valuesSet.Add(alternative);
                }
            }
            else
            {
                valuesSet.Add(binding.ToString());
            }
        }

        // Convert to ObservableCollection
        foreach (string value in valuesSet)
        {
            AvailableValues.Add(value);
        }

        // Load sections
        foreach (BindingsSection section in _gameBindingsSections)
        {
            BindingsSections.Add(section);
        }

        // Select the first section if available
        if (BindingsSections.Count > 0)
        {
            SelectedBindingsSection = BindingsSections[0];
        }
    }

    /// <summary>
    /// Loads entries for the current section.
    /// </summary>
    private void LoadEntriesForCurrentSection()
    {
        BindingsEntries.Clear();

        if (_currentSection == null)
        {
            return;
        }

        int index = 0;
        foreach (BindingsEntry entry in _currentSection.Entries)
        {
            BindingsEntries.Add(new BindingsEntryViewModel(entry, index++, this));
        }

        OnPropertyChanged(nameof(EntriesCountText));
    }

    /// <summary>
    /// Updates the index property for all entries.
    /// </summary>
    private void UpdateEntryIndices()
    {
        for (int i = 0; i < BindingsEntries.Count; i++)
        {
            BindingsEntries[i].Index = i;
        }
    }

    /// <summary>
    /// Removes the specified entry.
    /// </summary>
    [RelayCommand]
    private void RemoveEntry(BindingsEntryViewModel? entry)
    {
        if (entry == null || _currentSection == null)
        {
            return;
        }

        // Remove from the underlying section
        _currentSection.Entries.Remove(entry.Entry);

        // Remove from ViewModel
        BindingsEntries.Remove(entry);

        // Update indices
        UpdateEntryIndices();

        // Update entries count
        OnPropertyChanged(nameof(EntriesCountText));

        Logger.Info<MousehookControlsEditorDialogViewModel>($"Removed entry: {entry.Key} = {entry.Value}");
    }

    /// <summary>
    /// Adds a new entry to the current section.
    /// </summary>
    [RelayCommand]
    private void AddEntry()
    {
        if (_currentSection == null)
        {
            return;
        }

        // Create a new entry with default values
        BindingsEntry newEntry = _currentSection.AddEntry("None", "None");
        BindingsEntryViewModel newViewModel = new BindingsEntryViewModel(newEntry, _currentSection.Entries.Count - 1, this);

        BindingsEntries.Add(newViewModel);
        UpdateEntryIndices();

        // Update entries count
        OnPropertyChanged(nameof(EntriesCountText));

        Logger.Info<MousehookControlsEditorDialogViewModel>($"Added new entry to section: {_currentSection.Name}");
    }

    /// <summary>
    /// Detects keyboard/mouse input for the specified entry.
    /// </summary>
    [RelayCommand]
    private async Task DetectInput(BindingsEntryViewModel? entry)
    {
        if (entry == null)
        {
            return;
        }

        // TODO: Implement input detection logic
        // This will listen for keyboard/mouse input and update the entry's Key and Value
        Logger.Info<MousehookControlsEditorDialogViewModel>($"Detect input triggered for entry: {entry.Key} = {entry.Value}");
        await _messageBoxService.ShowErrorAsync("Not implemented", "This feature is not implemented yet.");
    }

    /// <summary>
    /// Saves all changes to the bindings file.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        try
        {
            // Apply any pending changes from all entries
            foreach (BindingsEntryViewModel entry in BindingsEntries)
            {
                Logger.Debug<MousehookControlsEditorDialogViewModel>($"Saving entry: {entry.Key} = {entry.Value} (commented: {entry.IsCommented})");
                entry.ApplyChanges();
            }

            // Save the bindings file
            _bindingsFile.Save();

            Logger.Info<MousehookControlsEditorDialogViewModel>($"Successfully saved bindings file with {BindingsEntries.Count} entries");
        }
        catch (Exception ex)
        {
            Logger.Error<MousehookControlsEditorDialogViewModel>($"Failed to save bindings file");
            Logger.LogExceptionDetails<MousehookControlsEditorDialogViewModel>(ex);
            throw;
        }
    }
}