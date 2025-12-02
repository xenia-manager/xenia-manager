using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
// Imported Librarires
using Tomlyn;
using Tomlyn.Model;
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Constants.Emulators;
using XeniaManager.Core.Enum;
using XeniaManager.Core.Game;
using XeniaManager.Core.Installation;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using XeniaManager.Desktop.ViewModel.Windows;
using Button = System.Windows.Controls.Button;
using TextBox = System.Windows.Controls.TextBox;

namespace XeniaManager.Desktop.Views.Windows;

public partial class GameSettingsEditor : FluentWindow
{
    #region Variables

    private readonly GameSettingsEditorViewModel _viewModel;
    private Game _currentGame { get; set; }
    private TomlTable _currentConfigurationFile { get; set; }
    private Dictionary<string, Action<TomlTable>> _settingLoaders { get; set; }
    private Dictionary<string, Action<TomlTable>> _settingSavers { get; set; }
    private Type? _readbackResolveOriginalType;

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
            try
            {
                TbTitleIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/64.png", UriKind.Absolute));
            }
            catch (Exception)
            {
                TbTitleIcon.Source = null;
            }
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
            //throw new IOException("Configuration file not found. Please create a new one from the default one.");
            XeniaVersion xeniaVersion = _currentGame.XeniaVersion;
            switch (xeniaVersion)
            {
                case XeniaVersion.Canary:
                    Logger.Info($"Using default configuration file from Xenia {xeniaVersion}");
                    configurationLocation = Path.Combine(DirectoryPaths.Base, XeniaCanary.ConfigLocation);
                    if (!File.Exists(configurationLocation))
                    {
                        Logger.Warning($"Generating default configuration file for Xenia {xeniaVersion} since it's missing");
                        Xenia.GenerateConfigFile(Path.Combine(DirectoryPaths.Base, XeniaCanary.ExecutableLocation), Path.Combine(DirectoryPaths.Base, XeniaCanary.DefaultConfigLocation));
                        File.Move(Path.Combine(DirectoryPaths.Base, XeniaCanary.DefaultConfigLocation), Path.Combine(DirectoryPaths.Base, XeniaCanary.ConfigLocation));
                    }
                    break;
                case XeniaVersion.Mousehook:
                    Logger.Info($"Using default configuration file from Xenia {xeniaVersion}");
                    configurationLocation = Path.Combine(DirectoryPaths.Base, XeniaMousehook.ConfigLocation);
                    if (!File.Exists(configurationLocation))
                    {
                        Logger.Warning($"Generating default configuration file for Xenia {xeniaVersion} since it's missing");
                        Xenia.GenerateConfigFile(Path.Combine(DirectoryPaths.Base, XeniaMousehook.ExecutableLocation), Path.Combine(DirectoryPaths.Base, XeniaMousehook.DefaultConfigLocation));
                        File.Move(Path.Combine(DirectoryPaths.Base, XeniaMousehook.DefaultConfigLocation), Path.Combine(DirectoryPaths.Base, XeniaMousehook.ConfigLocation));
                    }
                    break;
                case XeniaVersion.Netplay:
                    Logger.Info($"Using default configuration file from Xenia {xeniaVersion}");
                    configurationLocation = Path.Combine(DirectoryPaths.Base, XeniaNetplay.ConfigLocation);
                    if (!File.Exists(configurationLocation))
                    {
                        Logger.Warning($"Generating default configuration file for Xenia {xeniaVersion} since it's missing");
                        Xenia.GenerateConfigFile(Path.Combine(DirectoryPaths.Base, XeniaNetplay.ExecutableLocation), Path.Combine(DirectoryPaths.Base, XeniaNetplay.DefaultConfigLocation));
                        File.Move(Path.Combine(DirectoryPaths.Base, XeniaNetplay.DefaultConfigLocation), Path.Combine(DirectoryPaths.Base, XeniaNetplay.ConfigLocation));
                    }
                    break;
                default:
                    throw new NotImplementedException($"Xenia {xeniaVersion} is not supported");
            }
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
            CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Error"), LocalizationHelper.GetUiText("MessageBox_OverAllowedTextLimit"));
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

    // General section
    private void BtnChangeNotificationSoundPath_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog fileDialog = new OpenFileDialog
        {
            Title = "Select Notification Sound file",
            Multiselect = false,
            Filter = "WAV files (*.wav)|*.wav"
        };

        bool? result = fileDialog.ShowDialog();

        if (result == true)
        {
            Logger.Debug($"Selected file: {fileDialog.FileName}");
            BtnChangeNotificationSoundPath.ToolTip = fileDialog.FileName;
        }
        else if (BtnChangeNotificationSoundPath.ToolTip.ToString() != string.Empty)
        {
            Wpf.Ui.Controls.MessageBoxResult resetPath = CustomMessageBox.YesNo(LocalizationHelper.GetUiText("MessageBox_Reset"), LocalizationHelper.GetUiText("MessageBox_ResetNotificationSoundPathText"));
            if (resetPath == Wpf.Ui.Controls.MessageBoxResult.Primary)
            {
                Logger.Debug("Resetting notification sound path");
                BtnChangeNotificationSoundPath.ToolTip = string.Empty;
            }
        }
    }

    #endregion
}