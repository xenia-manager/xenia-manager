using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported Libraries
using NvAPIWrapper.DRS;
using Tomlyn;
using Tomlyn.Model;
using XeniaManager.Core;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Constants.Emulators;
using XeniaManager.Core.Game;
using XeniaManager.Core.GPU.NVIDIA;
using XeniaManager.Core.Installation;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using XeniaManager.Desktop.ViewModel.Pages;

namespace XeniaManager.Desktop.Views.Pages;

/// <summary>
/// Interaction logic for XeniaSettingsPage.xaml
/// </summary>
public partial class XeniaSettingsPage : Page
{
    #region Variables

    private readonly XeniaSettingsViewModel _viewModel;
    private Game _selectedGame { get; set; }
    private TomlTable _currentConfigurationFile { get; set; }
    private Dictionary<string, Action<TomlTable>> _settingLoaders { get; set; }
    private Dictionary<string, Action<TomlTable>> _settingSavers { get; set; }

    #endregion

    #region Constructors

    public XeniaSettingsPage()
    {
        InitializeComponent();
        _viewModel = new XeniaSettingsViewModel();
        this.DataContext = _viewModel;
        _settingLoaders = new Dictionary<string, Action<TomlTable>>
        {
            { "APU", LoadAudioSettings },
            { "CPU", LoadCpuSettings },
            { "Content", LoadContentSettings },
            { "D3D12", LoadD3D12Settings },
            { "Display", LoadDisplaySettings },
            { "General", LoadGeneralSettings },
            { "GPU", LoadGpuSettings },
            { "HID", LoadHidSettings },
            { "Kernel", LoadKernelSettings },
            { "Live", LoadLiveSettings },
            { "Memory", LoadMemorySettings },
            { "MouseHook", LoadMousehookSettings },
            { "Video", LoadVideoSettings },
            { "Storage", LoadStorageSettings },
            { "UI", LoadUiSettings },
            { "Vulkan", LoadVulkanSettings },
            { "XConfig", LoadXConfigSettings }
        };
        _settingSavers = new Dictionary<string, Action<TomlTable>>
        {
            { "APU", SaveAudioSettings },
            { "CPU", SaveCpuSettings },
            { "Content", SaveContentSettings },
            { "D3D12", SaveD3D12Settings },
            { "Display", SaveDisplaySettings },
            { "General", SaveGeneralSettings },
            { "GPU", SaveGpuSettings },
            { "HID", SaveHidSettings },
            { "Kernel", SaveKernelSettings },
            { "Live", SaveLiveSettings },
            { "Memory", SaveMemorySettings },
            { "MouseHook", SaveMousehookSettings },
            { "Video", SaveVideoSettings },
            { "Storage", SaveStorageSettings },
            { "UI", SaveUiSettings },
            { "Vulkan", SaveVulkanSettings },
            { "XConfig", SaveXConfigSettings }
        };
        ShowOnlyPanel(SpAudioSettings);
    }

    #endregion

    #region Functions & Events

