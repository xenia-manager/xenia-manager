using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Config;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Services;
using XeniaManager.Core.Utilities;
using XeniaManager.Core.Utilities.Paths;
using XeniaManager.Services;
using XeniaManager.ViewModels.Controls;

namespace XeniaManager.ViewModels.Pages;

/// <summary>
/// Represents a configuration file item in the settings page.
/// </summary>
public partial class ConfigFileItem
{
    public string DisplayName { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public bool IsEmulatorConfig { get; init; }
    public XeniaVersion EmulatorVersion { get; init; }
    public Game Game { get; init; } = new Game();
}

public partial class XeniaSettingsPageViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<ConfigFileItem> _configFiles = [];
    [ObservableProperty] private ConfigFileItem? _selectedConfigFile;
    /// <summary>
    /// Handles when a config file is selected to load it.
    /// </summary>
    partial void OnSelectedConfigFileChanged(ConfigFileItem? value)
    {
        if (value == null)
        {
            ConfigEditorViewModel = null;
            HasConfigFile = false;
            IsSelectedConfigEmulatorConfig = false;
            return;
        }

        IsSelectedConfigEmulatorConfig = value.IsEmulatorConfig;
        LoadConfigFile(value);
    }
    [ObservableProperty] private ConfigEditorViewModel? _configEditorViewModel;
    [ObservableProperty] private bool _hasConfigFile;
    [ObservableProperty] private bool _isSelectedConfigEmulatorConfig;
    [ObservableProperty] private string _currentConfigFilePath = string.Empty;

    private readonly IMessageBoxService _messageBoxService;

    public XeniaSettingsPageViewModel()
    {
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
        LoadAllConfigFiles();

        // Subscribe to game library changes to refresh the config file list
        EventManager.Instance.GameLibraryChanged += OnGameLibraryChanged;
    }

    /// <summary>
    /// Handles game library changes by refreshing the config file list.
    /// </summary>
    private void OnGameLibraryChanged()
    {
        LoadAllConfigFiles();
    }

    /// <summary>
    /// Loads all emulator and game config files into a single list.
    /// Emulator configs are loaded first, then game configs.
    /// </summary>
    private void LoadAllConfigFiles()
    {
        Logger.Info<XeniaSettingsPageViewModel>("Loading all configuration files");
        ConfigFiles.Clear();

        // First, add emulator configs (if they are installed)
        foreach (XeniaVersion version in Enum.GetValues<XeniaVersion>())
        {
            if (version == XeniaVersion.Custom)
            {
                continue;
            }
            try
            {
                XeniaVersionInfo versionInfo = XeniaVersionInfo.GetXeniaVersionInfo(version);
                string configPath = AppPathResolver.GetFullPath(versionInfo.ConfigLocation);

                if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
                {
                    ConfigFiles.Add(new ConfigFileItem
                    {
                        DisplayName = $"Xenia {version}",
                        FilePath = configPath,
                        IsEmulatorConfig = true,
                        EmulatorVersion = version
                    });
                    Logger.Debug<XeniaSettingsPageViewModel>($"Added emulator config: {configPath}");
                }
            }
            catch (Exception ex)
            {
                // Skip if the emulator version is not installed
                Logger.Warning<XeniaSettingsPageViewModel>($"Failed to load emulator config for {version}");
                Logger.LogExceptionDetails<XeniaSettingsPageViewModel>(ex);
            }
        }

        // Then, add game configs
        foreach (Game game in GameManager.Games)
        {
            string configPath = AppPathResolver.GetFullPath(game.FileLocations.Config);
            if (!string.IsNullOrEmpty(configPath) && File.Exists(configPath))
            {
                ConfigFiles.Add(new ConfigFileItem
                {
                    DisplayName = game.Title,
                    FilePath = configPath,
                    IsEmulatorConfig = false,
                    EmulatorVersion = game.XeniaVersion,
                    Game = game
                });
                Logger.Debug<XeniaSettingsPageViewModel>($"Added game config: {configPath}");
            }
        }

        Logger.Info<XeniaSettingsPageViewModel>($"Loaded {ConfigFiles.Count} configuration file(s)");

        // Select the first config if available
        if (ConfigFiles.Count > 0)
        {
            SelectedConfigFile = ConfigFiles[0];
        }
    }

