using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.Core.Logging;
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
                    // Check if the actual config option type is compatible with the UI definition
                    if (!IsTypeCompatible(option, optionDef))
                    {
                        // Skip options where the type doesn't match the UI definition
                        Logger.Debug<ConfigSectionViewModel>(
                            $"Skipping option '{optionDef.OptionName}' in section '{configSection.Name}': " +
                            $"expected control type '{optionDef.ControlType}', but actual type is '{option.Type}'");
                        continue;
                    }

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
    /// Updates the IsVisible flag and expands the section if any options are visible.
    /// Called from the parent ViewModel during filtering.
    /// </summary>
    /// <param name="searchText">The current search text. Empty means show all.</param>
    public void UpdateVisibilityFromOptions(string searchText = "")
    {
        IsVisible = Options.Any(o => o.IsVisible);
        // Always keep sections expanded when visible
        IsExpanded = IsVisible;
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

    /// <summary>
    /// Checks if the actual config option type is compatible with the UI definition.
    /// </summary>
    /// <param name="option">The actual config option from the file.</param>
    /// <param name="definition">The UI definition specifying the expected control type.</param>
    /// <returns>True if the types are compatible, false otherwise.</returns>
    private static bool IsTypeCompatible(ConfigOption option, ConfigOptionDefinition definition)
    {
        // If no specific control type is defined in the definition, accept any type
        if (definition.ControlType == ConfigControlType.Auto)
        {
            return true;
        }

        // Check compatibility between actual type and expected control type
        switch (definition.ControlType)
        {
            case ConfigControlType.ToggleSwitch:
                // Toggle expects boolean
                return option.Type == ConfigOptionType.Boolean;

            case ConfigControlType.Slider:
            case ConfigControlType.NumberBox:
                // Slider/NumberBox expects numeric types
                return option.Type is ConfigOptionType.Integer or ConfigOptionType.Float;

            case ConfigControlType.ComboBox:
                // ComboBox can handle both String and Integer types (some options use integer enums)
                return option.Type is ConfigOptionType.String or ConfigOptionType.Integer;

            case ConfigControlType.TextBox:
                // TextBox can handle scalar types, but arrays need special handling
                return option.Type is not ConfigOptionType.Array;

            default:
                // Unknown control types - excluding arrays
                return option.Type is not ConfigOptionType.Array;
        }
    }
}