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
using XeniaManager.Core.Game;
using XeniaManager.Core.GPU.NVIDIA;
using XeniaManager.Core.Installation;
using XeniaManager.Desktop.Components;
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
            { "Memory", LoadMemorySettings },
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
            { "Memory", SaveMemorySettings },
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
            CustomMessageBox.Show(ex);
        }
    }

    private void LoadConfiguration(string configurationLocation, bool readFile = true)
    {
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
                    LoadConfiguration(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ConfigLocation));
                    MniOptmizeSettings.Visibility = Visibility.Collapsed;
                    break;
                case "Default Xenia Mousehook":
                    Logger.Info($"Loading default configuration file for Xenia Mousehook");
                    LoadConfiguration(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.ConfigLocation));
                    MniOptmizeSettings.Visibility = Visibility.Collapsed;
                    break;
                case "Default Xenia Netplay":
                    Logger.Info($"Loading default configuration file for Xenia Netplay");
                    MniOptmizeSettings.Visibility = Visibility.Collapsed;
                    break;
                default:
                    _selectedGame = GameManager.Games.FirstOrDefault(game => game.Title == CmbConfigurationFiles.SelectedItem);
                    Logger.Info($"Loading configuration file for {_selectedGame.Title}");
                    if (_selectedGame != null)
                    {
                        LoadConfiguration(Path.Combine(Constants.DirectoryPaths.Base, _selectedGame.FileLocations.Config));
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
            CustomMessageBox.Show(ex);
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
                    if (File.Exists(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ConfigLocation)))
                    {
                        File.Delete(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ConfigLocation));
                    }

                    if (File.Exists(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.DefaultConfigLocation)))
                    {
                        File.Delete(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.DefaultConfigLocation));
                    }

                    Xenia.GenerateConfigFile(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ExecutableLocation),
                        Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.DefaultConfigLocation));

                    if (File.Exists(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.DefaultConfigLocation)))
                    {
                        File.Copy(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.DefaultConfigLocation),
                            Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ConfigLocation), true);
                    }

                    CmbConfigurationFiles_SelectionChanged(CmbConfigurationFiles, null);
                    CustomMessageBox.Show("Successful settings reset", "Default Xenia Canary settings have been reset.");
                    break;
                case "Default Xenia Mousehook":
                    Logger.Info($"Resetting default configuration file for Xenia Mousehook");
                    if (File.Exists(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.ConfigLocation)))
                    {
                        File.Delete(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.ConfigLocation));
                    }

                    if (File.Exists(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.DefaultConfigLocation)))
                    {
                        File.Delete(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.DefaultConfigLocation));
                    }

                    Xenia.GenerateConfigFile(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.ExecutableLocation),
                        Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.DefaultConfigLocation));

                    if (File.Exists(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.DefaultConfigLocation)))
                    {
                        File.Copy(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.DefaultConfigLocation),
                            Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.ConfigLocation), true);
                    }

                    CmbConfigurationFiles_SelectionChanged(CmbConfigurationFiles, null);
                    CustomMessageBox.Show("Successful settings reset", "Default Xenia Mousehook settings have been reset.");
                    break;
                case "Default Xenia Netplay":
                    throw new NotImplementedException("Resetting Xenia Netplay configuration is not implemented.");
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
                                LoadConfiguration(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ConfigLocation));
                                break;
                            case XeniaVersion.Mousehook:
                                Logger.Info($"Resetting default configuration file for {_selectedGame.Title}");
                                LoadConfiguration(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.ConfigLocation));
                                break;
                            case XeniaVersion.Netplay:
                                throw new NotImplementedException("Resetting Xenia Netplay configuration is not implemented.");
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
            CustomMessageBox.Show(ex);
        }
    }

    private async void BtnOptimizeSettings_OnClick(object sender, RoutedEventArgs e)
    {
        _selectedGame = GameManager.Games.FirstOrDefault(game => game.Title == CmbConfigurationFiles.SelectedItem);
        if (_selectedGame == null)
        {
            CustomMessageBox.Show("Error", "You didn't select a game");
            return;
        }
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            Logger.Info($"Trying to find optimized settings for {_selectedGame.Title}");
            JsonElement? optimizedSettings = await ConfigManager.SearchForOptimizedSettings(_selectedGame);
            if (optimizedSettings == null)
            {
                CustomMessageBox.Show("No optimized settings found", "This game has no optimized settings.");
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
            CustomMessageBox.Show("Success", $"Optimized Settings:\n\n{changedSettings}\n have been successfully loaded.\n To apply them, please click the 'Save Changes' button.");
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            Mouse.OverrideCursor = null;
            CustomMessageBox.Show(ex);
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
                    configPath = Constants.Xenia.Canary.ConfigLocation;
                    break;
                case "Default Xenia Mousehook":
                    throw new NotImplementedException("Opening Xenia Mousehook configuration is not implemented.");
                    break;
                case "Default Xenia Netplay":
                    throw new NotImplementedException("Opening Xenia Netplay configuration is not implemented.");
                default:
                    _selectedGame = GameManager.Games.FirstOrDefault(game => game.Title == CmbConfigurationFiles.SelectedItem);
                    if (_selectedGame == null)
                    {
                        CustomMessageBox.Show("Error", "You didn't select a game");
                        return;
                    }
                    configPath = Path.Combine(Constants.DirectoryPaths.Base, _selectedGame.FileLocations.Config);
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
            CustomMessageBox.Show(ex);
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
        CustomMessageBox.Show("Success", "Changes to the configuration files have been saved.");
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
                    SaveConfiguration(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ConfigLocation));
                    break;
                case "Default Xenia Mousehook":
                    Logger.Info($"Loading default configuration file for Xenia Mousehook");
                    SaveConfiguration(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.ConfigLocation));
                    break;
                case "Default Xenia Netplay":
                    Logger.Info($"Loading default configuration file for Xenia Netplay");
                    break;
                default:
                    _selectedGame = GameManager.Games.FirstOrDefault(game => game.Title == CmbConfigurationFiles.SelectedItem);
                    Logger.Info($"Loading configuration file for {_selectedGame.Title}");
                    if (_selectedGame != null)
                    {
                        SaveConfiguration(Path.Combine(Constants.DirectoryPaths.Base, _selectedGame.FileLocations.Config));
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
            CustomMessageBox.Show(ex);
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
                _ => throw new NotImplementedException("Missing implementation for this button.")
            };

            ShowOnlyPanel(settingsPanel);
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.Show(ex);
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
            CustomMessageBox.Show("Error", "You went over the allowed limit of characters for this field.");
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