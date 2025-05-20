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

    // Variables
    public Dictionary<int, string> CountryIdMap { get; } = new Dictionary<int, string>
    {
        { 1, "United Arab Emirates" }, { 2, "Albania" }, { 3, "Armenia" }, { 4, "Argentina" }, { 5, "Austria" },
        { 6, "Australia" }, { 7, "Azerbaijan" }, { 8, "Belgium" }, { 9, "Bulgaria" },
        { 10, "Bahrain" }, { 11, "Brunei" }, { 12, "Bolivia" }, { 13, "Brazil" }, { 14, "Belarus" },
        { 15, "Belize" }, { 16, "Canada" }, { 18, "Switzerland" }, { 19, "Chile" },
        { 20, "China" }, { 21, "Colombia" }, { 22, "Costa Rica" }, { 23, "Czech Republic" }, { 24, "Germany" },
        { 25, "Denmark" }, { 26, "Dominican Republic" }, { 27, "Algeria" }, { 28, "Ecuador" },
        { 29, "Estonia" }, { 30, "Egypt" }, { 31, "Spain" }, { 32, "Finland" }, { 33, "Faroe Islands" },
        { 34, "France" }, { 35, "United Kingdom" }, { 36, "Georgia" }, { 37, "Greece" },
        { 38, "Guatemala" }, { 39, "Hong Kong" }, { 40, "Honduras" }, { 41, "Croatia" }, { 42, "Hungary" },
        { 43, "Indonesia" }, { 44, "Ireland" }, { 45, "Israel" }, { 46, "India" },
        { 47, "Iraq" }, { 48, "Iran" }, { 49, "Iceland" }, { 50, "Italy" }, { 51, "Jamaica" },
        { 52, "Jordan" }, { 53, "Japan" }, { 54, "Kenya" }, { 55, "Kyrgyzstan" },
        { 56, "South Korea" }, { 57, "Kuwait" }, { 58, "Kazakhstan" }, { 59, "Lebanon" }, { 60, "Liechtenstein" },
        { 61, "Lithuania" }, { 62, "Luxembourg" }, { 63, "Latvia" }, { 64, "Libya" },
        { 65, "Morocco" }, { 66, "Monaco" }, { 67, "North Macedonia" }, { 68, "Mongolia" }, { 69, "Macau" },
        { 70, "Maldives" }, { 71, "Mexico" }, { 72, "Malaysia" }, { 73, "Nicaragua" },
        { 74, "Netherlands" }, { 75, "Norway" }, { 76, "New Zealand" }, { 77, "Oman" }, { 78, "Panama" },
        { 79, "Peru" }, { 80, "Philippines" }, { 81, "Pakistan" }, { 82, "Poland" },
        { 83, "Puerto Rico" }, { 84, "Portugal" }, { 85, "Paraguay" }, { 86, "Qatar" }, { 87, "Romania" },
        { 88, "Russia" }, { 89, "Saudi Arabia" }, { 90, "Sweden" }, { 91, "Singapore" },
        { 92, "Slovenia" }, { 93, "Slovakia" }, { 95, "El Salvador" }, { 96, "Syria" }, { 97, "Thailand" },
        { 98, "Tunisia" }, { 99, "Turkey" }, { 100, "Trinidad and Tobago" }, { 101, "Taiwan" },
        { 102, "Ukraine" }, { 103, "United States" }, { 104, "Uruguay" }, { 105, "Uzbekistan" },
        { 106, "Venezuela" }, { 107, "Vietnam" }, { 108, "Yemen" }, { 109, "South Africa" }
    };

    public XeniaSettingsPage()
    {
        InitializeComponent();
        this.DataContext = this;
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