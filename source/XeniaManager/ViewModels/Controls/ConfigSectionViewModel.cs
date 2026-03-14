using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.Core.Models.Files.Config;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for a configuration section in the config editor.
/// </summary>
public partial class ConfigSectionViewModel : ObservableObject, IDisposable
{
    private readonly ConfigSection _configSection;
    private readonly ConfigSectionDefinition? _definition;
    private bool _disposed;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private ObservableCollection<ConfigOptionViewModel> _options = [];
    [ObservableProperty] private bool _isExpanded = true;
    [ObservableProperty] private bool _isVisible = true;
    [ObservableProperty] private bool _hasUnsavedChanges;

    public ConfigSectionViewModel(ConfigSection configSection, ConfigSectionDefinition? definition = null)
    {
        _configSection = configSection;
        _definition = definition;
        Name = configSection.Name;
        DisplayName = definition?.DisplayName ?? configSection.Name;
        Description = definition?.CustomDescription ?? configSection.Description;
        IsExpanded = definition?.IsExpandedByDefault ?? true;

        if (definition is { Options.Count: > 0 })
        {
            // Load only the specified options from the definition
            foreach (ConfigOptionDefinition optionDef in definition.Options)
            {
                ConfigOption? option = configSection.GetOption(optionDef.OptionName);
                if (option != null)
                {
                    ConfigOptionViewModel optionVm = new ConfigOptionViewModel(option, optionDef);
                    optionVm.PropertyChanged += OptionViewModel_PropertyChanged;
                    Options.Add(optionVm);
                }
            }
        }
        else
        {
            // Load all options from the section
            foreach (ConfigOption option in configSection.OptionsReadOnly)
            {
                ConfigOptionViewModel optionVm = new ConfigOptionViewModel(option);
                optionVm.PropertyChanged += OptionViewModel_PropertyChanged;
                Options.Add(optionVm);
            }
        }

        // Update visibility and change status after loading all options
        UpdateVisibility();
        UpdateUnsavedChangesStatus();
    }

    /// <summary>
    /// Handles property changes from option ViewModels.
    /// </summary>
    private void OptionViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Update visibility if option visibility changed
        if (e.PropertyName == nameof(ConfigOptionViewModel.IsVisible))
        {
            UpdateVisibility();
        }
        // Update unsaved changes status
        else if (e.PropertyName == nameof(ConfigOptionViewModel.HasUnsavedChanges))
        {
            UpdateUnsavedChangesStatus();
        }
    }

    /// <summary>
    /// Updates the IsVisible flag based on whether any options are visible.
    /// </summary>
    private void UpdateVisibility()
    {
        // Section is visible if at least one option is visible
        IsVisible = Options.Any(o => o.IsVisible);
    }

    /// <summary>
    /// Updates the HasUnsavedChanges flag based on whether any options have unsaved changes.
    /// </summary>
    private void UpdateUnsavedChangesStatus()
    {
        HasUnsavedChanges = Options.Any(o => o.HasUnsavedChanges);
    }

    /// <summary>
    /// Applies all changes from the option ViewModels back to the underlying ConfigSection.
    /// </summary>
    public void ApplyChanges()
    {
        foreach (ConfigOptionViewModel optionVm in Options)
        {
            optionVm.ApplyChanges();
        }
    }

    /// <summary>
    /// Marks this section and all its options as saved.
    /// </summary>
    public void MarkAsSaved()
    {
        foreach (ConfigOptionViewModel optionVm in Options)
        {
            optionVm.MarkAsSaved();
        }

        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Disposes of resources used by this ViewModel.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Unsubscribe from all option events
        foreach (ConfigOptionViewModel optionVm in Options)
        {
            optionVm.PropertyChanged -= OptionViewModel_PropertyChanged;
            optionVm.Dispose();
        }

        Options.Clear();
        _disposed = true;
    }
}