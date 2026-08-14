using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Config;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// ViewModel for a configuration section in the game settings pane.
/// Port of the desktop's ConfigSectionViewModel.
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
            foreach (ConfigOptionDefinition optionDef in definition.Options)
            {
                ConfigOption? option = configSection.GetOption(optionDef.OptionName);
                if (option == null)
                {
                    continue;
                }

                if (!IsTypeCompatible(option, optionDef))
                {
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
        else
        {
            foreach (ConfigOption option in configSection.OptionsReadOnly)
            {
                ConfigOptionViewModel optionVm = new ConfigOptionViewModel(option);
                optionVm.PropertyChanged += OptionViewModel_PropertyChanged;
                Options.Add(optionVm);
            }
        }

        UpdateVisibility();
        UpdateUnsavedChangesStatus();
    }

    private void OptionViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConfigOptionViewModel.IsVisible))
        {
            UpdateVisibility();
        }
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
        IsVisible = Options.Any(o => o.IsVisible);
    }

    /// <summary>
    /// Updates the IsVisible flag based on the option visibility (search filtering).
    /// </summary>
    public void UpdateVisibilityFromOptions()
    {
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
    private static bool IsTypeCompatible(ConfigOption option, ConfigOptionDefinition definition)
    {
        if (definition.ControlType == ConfigControlType.Auto)
        {
            return true;
        }

        return definition.ControlType switch
        {
            ConfigControlType.ToggleSwitch => option.Type == ConfigOptionType.Boolean,
            ConfigControlType.Slider or ConfigControlType.NumberBox =>
                option.Type is ConfigOptionType.Integer or ConfigOptionType.Float,
            ConfigControlType.ComboBox => option.Type is ConfigOptionType.String or ConfigOptionType.Integer,
            ConfigControlType.TextBox => option.Type is not ConfigOptionType.Array,
            _ => option.Type is not ConfigOptionType.Array,
        };
    }
}
