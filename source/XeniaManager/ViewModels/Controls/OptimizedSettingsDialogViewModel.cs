using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Config;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;
using XeniaManager.ViewModels.Items;

namespace XeniaManager.ViewModels.Controls;

/// <summary>
/// ViewModel for the optimized settings dialog.
/// Shows all optimized settings that will be applied and allows the user to review/remove settings.
/// </summary>
public partial class OptimizedSettingsDialogViewModel : ObservableObject, IDisposable
{
    private readonly ConfigFile _currentConfigFile;
    private readonly ConfigFile _optimizedConfigFile;
    private readonly IMessageBoxService _messageBoxService;
    private bool _disposed;

    [ObservableProperty] private string _gameName;
    [ObservableProperty] private ObservableCollection<OptimizedSettingOptionViewModel> _settings = [];
    [ObservableProperty] private OptimizedSettingOptionViewModel? _selectedSetting;
    [ObservableProperty] private bool _hasSettings;

    /// <summary>
    /// Gets the message box service for showing dialogs.
    /// </summary>
    public IMessageBoxService MessageBoxService => _messageBoxService;

    public OptimizedSettingsDialogViewModel(ConfigFile currentConfigFile, ConfigFile optimizedConfigFile, string gameName)
    {
        _currentConfigFile = currentConfigFile;
        _optimizedConfigFile = optimizedConfigFile;
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
        _gameName = gameName;

        LoadSettings();
    }

    /// <summary>
    /// Loads and compares the settings from both config files.
    /// Only shows settings that exist in the current config with matching types.
    /// </summary>
    private void LoadSettings()
    {
        Settings.Clear();

        foreach (ConfigSection optimizedSection in _optimizedConfigFile.Sections)
        {
            ConfigSection? currentSection = _currentConfigFile.GetSection(optimizedSection.Name);

            foreach (ConfigOption optimizedOption in optimizedSection.OptionsReadOnly)
            {
                ConfigOption? currentOption = currentSection?.GetOption(optimizedOption.Name);

                // Skip if the option doesn't exist in the current config
                if (currentOption == null)
                {
                    Logger.Debug<OptimizedSettingsDialogViewModel>($"Skipping optimized setting '{optimizedSection.Name}.{optimizedOption.Name}' " +
                                                                   $"- not found in current config");
                    continue;
                }

                // Skip if types don't match
                if (currentOption.Type != optimizedOption.Type)
                {
                    Logger.Debug<OptimizedSettingsDialogViewModel>($"Skipping optimized setting '{optimizedSection.Name}.{optimizedOption.Name}' " +
                                                                   $"- type mismatch (current: {currentOption.Type}, optimized: {optimizedOption.Type})");
                    continue;
                }

                OptimizedSettingOptionViewModel settingVm = new OptimizedSettingOptionViewModel(optimizedSection.Name, optimizedOption, currentOption);

                Settings.Add(settingVm);
            }
        }

        HasSettings = Settings.Count > 0;
        Logger.Info<OptimizedSettingsDialogViewModel>($"Loaded {Settings.Count} optimized settings for comparison");
    }

    /// <summary>
    /// Removes the selected setting from the list.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelectedSetting))]
    private void RemoveSetting()
    {
        if (SelectedSetting == null)
        {
            return;
        }

        Logger.Debug<OptimizedSettingsDialogViewModel>($"Removing setting: {SelectedSetting.SectionName}.{SelectedSetting.OptionName}");
        Settings.Remove(SelectedSetting);
        SelectedSetting = null;
        HasSettings = Settings.Count > 0;
    }

    private bool HasSelectedSetting => SelectedSetting != null;

    /// <summary>
    /// Applies all remaining optimized settings to the current config file.
    /// </summary>
    public async Task<bool> ApplySettingsAsync()
    {
        try
        {
            Logger.Info<OptimizedSettingsDialogViewModel>($"Applying {Settings.Count} optimized settings");

            foreach (OptimizedSettingOptionViewModel setting in Settings)
            {
                ConfigSection currentSection = _currentConfigFile.GetOrCreateSection(setting.SectionNameValue);
                ConfigOption? currentOption = currentSection.GetOption(setting.OptionName);

                if (currentOption != null)
                {
                    // Update existing option
                    currentOption.Value = setting.OptimizedOption.Value;
                    if (!string.IsNullOrEmpty(setting.OptimizedOption.Comment))
                    {
                        currentOption.Comment = setting.OptimizedOption.Comment;
                    }
                }
                else
                {
                    // This shouldn't happen since we filtered them out, but just in case
                    currentSection.AddOption(setting.OptionName, setting.OptimizedOption.Value, setting.OptimizedOption.Comment, false, setting.OptimizedOption.Type);
                }

                Logger.Debug<OptimizedSettingsDialogViewModel>($"Applied setting: {setting.SectionNameValue}.{setting.OptionName} = {setting.NewValue}");
            }

            Logger.Info<OptimizedSettingsDialogViewModel>("Successfully applied all optimized settings");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error<OptimizedSettingsDialogViewModel>("Failed to apply optimized settings");
            Logger.LogExceptionDetails<OptimizedSettingsDialogViewModel>(ex);

            await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("OptimizedSettingsDialog.ApplySettings.Error.Title"),
                string.Format(LocalizationHelper.GetText("OptimizedSettingsDialog.ApplySettings.Error.Message"), ex.Message));

            return false;
        }
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

        Settings.Clear();
        _disposed = true;
    }
}