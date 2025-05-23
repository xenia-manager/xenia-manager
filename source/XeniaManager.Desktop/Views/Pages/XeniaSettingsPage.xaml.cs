using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tomlyn;
using Tomlyn.Model;
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Core.Installation;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.ViewModel.Pages;

namespace XeniaManager.Desktop.Views.Pages;

/// <summary>
/// Interaction logic for XeniaSettingsPage.xaml
/// </summary>
public partial class XeniaSettingsPage : Page
{
    // TODO: Xenia Settings Page

    // Variables
    private readonly XeniaSettingsViewModel _viewModel;
    private Game _selectedGame { get; set; }
    private TomlTable _currentConfigurationFile { get; set; }
    private Dictionary<string, Action<TomlTable>> _settingLoaders { get; set; }
    private Dictionary<string, Action<TomlTable>> _settingSavers { get; set; }

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

    private void XeniaSettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        Mouse.OverrideCursor = Cursors.Wait;
        CmbConfigurationFiles.SelectedIndex = 0;
        Mouse.OverrideCursor = null;
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
                    break;
                case "Default Xenia Mousehook":
                    Logger.Info($"Loading default configuration file for Xenia Mousehook");
                    break;
                case "Default Xenia Netplay":
                    Logger.Info($"Loading default configuration file for Xenia Netplay");
                    break;
                default:
                    _selectedGame = GameManager.Games.FirstOrDefault(game => game.Title == CmbConfigurationFiles.SelectedItem);
                    Logger.Info($"Loading configuration file for {_selectedGame.Title}");
                    if (_selectedGame != null)
                    {
                        LoadConfiguration(Path.Combine(Constants.DirectoryPaths.Base, _selectedGame.FileLocations.Config));
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
            Logger.Error(ex);
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
                    throw new NotImplementedException("Resetting Xenia Mousehook configuration is not implemented.");
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
                                throw new NotImplementedException("Resetting Xenia Mousehook configuration is not implemented.");
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
            Logger.Error(ex.Message + "\nFull Error:\n" + ex);
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
            Logger.Error(ex.Message + "\nFull Error:\n" + ex);
            Mouse.OverrideCursor = null;
            CustomMessageBox.Show(ex);
        }
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
                    SaveConfiguration(Constants.Xenia.Canary.ConfigLocation);
                    break;
                case "Default Xenia Mousehook":
                    Logger.Info($"Loading default configuration file for Xenia Mousehook");
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
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
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
            Logger.Error(ex);
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
}