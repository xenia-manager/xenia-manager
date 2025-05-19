using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging;
using XeniaManager.Core;
using XeniaManager.Desktop.Components;

namespace XeniaManager.Desktop.Views.Pages;

/// <summary>
/// Interaction logic for XeniaSettingsPage.xaml
/// </summary>
public partial class XeniaSettingsPage : Page
{
    // TODO: Xenia Settings Page

    public IEnumerable<string> AudioSystemOptions { get; } =
    [
        "any", "nop", "sdl", "xaudio2"
    ];

    public XeniaSettingsPage()
    {
        InitializeComponent();
        DataContext = this;
        ShowOnlyPanel(SpAudioSettings);
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
}