    private void XeniaSettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        Mouse.OverrideCursor = Cursors.Wait;
        CmbConfigurationFiles.SelectedIndex = 0;
        ReadNVIDIAProfile();
        Mouse.OverrideCursor = null;
    }

    private void ReadNVIDIAProfile()
    {
        try
        {
            if (!NVAPI.Initialize())
            {
                Logger.Error("Failed to initialize NVAPI");
                return;
            }

            NVAPI.FindAppProfile();

            ProfileSetting nvidiaVSync = NVAPI.GetSetting(NVAPI_SETTINGS.VSYNC_MODE);
            if (nvidiaVSync != null)
            {
                Logger.Info($"NVIDIA Vertical Sync - {(NVAPI_VSYNC_MODE)nvidiaVSync.CurrentValue}");
                CmbNvidiaVerticalSync.SelectedValue = (NVAPI_VSYNC_MODE)nvidiaVSync.CurrentValue;
            }
            else
            {
                Logger.Warning($"Couldn't find NVIDIA Vertical Sync setting");
                CmbNvidiaVerticalSync.SelectedValue = NVAPI_VSYNC_MODE.DEFAULT;
            }

            ProfileSetting framerateLimiter = NVAPI.GetSetting(NVAPI_SETTINGS.FRAMERATE_LIMITER);
            if (framerateLimiter != null)
            {
                Logger.Info($"NVIDIA Framerate Limiter - {framerateLimiter.CurrentValue}");
                SldNvidiaFramerate.Value = Convert.ToDouble(framerateLimiter.CurrentValue);
            }
            else
            {
                Logger.Info($"Couldn't find NVIDIA Framerate Limiter setting");
                SldNvidiaFramerate.Value = 0;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.ShowAsync(ex);
        }
    }

    private void LoadConfiguration(string configurationLocation, bool readFile = true)
    {
        _viewModel.ResetUniqueSettingsVisibility();
        if (!File.Exists(configurationLocation))
        {
            Logger.Warning("Configuration file not found");
            // TODO: Create new one from the default
            throw new IOException("Configuration file not found. Please create a new one from the default one.");
        }

        if (readFile)
        {
            _currentConfigurationFile = Toml.Parse(File.ReadAllText(configurationLocation)).ToModel();
        }

        foreach (KeyValuePair<string, object> section in _currentConfigurationFile)
        {
            if (section.Value is TomlTable sectionTable && _settingLoaders.TryGetValue(section.Key, out Action<TomlTable> loader))
            {
                Logger.Info($"Section: {section.Key}");
                loader(sectionTable);
            }
            else
            {
                Logger.Warning($"Unknown section '{section.Key}' in the configuration file");
            }
        }

        if (_viewModel.MousehookSettingsVisibility != Visibility.Visible && SpMousehookSettings.Visibility == Visibility.Visible)
        {
            ShowOnlyPanel(SpAudioSettings);
        }
    }

    private void CmbConfigurationFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbConfigurationFiles.SelectedIndex < 0)
        {
            Logger.Debug("Nothing configuration file is selected");
            return;
        }

        try
        {
            switch (CmbConfigurationFiles.SelectedItem)
            {
                case "Default Xenia Canary":
                    Logger.Info($"Loading default configuration file for Xenia Canary");
                    LoadConfiguration(Path.Combine(DirectoryPaths.Base, XeniaCanary.ConfigLocation));
                    MniOptmizeSettings.Visibility = Visibility.Collapsed;
                    break;
                case "Default Xenia Mousehook":
                    Logger.Info($"Loading default configuration file for Xenia Mousehook");
                    LoadConfiguration(Path.Combine(DirectoryPaths.Base, XeniaMousehook.ConfigLocation));
                    MniOptmizeSettings.Visibility = Visibility.Collapsed;
                    break;
                case "Default Xenia Netplay":
                    Logger.Info($"Loading default configuration file for Xenia Netplay");
                    LoadConfiguration(Path.Combine(DirectoryPaths.Base, XeniaNetplay.ConfigLocation));
                    MniOptmizeSettings.Visibility = Visibility.Collapsed;
                    break;
                default:
                    _selectedGame = GameManager.Games.FirstOrDefault(game => game.Title == CmbConfigurationFiles.SelectedItem);
                    Logger.Info($"Loading configuration file for {_selectedGame.Title}");
                    if (_selectedGame != null)
                    {
                        LoadConfiguration(Path.Combine(DirectoryPaths.Base, _selectedGame.FileLocations.Config));
                        MniOptmizeSettings.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        throw new Exception("Game not found");
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.ShowAsync(ex);
        }
    }

    private void BtnResetSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            switch (CmbConfigurationFiles.SelectedItem)
            {
                case "Default Xenia Canary":
                    Logger.Info($"Resetting default configuration file for Xenia Canary");
                    if (File.Exists(Path.Combine(DirectoryPaths.Base, XeniaCanary.ConfigLocation)))
                    {
                        File.Delete(Path.Combine(DirectoryPaths.Base, XeniaCanary.ConfigLocation));
                    }

                    if (File.Exists(Path.Combine(DirectoryPaths.Base, XeniaCanary.DefaultConfigLocation)))
                    {
                        File.Delete(Path.Combine(DirectoryPaths.Base, XeniaCanary.DefaultConfigLocation));
                    }

                    Xenia.GenerateConfigFile(Path.Combine(DirectoryPaths.Base, XeniaCanary.ExecutableLocation),
                        Path.Combine(DirectoryPaths.Base, XeniaCanary.DefaultConfigLocation));

                    if (File.Exists(Path.Combine(DirectoryPaths.Base, XeniaCanary.DefaultConfigLocation)))
                    {
                        File.Copy(Path.Combine(DirectoryPaths.Base, XeniaCanary.DefaultConfigLocation),
                            Path.Combine(DirectoryPaths.Base, XeniaCanary.ConfigLocation), true);
                    }

                    CmbConfigurationFiles_SelectionChanged(CmbConfigurationFiles, null);
                    CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_XeniaSettingsResetText"), XeniaVersion.Canary));
                    break;
                case "Default Xenia Mousehook":
                    Logger.Info($"Resetting default configuration file for Xenia Mousehook");
                    if (File.Exists(Path.Combine(DirectoryPaths.Base, XeniaMousehook.ConfigLocation)))
                    {
                        File.Delete(Path.Combine(DirectoryPaths.Base, XeniaMousehook.ConfigLocation));
                    }

                    if (File.Exists(Path.Combine(DirectoryPaths.Base, XeniaMousehook.DefaultConfigLocation)))
                    {
                        File.Delete(Path.Combine(DirectoryPaths.Base, XeniaMousehook.DefaultConfigLocation));
                    }

                    Xenia.GenerateConfigFile(Path.Combine(DirectoryPaths.Base, XeniaMousehook.ExecutableLocation),
                        Path.Combine(DirectoryPaths.Base, XeniaMousehook.DefaultConfigLocation));

                    if (File.Exists(Path.Combine(DirectoryPaths.Base, XeniaMousehook.DefaultConfigLocation)))
                    {
                        File.Copy(Path.Combine(DirectoryPaths.Base, XeniaMousehook.DefaultConfigLocation),
                            Path.Combine(DirectoryPaths.Base, XeniaMousehook.ConfigLocation), true);
                    }

                    CmbConfigurationFiles_SelectionChanged(CmbConfigurationFiles, null);
                    CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_XeniaSettingsResetText"), XeniaVersion.Mousehook));
                    break;
                case "Default Xenia Netplay":
                    Logger.Info($"Resetting default configuration file for Xenia Netplay");
                    if (File.Exists(Path.Combine(DirectoryPaths.Base, XeniaNetplay.ConfigLocation)))
                    {
                        File.Delete(Path.Combine(DirectoryPaths.Base, XeniaNetplay.ConfigLocation));
                    }

                    if (File.Exists(Path.Combine(DirectoryPaths.Base, XeniaNetplay.DefaultConfigLocation)))
                    {
                        File.Delete(Path.Combine(DirectoryPaths.Base, XeniaNetplay.DefaultConfigLocation));
                    }

                    Xenia.GenerateConfigFile(Path.Combine(DirectoryPaths.Base, XeniaNetplay.ExecutableLocation),
                        Path.Combine(DirectoryPaths.Base, XeniaNetplay.DefaultConfigLocation));

                    if (File.Exists(Path.Combine(DirectoryPaths.Base, XeniaNetplay.DefaultConfigLocation)))
                    {
                        File.Copy(Path.Combine(DirectoryPaths.Base, XeniaNetplay.DefaultConfigLocation),
                            Path.Combine(DirectoryPaths.Base, XeniaNetplay.ConfigLocation), true);
                    }

                    CmbConfigurationFiles_SelectionChanged(CmbConfigurationFiles, null);
                    CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_XeniaSettingsResetText"), XeniaVersion.Netplay));
                    break;
                default:
                    _selectedGame = GameManager.Games.FirstOrDefault(game => game.Title == CmbConfigurationFiles.SelectedItem);
                    Logger.Info($"Loading configuration file for {_selectedGame.Title}");
                    if (_selectedGame != null)
                    {
                        switch (_selectedGame.XeniaVersion)
                        {
                            case XeniaVersion.Canary:
                                Logger.Info($"Resetting default configuration file for {_selectedGame.Title}");
                                LoadConfiguration(Path.Combine(DirectoryPaths.Base, XeniaCanary.ConfigLocation));
                                break;
                            case XeniaVersion.Mousehook:
                                Logger.Info($"Resetting default configuration file for {_selectedGame.Title}");
                                LoadConfiguration(Path.Combine(DirectoryPaths.Base, XeniaMousehook.ConfigLocation));
                                break;
                            case XeniaVersion.Netplay:
                                Logger.Info($"Resetting default configuration file for {_selectedGame.Title}");
                                LoadConfiguration(Path.Combine(DirectoryPaths.Base, XeniaNetplay.ConfigLocation));
                                break;
                        }
                    }
                    else
                    {
                        throw new Exception("Game not found");
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.ShowAsync(ex);
        }
    }

    private async void BtnOptimizeSettings_OnClick(object sender, RoutedEventArgs e)
    {
        _selectedGame = GameManager.Games.FirstOrDefault(game => game.Title == CmbConfigurationFiles.SelectedItem);
        if (_selectedGame == null)
        {
            CustomMessageBox.ShowAsync("Error", "You didn't select a game");
            return;
        }
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            Logger.Info($"Trying to find optimized settings for {_selectedGame.Title}");
            JsonElement? optimizedSettings = await ConfigManager.SearchForOptimizedSettings(_selectedGame);
            if (optimizedSettings == null)
            {
                CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Error"), string.Format(LocalizationHelper.GetUiText("MessageBox_MissingOptimizedSettingsText"), _selectedGame.Title));
                return;
            }
            Logger.Debug($"Optimized Settings\n:{JsonSerializer.Serialize(optimizedSettings.Value, new JsonSerializerOptions
            {
                WriteIndented = true
            })}");
            Logger.Info("Applying optimized settings");
            string changedSettings = ConfigManager.OptimizeSettings(_currentConfigurationFile, (JsonElement)optimizedSettings);
            Logger.Info("Reloading the UI to apply the changes");
            LoadConfiguration(_selectedGame.FileLocations.Config, false);
            Mouse.OverrideCursor = null;
            CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessOptimizedSettingsText"), changedSettings));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            Mouse.OverrideCursor = null;
            CustomMessageBox.ShowAsync(ex);
        }
    }

    private void BtnOpenInEditor_OnClick(object sender, RoutedEventArgs e)
    {
        string configPath = string.Empty;
        ProcessStartInfo startInfo = new ProcessStartInfo();
        Process process;
        try
        {
            switch (CmbConfigurationFiles.SelectedItem)
            {
                case "Default Xenia Canary":
                    Logger.Info($"Opening default configuration file for Xenia Canary");
                    configPath = XeniaCanary.ConfigLocation;
                    break;
                case "Default Xenia Mousehook":
                    Logger.Info($"Opening default configuration file for Xenia Canary");
                    configPath = XeniaMousehook.ConfigLocation;
                    break;
                case "Default Xenia Netplay":
                    Logger.Info($"Opening default configuration file for Xenia Canary");
                    configPath = XeniaNetplay.ConfigLocation;
                    break;
                default:
                    _selectedGame = GameManager.Games.FirstOrDefault(game => game.Title == CmbConfigurationFiles.SelectedItem);
                    if (_selectedGame == null)
                    {
                        CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Error"), LocalizationHelper.GetUiText("MessageBox_GameNotSelectedErrorText"));
                        return;
                    }
                    configPath = Path.Combine(DirectoryPaths.Base, _selectedGame.FileLocations.Config);
                    break;
            }

            startInfo = new ProcessStartInfo
            {
                FileName = configPath,
                UseShellExecute = true
            };

            process = Process.Start(startInfo);
            if (process == null)
            {
                throw new Exception("Failed to open the configuration file with default app.");
            }
        }
        catch (Win32Exception)
        {
            Logger.Warning("Default application not found");
            Logger.Info("Trying to open the file with notepad");
            startInfo = new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = configPath,
                UseShellExecute = true
            };
            process = Process.Start(startInfo);
            if (process == null)
            {
                throw new Exception("Failed to open the configuration file with notepad.");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.ShowAsync(ex);
            return;
        }
    }

    private void SaveNVIDIASettings()
    {
        if (SpNvidiaSettings.Visibility != Visibility.Visible)
        {
            return;
        }

        // Vertical Sync
        NVAPI.SetSettingValue(NVAPI_SETTINGS.VSYNC_MODE, (uint)CmbNvidiaVerticalSync.SelectedValue);

        // Framerate Limiter
        NVAPI.SetSettingValue(NVAPI_SETTINGS.FRAMERATE_LIMITER, (uint)SldNvidiaFramerate.Value);
    }

    private void SaveConfiguration(string configurationLocation)
    {
        if (!File.Exists(configurationLocation))
        {
            Logger.Warning("Configuration file not found");
            // TODO: Create new one from the default
            throw new IOException("Configuration file not found. Please create a new one from the default one.");
        }

        foreach (KeyValuePair<string, object> section in _currentConfigurationFile)
        {
            if (section.Value is TomlTable sectionTable && _settingSavers.TryGetValue(section.Key, out Action<TomlTable> saver))
            {
                Logger.Info($"Section: {section.Key}");
                saver(sectionTable);
            }
            else
            {
                Logger.Warning($"Unknown section '{section.Key}' in the configuration file");
            }
        }

        File.WriteAllText(configurationLocation, Toml.FromModel(_currentConfigurationFile));
        Logger.Info("Changes have been saved");
        CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), LocalizationHelper.GetUiText("MessageBox_SuccessSaveChangesText"));
    }

    private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Logger.Info("Saving changes");
            switch (CmbConfigurationFiles.SelectedItem)
            {
                case "Default Xenia Canary":
                    Logger.Info($"Loading default configuration file for Xenia Canary");
                    SaveConfiguration(Path.Combine(DirectoryPaths.Base, XeniaCanary.ConfigLocation));
                    break;
                case "Default Xenia Mousehook":
                    Logger.Info($"Loading default configuration file for Xenia Mousehook");
                    SaveConfiguration(Path.Combine(DirectoryPaths.Base, XeniaMousehook.ConfigLocation));
                    break;
                case "Default Xenia Netplay":
                    Logger.Info($"Loading default configuration file for Xenia Netplay");
                    SaveConfiguration(Path.Combine(DirectoryPaths.Base, XeniaNetplay.ConfigLocation));
                    break;
                default:
                    _selectedGame = GameManager.Games.FirstOrDefault(game => game.Title == CmbConfigurationFiles.SelectedItem);
                    Logger.Info($"Loading configuration file for {_selectedGame.Title}");
                    if (_selectedGame != null)
                    {
                        SaveConfiguration(Path.Combine(DirectoryPaths.Base, _selectedGame.FileLocations.Config));
                    }
                    else
                    {
                        throw new Exception("Game not found");
                    }
                    break;
            }
            SaveNVIDIASettings();
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.ShowAsync(ex);
        }
    }

    private void ShowOnlyPanel(StackPanel settings)
    {
        foreach (object? child in SpSettingsPanel.Children)
        {
            if (child is UIElement el)
            {
                el.Visibility = Visibility.Collapsed;
            }
        }

        settings.Visibility = Visibility.Visible;
    }

    private void BtnShowSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!(sender is Button btn))
            {
                return;
            }
            ;

            StackPanel settingsPanel = btn.Name switch
            {
                "BtnAudioSettings" => SpAudioSettings,
                "BtnDisplaySettings" => SpDisplaySettings,
                "BtnGraphicalSettings" => SpGraphicalSettings,
                "BtnGeneralSettings" => SpGeneralSettings,
                "BtnUserInputSettings" => SpUserInputSettings,
                "BtnStorageSettings" => SpStorageSettings,
                "BtnHackSettings" => SpHackSettings,
                "BtnMousehookSettings" => SpMousehookSettings,
                "BtnNetplaySettings" => SpNetplaySettings,
                _ => throw new NotImplementedException("Missing implementation for this button.")
            };

            ShowOnlyPanel(settingsPanel);
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.ShowAsync(ex);
        }
    }

    // Audio section
    private void TxtAudioMaxQueuedFrames_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        if (textBox.Text.Length > 10)
        {
            Logger.Error("User went over the allowed limit of characters for 'Audio Max Queued Frames' field");
            textBox.Text = "8";
            CustomMessageBox.ShowAsync("Error", "You went over the allowed limit of characters for this field.");
        }
    }

    private void ChkXmp_Click(object sender, RoutedEventArgs e)
    {
        if (ChkXmp.IsChecked == true)
        {
            Logger.Debug("XMP is enabled, making volume slider visible");
            BrdXmpVolumeSetting.Visibility = Visibility.Visible;
        }
        else
        {
            Logger.Debug("XMP is disabled, making volume slider collapsed");
            BrdXmpVolumeSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SldXmpVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (sender is not Slider xmpSlider)
        {
            return;
        }

        TxtSldXmpVolume.Text = $"{xmpSlider.Value}%";
    }

    // Display section
    private void CmbInternalDisplayResolution_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbInternalDisplayResolution.SelectedItem == "Custom")
        {
            BrdCustomInternalDisplayResolutionWidthSetting.Visibility = Visibility.Visible;
            BrdCustomInternalDisplayResolutionHeightSetting.Visibility = Visibility.Visible;
        }
        else
        {
            BrdCustomInternalDisplayResolutionWidthSetting.Visibility = Visibility.Collapsed;
            BrdCustomInternalDisplayResolutionHeightSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SldNvidiaFramerate_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Slider slider = sender as Slider;

        if (slider == null)
        {
            return;
        }

        if (slider.Value is > 0 and < 20)
        {
            slider.Value = slider.Value < 10 ? 0 : 20;
        }
    }

    // Graphics section
    private void CmbGpuApi_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        switch (CmbGpuApi.SelectedItem.ToString().ToLower())
        {
            case "d3d12":
                SpD3D12Settings.Visibility = Visibility.Visible;
                SpVulkanSettings.Visibility = Visibility.Collapsed;
                break;
            case "vulkan":
                SpD3D12Settings.Visibility = Visibility.Collapsed;
                SpVulkanSettings.Visibility = Visibility.Visible;
                break;
            default:
                SpD3D12Settings.Visibility = Visibility.Visible;
                SpVulkanSettings.Visibility = Visibility.Visible;
                break;
        }
    }

    #endregion
}