using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Config;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for the configuration editor dialog.
/// Manages the collection of sections and options for editing a config file.
/// </summary>
public partial class ConfigEditorViewModel : ObservableObject, IDisposable
{
    private readonly ConfigFile _configFile;
    private readonly IMessageBoxService _messageBoxService;
    private readonly string? _configFilePath;
    private readonly ConfigUiDefinition? _uiDefinition;
    private bool _disposed;

    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private ObservableCollection<ConfigSectionViewModel> _sections = [];
    [ObservableProperty] private bool _hasUnsavedChanges;
    [ObservableProperty] private string _searchText = string.Empty;

    /// <summary>
    /// Gets the message box service for showing dialogs.
    /// </summary>
    public IMessageBoxService MessageBoxService => _messageBoxService;

    public ConfigEditorViewModel(ConfigFile configFile, string? configFilePath = null, ConfigUiDefinition? uiDefinition = null)
    {
        _configFile = configFile;
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
        _configFilePath = configFilePath;
        _uiDefinition = uiDefinition;

        LoadSections();
    }

    /// <summary>
    /// Handles changes to the search text and filters sections/options.
    /// </summary>
    partial void OnSearchTextChanged(string value)
    {
        FilterSections(value);
    }

    /// <summary>
    /// Loads the sections and options based on the UI definition or defaults.
    /// </summary>
    private void LoadSections()
    {
        HasUnsavedChanges = false;
        Sections.Clear();

        try
        {
            if (_uiDefinition is { Sections.Count: > 0 })
            {
                // Use the provided UI definition
                foreach (ConfigSectionDefinition sectionDef in _uiDefinition.Sections)
                {
                    if (!sectionDef.IsVisible)
                    {
                        continue;
                    }

                    ConfigSection? section = _configFile.GetSection(sectionDef.SectionName);

                    // Skip sections that don't exist in the config file
                    if (section == null)
                    {
                        continue;
                    }

                    ConfigSectionViewModel sectionVm = new ConfigSectionViewModel(section, sectionDef);
                    sectionVm.PropertyChanged += SectionViewModel_PropertyChanged;
                    Sections.Add(sectionVm);
                }
            }
        }
        catch
        {
            //
        }
    }

    /// <summary>
    /// Filters sections and options based on the search text.
    /// Searches by section names and config option names.
    /// </summary>
    private void FilterSections(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            // Show all sections and options when search is empty
            foreach (ConfigSectionViewModel sectionVm in Sections)
            {
                foreach (ConfigOptionViewModel optionVm in sectionVm.Options)
                {
                    optionVm.IsVisible = true;
                }
                sectionVm.UpdateVisibilityFromOptions();
            }
        }
        else
        {
            string searchLower = searchText.ToLowerInvariant();

            foreach (ConfigSectionViewModel sectionVm in Sections)
            {
                bool sectionNameMatches = sectionVm.DisplayName.ToLowerInvariant().Contains(searchLower) ||
                                          sectionVm.Name.ToLowerInvariant().Contains(searchLower);

                foreach (ConfigOptionViewModel optionVm in sectionVm.Options)
                {
                    // Option is visible if it matches OR if the section name matches
                    bool optionMatches = optionVm.DisplayName.ToLowerInvariant().Contains(searchLower) ||
                                         optionVm.Name.ToLowerInvariant().Contains(searchLower);
                    optionVm.IsVisible = optionMatches || sectionNameMatches;
                }

                // Section is visible if the section name matches OR if any of its options match
                sectionVm.UpdateVisibilityFromOptions(searchText);
            }
        }
    }

    /// <summary>
    /// Handles property changes from the section ViewModels.
    /// </summary>
    private void SectionViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ConfigSectionViewModel.HasUnsavedChanges))
        {
            UpdateUnsavedChangesStatus();
        }
    }

    /// <summary>
    /// Updates the overall unsaved changes status based on all sections.
    /// </summary>
    private void UpdateUnsavedChangesStatus()
    {
        HasUnsavedChanges = Sections.Any(s => s.HasUnsavedChanges);
    }

    /// <summary>
    /// Saves all changes to the config file.
    /// </summary>
    public async Task<bool> SaveAsync()
    {
        try
        {
            // Apply changes from all sections
            foreach (ConfigSectionViewModel sectionVm in Sections)
            {
                sectionVm.ApplyChanges();
            }

            // Save the config file if we have a path
            if (!string.IsNullOrEmpty(_configFilePath))
            {
                _configFile.Save(_configFilePath);
                Logger.Info<ConfigEditorViewModel>($"Successfully saved config file: {_configFilePath}");
            }
            else
            {
                Logger.Info<ConfigEditorViewModel>("Config changes applied (no file path specified)");
            }

            // Mark all sections as saved
            foreach (ConfigSectionViewModel sectionVm in Sections)
            {
                sectionVm.MarkAsSaved();
            }

            HasUnsavedChanges = false;
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error<ConfigEditorViewModel>("Failed to save config file");
            Logger.LogExceptionDetails<ConfigEditorViewModel>(ex);

            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("ConfigEditorDialog.Save.Failed.Title"),
                string.Format(LocalizationHelper.GetText("ConfigEditorDialog.Save.Failed.Message"), ex.Message));

            return false;
        }
    }

    /// <summary>
    /// Discards all changes and reloads the config from the file.
    /// </summary>
    public void DiscardChanges()
    {
        // Unsubscribe from events first
        foreach (ConfigSectionViewModel sectionVm in Sections)
        {
            sectionVm.PropertyChanged -= SectionViewModel_PropertyChanged;
        }

        LoadSections();
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

        // Unsubscribe from all section events
        foreach (ConfigSectionViewModel sectionVm in Sections)
        {
            sectionVm.PropertyChanged -= SectionViewModel_PropertyChanged;
            sectionVm.Dispose();
        }

        Sections.Clear();
        _disposed = true;
    }
}