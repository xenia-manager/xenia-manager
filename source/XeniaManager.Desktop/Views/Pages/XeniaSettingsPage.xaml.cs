using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tomlyn;
using Tomlyn.Model;
using XeniaManager.Core;
using XeniaManager.Core.Game;
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
            { "APU", LoadAudioSettings }
        };
        _settingSavers = new Dictionary<string, Action<TomlTable>>
        {
            { "APU", SaveAudioSettings }
        };
        ShowOnlyPanel(SpAudioSettings);
    }

    private void XeniaSettingsPage_OnLoaded(object sender, RoutedEventArgs e)
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

    private void CmbConfigurationFiles_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
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
                    LoadConfiguration(Constants.Xenia.Canary.ConfigLocation);
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
    
    private void BtnSaveSettings_OnClick(object sender, RoutedEventArgs e)
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
    private void TxtAudioMaxQueuedFrames_OnTextChanged(object sender, TextChangedEventArgs e)
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
    
    private void ChkXmp_OnClick(object sender, RoutedEventArgs e)
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
    
    private void SldXmpVolume_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (sender is not Slider xmpSlider)
        {
            return;
        }

        TxtSldXmpVolume.Text = $"{xmpSlider.Value}%";
    }
}