using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;
using System.Windows.Input;
using System.Windows.Media.Imaging;

// Imported Librarires
using Tomlyn;
using Tomlyn.Model;
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.ViewModel.Windows;
using XeniaManager.Core.Constants;

namespace XeniaManager.Desktop.Views.Windows;

public partial class GameSettingsEditor : FluentWindow
{
    #region Variables

    private readonly GameSettingsEditorViewModel _viewModel;
    private Game _currentGame { get; set; }
    private TomlTable _currentConfigurationFile { get; set; }
    private Dictionary<string, Action<TomlTable>> _settingLoaders { get; set; }
    private Dictionary<string, Action<TomlTable>> _settingSavers { get; set; }

    #endregion

    #region Constructors

    public GameSettingsEditor(Game game)
    {
        InitializeComponent();
        _viewModel = new GameSettingsEditorViewModel();
        this.DataContext = _viewModel;
        this._currentGame = game;
        TbTitle.Title = $"{_currentGame.Title} Settings";
        try
        {
            TbTitleIcon.Source = ArtworkManager.CacheLoadArtwork(Path.Combine(DirectoryPaths.Base, _currentGame.Artwork.Icon));
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            TbTitleIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/1024.png", UriKind.Absolute));
        }
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

    private void GameSettingsEditor_Loaded(object sender, RoutedEventArgs e)
    {
        Mouse.OverrideCursor = Cursors.Wait;
        LoadConfiguration(Path.Combine(DirectoryPaths.Base, _currentGame.FileLocations.Config));
        Mouse.OverrideCursor = null;
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
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        Mouse.OverrideCursor = Cursors.Wait;
        SaveConfiguration(Path.Combine(DirectoryPaths.Base, _currentGame.FileLocations.Config));
        Mouse.OverrideCursor = null;
        base.OnClosing(e);
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