    /// <summary>
    /// Loads the specified configuration file.
    /// </summary>
    private void LoadConfigFile(ConfigFileItem configItem)
    {
        try
        {
            if (configItem.EmulatorVersion != XeniaVersion.Custom)
            {
                if (string.IsNullOrEmpty(configItem.FilePath) || !File.Exists(configItem.FilePath))
                {
                    HasConfigFile = false;
                    ConfigEditorViewModel = null;
                    return;
                }

                HasConfigFile = true;
                CurrentConfigFilePath = configItem.FilePath;

                // Load the config file and create the ConfigEditorViewModel
                ConfigFile configFile = ConfigFile.Load(configItem.FilePath);
                ConfigEditorViewModel = new ConfigEditorViewModel(configFile,
                    configItem.FilePath,
                    ConfigUiSettings.AllSettings);
            }
            else
            {
                // Try to find the.config.toml file next to the CustomEmulatorExecutable
                string? emulatorExecutableName = Path.GetFileNameWithoutExtension(configItem.Game.FileLocations.CustomEmulatorExecutable)?.Replace('_', '-');
                string? emulatorFolder = Path.GetDirectoryName(configItem.Game.FileLocations.CustomEmulatorExecutable);
                string configPath = string.Empty;
                if (emulatorFolder != null)
                {
                    configPath = Path.Combine(emulatorFolder, $"{emulatorExecutableName}.config.toml");
                }
                if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                {
                    HasConfigFile = false;
                    ConfigEditorViewModel = null;
                    return;
                }
                HasConfigFile = true;
                CurrentConfigFilePath = configPath;
                // Load the config file and create the ConfigEditorViewModel
                ConfigFile configFile = ConfigFile.Load(configPath);
                ConfigEditorViewModel = new ConfigEditorViewModel(configFile,
                    configPath,
                    ConfigUiSettings.AllSettings);
            }
        }
        catch (Exception ex)
        {
            HasConfigFile = false;
            ConfigEditorViewModel = null;
            Logger.Error<XeniaSettingsPageViewModel>($"Failed to load config '{configItem.DisplayName}'");
            Logger.LogExceptionDetails<XeniaSettingsPageViewModel>(ex);

            // Show an error message to the user
            _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("XeniaSettingsPage.LoadConfig.Error.Title"),
                string.Format(LocalizationHelper.GetText("XeniaSettingsPage.LoadConfig.Error.Message"),
                    configItem.DisplayName, ex.Message));
        }
    }

    [RelayCommand]
    private async Task ResetSettings()
    {
        if (SelectedConfigFile == null)
        {
            Logger.Warning<XeniaSettingsPageViewModel>("No configuration file selected");
            await _messageBoxService.ShowWarningAsync(
                LocalizationHelper.GetText("XeniaSettingsPage.ResetSettings.NoSelection.Title"),
                LocalizationHelper.GetText("XeniaSettingsPage.ResetSettings.NoSelection.Message"));
            return;
        }

        // Confirm the reset action with the user
        bool confirmed = await _messageBoxService.ShowConfirmationAsync(
            LocalizationHelper.GetText("XeniaSettingsPage.ResetSettings.Confirmation.Title"),
            string.Format(LocalizationHelper.GetText("XeniaSettingsPage.ResetSettings.Confirmation.Message"), SelectedConfigFile.DisplayName));

        if (!confirmed)
        {
            Logger.Info<XeniaSettingsPageViewModel>("Reset settings cancelled by user");
            return;
        }

        Logger.Info<XeniaSettingsPageViewModel>($"Resetting configuration file: {SelectedConfigFile.DisplayName}");

        try
        {
            XeniaVersionInfo xeniaVersionInfo = XeniaVersionInfo.GetXeniaVersionInfo(SelectedConfigFile.EmulatorVersion);

            if (SelectedConfigFile.IsEmulatorConfig)
            {
                // Delete current config file
                if (File.Exists(AppPathResolver.GetFullPath(xeniaVersionInfo.ConfigLocation)))
                {
                    File.Delete(AppPathResolver.GetFullPath(xeniaVersionInfo.ConfigLocation));
                    Logger.Debug<XeniaSettingsPageViewModel>($"Deleted config file: {xeniaVersionInfo.ConfigLocation}");
                }
                // Delete the default config file if it exists
                if (File.Exists(AppPathResolver.GetFullPath(xeniaVersionInfo.DefaultConfigLocation)))
                {
                    File.Delete(AppPathResolver.GetFullPath(xeniaVersionInfo.DefaultConfigLocation));
                    Logger.Debug<XeniaSettingsPageViewModel>($"Deleted default config file: {xeniaVersionInfo.DefaultConfigLocation}");
                }

                // Regenerate the default config file
                ConfigManager.GenerateEmulatorConfigurationFile(SelectedConfigFile.EmulatorVersion, false);
                Logger.Info<XeniaSettingsPageViewModel>($"Generated new default config for Xenia {SelectedConfigFile.EmulatorVersion}");
            }
            else
            {
                // Generate default config for game's Xenia version
                string configPath = Path.Combine(AppPathResolver.GetFullPath(SelectedConfigFile.Game.FileLocations.Config));
                ConfigManager.CreateConfigurationFile(configPath, SelectedConfigFile.Game.XeniaVersion);
                Logger.Info<XeniaSettingsPageViewModel>($"Generated new default config for game: {SelectedConfigFile.DisplayName}");
            }

            // Reload the config file and UI
            LoadConfigFile(SelectedConfigFile);

            Logger.Info<XeniaSettingsPageViewModel>($"Configuration reset successfully for: {SelectedConfigFile.DisplayName}");

            await _messageBoxService.ShowInfoAsync(
                LocalizationHelper.GetText("XeniaSettingsPage.ResetSettings.Success.Title"),
                string.Format(LocalizationHelper.GetText("XeniaSettingsPage.ResetSettings.Success.Message"),
                    SelectedConfigFile.DisplayName));
        }
        catch (Exception ex)
        {
            Logger.Error<XeniaSettingsPageViewModel>($"Failed to reset configuration file: {ex.Message}");
            Logger.LogExceptionDetails<XeniaSettingsPageViewModel>(ex);

            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("XeniaSettingsPage.ResetSettings.Failure.Title"),
                string.Format(LocalizationHelper.GetText("XeniaSettingsPage.ResetSettings.Failure.Message"),
                    SelectedConfigFile.DisplayName, ex.Message));
        }
    }

    [RelayCommand]
    private async Task OptimizeSettings()
    {
        // TODO: Add this functionality once `Optimized Settings` repository is moved to using .TOML files
        await _messageBoxService.ShowErrorAsync("Not implemented", "This feature is not implemented yet.");
    }

    /// <summary>
    /// Opens the selected configuration file in the default text editor.
    /// </summary>
    [RelayCommand]
    private async Task OpenInEditor()
    {
        if (SelectedConfigFile == null)
        {
            Logger.Warning<XeniaSettingsPageViewModel>("No configuration file selected");
            await _messageBoxService.ShowWarningAsync(
                LocalizationHelper.GetText("XeniaSettingsPage.OpenInEditor.NoSelection.Title"),
                LocalizationHelper.GetText("XeniaSettingsPage.OpenInEditor.NoSelection.Message"));
            return;
        }

        string configPath = SelectedConfigFile.Game.XeniaVersion != XeniaVersion.Custom ? SelectedConfigFile.FilePath : CurrentConfigFilePath;
        Logger.Info<XeniaSettingsPageViewModel>($"Attempting to open configuration file: {configPath}");

        ProcessStartInfo startInfo;
        Process? process;
        try
        {
            startInfo = new ProcessStartInfo
            {
                FileName = configPath,
                UseShellExecute = true
            };
            process = Process.Start(startInfo);
            if (process is null)
            {
                Logger.Error<XeniaSettingsPageViewModel>("Process.Start returned null");
                throw new Exception("Failed to open the configuration file with default app.");
            }

            Logger.Info<XeniaSettingsPageViewModel>("Configuration file opened successfully");
        }
        catch (Win32Exception)
        {
            Logger.Warning<XeniaSettingsPageViewModel>("Win32Exception occurred, trying with Notepad as fallback");
            startInfo = new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = configPath,
                UseShellExecute = true
            };
            process = Process.Start(startInfo);
            if (process is null)
            {
                Logger.Error<XeniaSettingsPageViewModel>("Failed to open with Notepad");
                throw new Exception("Failed to open the configuration file with default app.");
            }

            Logger.Info<XeniaSettingsPageViewModel>("Configuration file opened successfully with Notepad");
        }
        catch (Exception ex)
        {
            Logger.Error<XeniaSettingsPageViewModel>($"Failed to open the configuration file");
            Logger.LogExceptionDetails<XeniaSettingsPageViewModel>(ex);

            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("XeniaSettingsPage.OpenInEditor.Failure.Title"),
                string.Format(LocalizationHelper.GetText("XeniaSettingsPage.OpenInEditor.Failure.Message"),
                    SelectedConfigFile.DisplayName, ex.Message));
        }
    }

    /// <summary>
    /// Saves the current configuration file.
    /// </summary>
    [RelayCommand]
    private async Task SaveAsync()
    {
        if (ConfigEditorViewModel == null || string.IsNullOrEmpty(CurrentConfigFilePath))
        {
            return;
        }

        bool success = await ConfigEditorViewModel.SaveAsync();

        if (success)
        {
            await _messageBoxService.ShowInfoAsync(
                LocalizationHelper.GetText("XeniaSettingsPage.SaveSettings.Success.Title"),
                LocalizationHelper.GetText("XeniaSettingsPage.SaveSettings.Success.Message"));
        }
    }
}