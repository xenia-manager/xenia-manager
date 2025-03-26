using System.Windows.Controls;

// Imported
using XeniaManager.Desktop.Utilities;

namespace XeniaManager.Desktop.Views.Pages;

/// <summary>
/// Interaction logic for SettingsPage.xaml
/// </summary>
public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        // Load language into the UI
        LoadCurrentLanguage();
    }

    private void LoadCurrentLanguage()
    {
        int selectedIndex = Array.FindIndex(LocalizationHelper.SupportedLanguages,
                                        lang => lang.TwoLetterISOLanguageName == App.Settings.UI.Language);
        if (selectedIndex >= 0)
        {
            CmbLanguage.SelectedIndex = selectedIndex;
        }
        else
        {
            CmbLanguage.SelectedIndex = 0;
            if (CmbLanguage.SelectedValue is string selectedLanguageCode)
            {
                App.Settings.UI.Language = selectedLanguageCode;
            }
        }
    }

    private void CmbLanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbLanguage.SelectedValue is string selectedLanguageCode)
        {
            LocalizationHelper.LoadLanguage(selectedLanguageCode);
            App.Settings.UI.Language = selectedLanguageCode;
        }
    }
}